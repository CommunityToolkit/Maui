using Android.App;
using Android.Content;
using AndroidX.Activity;
using AndroidX.Activity.Result;
using CommunityToolkit.Maui.Core.Essentials;
using CommunityToolkit.Maui.DeviceTests.Runners;
using CommunityToolkit.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
using AndroidUri = Android.Net.Uri;

namespace CommunityToolkit.Maui.DeviceTests;

// ActivityResultManager (Android only) launches an Intent through the AndroidX ActivityResultRegistry of the current activity and
// yields the result. The tests put a FilesFakePickerActivity in front: a real ComponentActivity whose last step is replaced, so the
// registry, the lifecycle events and the toolkit's request tracking all run for real and only the system picker is faked.
[Category(TestCategory.ActivityResultManager)]
[Collection(TestCollections.DisableParallelization)]
public class ActivityResultManagerTests : FilesTestBase
{
	const string pickAction = "files.test.PICK";
	const string toolkitKeyPrefix = "CommunityToolkit.Maui.";

	[Fact]
	public async Task StartAsyncLaunchesTheIntentOnTheCurrentActivityAndCompletesWithTheResult()
	{
		var session = await StartSessionAsync();
		var intent = new Intent(pickAction).PutExtra("files.test.EXTRA", "value");
		Intent? received = null;

		var task = await StartRequestAsync(intent, onResult: result => received = result);
		var launch = await session.WaitForLaunchAsync();

		Assert.Same(session.Activity, launch.Activity);
		Assert.Equal(pickAction, launch.Intent.Action);
		Assert.Equal("value", launch.Intent.GetStringExtra("files.test.EXTRA"));
		Assert.False(task.IsCompleted);
		Assert.Single(FilesActivityResultManagerState.PendingKeys);

		await session.DeliverAsync(launch, Result.Ok, AnswerWith("content://files.test/answer"));
		var result = await task.WaitAsync(operationTimeout);

		Assert.Equal("content://files.test/answer", result.Data?.ToString());
		Assert.Same(received, result);
		Assert.Equal(0, FilesActivityResultManagerState.PendingCount);
	}

	[Theory]
	[InlineData(2000, "RequestCodeFolderPicker")]
	[InlineData(2001, "RequestCodeSaveFilePicker")]
	public async Task StartAsyncNamesTheRegistryKeyAfterTheRequestCodeAndAGuid(int requestCode, string expectedName)
	{
		var session = await StartSessionAsync();

		var task = await StartRequestAsync(new Intent(pickAction), (AndroidRequestCode)requestCode);
		var launch = await session.WaitForLaunchAsync();
		var key = Assert.Single(FilesActivityResultManagerState.PendingKeys);

		Assert.StartsWith($"{toolkitKeyPrefix}{expectedName}.", key, StringComparison.Ordinal);
		Assert.True(Guid.TryParseExact(key[$"{toolkitKeyPrefix}{expectedName}.".Length..], "N", out _), $"The key '{key}' does not end with a GUID.");
		Assert.Contains(key, await InvokeOnMainThreadAsync(() => FilesActivityResultRegistry.RegisteredKeys(session.Activity)));

		await session.DeliverAsync(launch, Result.Canceled);
		await Assert.ThrowsAsync<TaskCanceledException>(() => task);
	}

	[Fact]
	public async Task StartAsyncKeepsConcurrentRequestsApartAndCompletesEachWithItsOwnResult()
	{
		var session = await StartSessionAsync();
		var first = await StartRequestAsync(new Intent(pickAction), AndroidRequestCode.RequestCodeFolderPicker);
		var second = await StartRequestAsync(new Intent(pickAction), AndroidRequestCode.RequestCodeSaveFilePicker);
		var firstLaunch = await session.WaitForLaunchAsync(0);
		var secondLaunch = await session.WaitForLaunchAsync(1);

		Assert.NotEqual(firstLaunch.RequestCode, secondLaunch.RequestCode);
		Assert.Equal(2, FilesActivityResultManagerState.PendingKeys.Distinct().Count());

		await session.DeliverAsync(secondLaunch, Result.Ok, AnswerWith("content://files.test/second"));
		Assert.Equal("content://files.test/second", (await second.WaitAsync(operationTimeout)).Data?.ToString());
		Assert.False(first.IsCompleted);
		Assert.Single(FilesActivityResultManagerState.PendingKeys);

		await session.DeliverAsync(firstLaunch, Result.Ok, AnswerWith("content://files.test/first"));
		Assert.Equal("content://files.test/first", (await first.WaitAsync(operationTimeout)).Data?.ToString());
		Assert.Equal(0, FilesActivityResultManagerState.PendingCount);
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(1)]
	[InlineData(42)]
	public async Task StartAsyncTreatsEveryResultCodeButCanceledAsSuccess(int resultCode)
	{
		var session = await StartSessionAsync();
		var task = await StartRequestAsync(new Intent(pickAction));
		var launch = await session.WaitForLaunchAsync();

		await session.DeliverAsync(launch, (Result)resultCode, AnswerWith("content://files.test/answer"));

		Assert.Equal("content://files.test/answer", (await task.WaitAsync(operationTimeout)).Data?.ToString());
	}

	[Fact]
	public async Task StartAsyncCompletesCanceledWithoutCallingOnResultWhenThePickerIsCanceled()
	{
		var session = await StartSessionAsync();
		var onResultCalls = 0;
		var task = await StartRequestAsync(new Intent(pickAction), onResult: _ => onResultCalls++);
		var launch = await session.WaitForLaunchAsync();

		await session.DeliverAsync(launch, Result.Canceled, AnswerWith("content://files.test/ignored"));

		await Assert.ThrowsAsync<TaskCanceledException>(() => task);
		Assert.True(task.IsCanceled);
		Assert.Equal(0, onResultCalls);
		Assert.Equal(0, FilesActivityResultManagerState.PendingCount);
	}

	[Fact]
	public async Task StartAsyncPassesAnEmptyIntentWhenTheResultHasNoData()
	{
		var session = await StartSessionAsync();
		Intent? received = null;
		var task = await StartRequestAsync(new Intent(pickAction), onResult: result => received = result);
		var launch = await session.WaitForLaunchAsync();

		await session.DeliverAsync(launch, Result.Ok);
		var result = await task.WaitAsync(operationTimeout);

		Assert.NotNull(result);
		Assert.Null(result.Data);
		Assert.Same(received, result);
	}

	[Fact]
	public async Task StartAsyncFaultsTheTaskWithTheExceptionThatOnResultThrows()
	{
		var session = await StartSessionAsync();
		var failure = new InvalidOperationException("The result callback failed.");
		var task = await StartRequestAsync(new Intent(pickAction), onResult: _ => throw failure);
		var launch = await session.WaitForLaunchAsync();

		await session.DeliverAsync(launch, Result.Ok, AnswerWith("content://files.test/answer"));

		Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => task));
		Assert.Equal(0, FilesActivityResultManagerState.PendingCount);
	}

	[Fact]
	public async Task StartAsyncWithoutAnIntentThrowsBeforeAnythingIsRegistered()
	{
		var session = await StartSessionAsync();

		var exception = Assert.Throws<ArgumentNullException>(() => Start(null!));

		Assert.Equal("intent", exception.ParamName);
		Assert.Empty(session.Launches);
		Assert.Equal(0, FilesActivityResultManagerState.PendingCount);
	}

	[Fact]
	public async Task StartAsyncWithACanceledTokenThrowsBeforeLaunching()
	{
		var session = await StartSessionAsync();
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();

		Assert.Throws<OperationCanceledException>(() => Start(new Intent(pickAction), cancellation.Token));

		Assert.Empty(session.Launches);
		Assert.Equal(0, FilesActivityResultManagerState.PendingCount);
	}

	[Fact]
	public async Task StartAsyncChecksTheIntentBeforeTheToken()
	{
		await StartSessionAsync();
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();

		Assert.Throws<ArgumentNullException>(() => Start(null!, cancellation.Token));
	}

	[Fact]
	public async Task StartAsyncThrowsWhenTheCurrentActivityIsNotAComponentActivity()
	{
		var session = await StartSessionAsync();
		await session.StartPlainAsync();

		var exception = Assert.Throws<InvalidOperationException>(() => Start(new Intent(pickAction)));

		Assert.Contains(nameof(ComponentActivity), exception.Message, StringComparison.Ordinal);
		Assert.Empty(session.Launches);
		Assert.Equal(0, FilesActivityResultManagerState.PendingCount);
	}

	[Fact]
	public async Task StartAsyncRethrowsTheLaunchFailureAndReleasesTheRequest()
	{
		// No fake picker here: the runner's own activity launches an intent that no app can handle, as a device without a document
		// picker would for FileSaver and FolderPicker, and the framework throws ActivityNotFoundException from Launch.
		var current = Assert.IsType<ComponentActivity>(Platform.CurrentActivity, exactMatch: false);

		var exception = await Assert.ThrowsAsync<ActivityNotFoundException>(() => InvokeOnMainThreadAsync(() =>
		{
			_ = ActivityResultManager.StartAsync(new Intent("files.test.NOBODY_HANDLES_THIS"), AndroidRequestCode.RequestCodeFolderPicker);
		}));

		Assert.Contains("files.test.NOBODY_HANDLES_THIS", exception.Message, StringComparison.Ordinal);
		Assert.Equal(0, FilesActivityResultManagerState.PendingCount);
		await AssertRegistryReleasedAsync(current);
	}

	[Fact]
	public async Task CancelingTheTokenCancelsTheTaskAndTheLateResultIsDrained()
	{
		var session = await StartSessionAsync();
		using var cancellation = new CancellationTokenSource();
		var onResultCalls = 0;
		var task = await StartRequestAsync(new Intent(pickAction), onResult: _ => onResultCalls++, token: cancellation.Token);
		var launch = await session.WaitForLaunchAsync();

		await cancellation.CancelAsync();

		var exception = await Assert.ThrowsAsync<TaskCanceledException>(() => task);
		Assert.Equal(cancellation.Token, exception.CancellationToken);
		// The request stays registered until the picker answers: AndroidX keeps the launched key until its result is drained.
		Assert.Single(FilesActivityResultManagerState.PendingKeys);

		await session.DeliverAsync(launch, Result.Ok, AnswerWith("content://files.test/late"));
		await AssertNoPendingRequestsAsync();
		await AssertRegistryReleasedAsync(session.Activity);

		Assert.Equal(0, onResultCalls);
		Assert.True(task.IsCanceled);
	}

	[Fact]
	public async Task CancelingTheTokenAfterTheResultArrivedChangesNothing()
	{
		var session = await StartSessionAsync();
		using var cancellation = new CancellationTokenSource();
		var task = await StartRequestAsync(new Intent(pickAction), token: cancellation.Token);
		var launch = await session.WaitForLaunchAsync();
		await session.DeliverAsync(launch, Result.Ok, AnswerWith("content://files.test/answer"));
		await task.WaitAsync(operationTimeout);

		await cancellation.CancelAsync();

		Assert.Equal(TaskStatus.RanToCompletion, task.Status);
		Assert.Equal(0, FilesActivityResultManagerState.PendingCount);
	}

	[Fact]
	public async Task StartAsyncCompletesWhenThePickerAnswersWhileTheLaunchIsStillRunning()
	{
		var session = await StartSessionAsync();
		FilesFakePicker.LaunchBehavior = launch => launch.Activity.DeliverResult(launch.RequestCode, Result.Ok, AnswerWith("content://files.test/immediate"));
		using var cancellation = new CancellationTokenSource();

		var task = await StartRequestAsync(new Intent(pickAction), token: cancellation.Token);

		Assert.True(task.IsCompleted, "The result was delivered inside Launch, so the task is complete when StartAsync returns.");
		Assert.Equal("content://files.test/immediate", (await task).Data?.ToString());
		Assert.Equal(0, FilesActivityResultManagerState.PendingCount);
		await cancellation.CancelAsync();
		Assert.Equal(TaskStatus.RanToCompletion, task.Status);
		await AssertRegistryReleasedAsync(session.Activity);
	}

	[Fact]
	public async Task FinishingTheHostActivityCancelsThePendingRequestAndReleasesIt()
	{
		var session = await StartSessionAsync();
		var task = await StartRequestAsync(new Intent(pickAction));
		await session.WaitForLaunchAsync();

		await session.FinishAsync(session.Activity);

		await Assert.ThrowsAsync<TaskCanceledException>(() => task);
		Assert.True(task.IsCanceled);
		Assert.Equal(0, FilesActivityResultManagerState.PendingCount);
	}

	[Fact]
	public async Task FinishingTheHostActivityAfterTheTokenWasCanceledRemovesTheRequestWithoutAResult()
	{
		var session = await StartSessionAsync();
		using var cancellation = new CancellationTokenSource();
		var task = await StartRequestAsync(new Intent(pickAction), token: cancellation.Token);
		await session.WaitForLaunchAsync();
		await cancellation.CancelAsync();
		Assert.Single(FilesActivityResultManagerState.PendingKeys);

		await session.FinishAsync(session.Activity);

		Assert.True(task.IsCanceled);
		Assert.Equal(0, FilesActivityResultManagerState.PendingCount);
	}

	[Fact]
	public async Task FinishingAnotherActivityLeavesThePendingRequestOnItsHost()
	{
		var session = await StartSessionAsync();
		var host = session.Activity;
		var task = await StartRequestAsync(new Intent(pickAction));
		var launch = await session.WaitForLaunchAsync();

		var another = await session.StartAnotherAsync();
		Assert.Same(host, Assert.Single(FilesActivityResultManagerState.RegisteredActivities));
		await session.FinishAsync(another);

		Assert.False(task.IsCompleted);
		Assert.Same(host, Assert.Single(FilesActivityResultManagerState.RegisteredActivities));
		await session.DeliverAsync(launch, Result.Ok, AnswerWith("content://files.test/answer"));
		Assert.Equal("content://files.test/answer", (await task.WaitAsync(operationTimeout)).Data?.ToString());
	}

	[Fact]
	public async Task AnActivityThatIsNotAComponentActivityComingAndGoingLeavesThePendingRequestAlone()
	{
		var session = await StartSessionAsync();
		var host = session.Activity;
		var task = await StartRequestAsync(new Intent(pickAction));
		var launch = await session.WaitForLaunchAsync();

		var plain = await session.StartPlainAsync();
		await session.FinishAsync(plain);

		Assert.False(task.IsCompleted);
		Assert.Same(host, Assert.Single(FilesActivityResultManagerState.RegisteredActivities));
		await session.DeliverAsync(launch, Result.Ok, AnswerWith("content://files.test/answer"));
		Assert.Equal("content://files.test/answer", (await task.WaitAsync(operationTimeout)).Data?.ToString());
	}

	[Fact]
	public async Task AResultDeliveredToTheNewActivityAfterItsCreationCompletesTheRequest()
	{
		var session = await StartSessionAsync();
		var original = session.Activity;
		var task = await StartRequestAsync(new Intent(pickAction));
		var launch = await session.WaitForLaunchAsync();

		var recreated = await session.RecreateAsync(original);

		Assert.NotSame(original, recreated);
		Assert.False(task.IsCompleted);
		Assert.Same(recreated, Assert.Single(FilesActivityResultManagerState.RegisteredActivities));
		Assert.Contains(Assert.Single(FilesActivityResultManagerState.PendingKeys), await InvokeOnMainThreadAsync(() => FilesActivityResultRegistry.RegisteredKeys(recreated)));

		await FilesFakePicker.DeliverAsync(recreated, launch.RequestCode, Result.Ok, AnswerWith("content://files.test/restored"));
		Assert.Equal("content://files.test/restored", (await task.WaitAsync(operationTimeout)).Data?.ToString());
		Assert.Equal(0, FilesActivityResultManagerState.PendingCount);
	}

	[Fact]
	public async Task AResultDeliveredToTheNewActivityAfterTheRequestRegisteredWithItCompletesTheRequest()
	{
		var session = await StartSessionAsync();
		Intent? received = null;
		var task = await StartRequestAsync(new Intent(pickAction), onResult: result => received = result);
		var launch = await session.WaitForLaunchAsync();
		var request = FilesPendingRequest.FromTracked(Assert.Single(FilesActivityResultManagerState.PendingKeys));
		RequestSnapshot? beforeDelivery = null;
		var completedByDelivery = false;
		Exception? hookFailure = null;
		// CreatedBehavior runs after the base OnCreate, where the toolkit heard of the new instance and registered the request with it,
		// so the answer reaches the request's callback through the registry's normal dispatch.
		FilesFakePicker.CreatedBehavior = (activity, restored) =>
		{
			if (!restored)
			{
				return;
			}

			try
			{
				beforeDelivery = RequestSnapshot.Of(request, activity);
				activity.DeliverResult(launch.RequestCode, Result.Ok, AnswerWith("content://files.test/restored"));
				completedByDelivery = task.IsCompleted;
			}
			catch (Exception ex)
			{
				hookFailure = ex;
			}
		};

		var recreated = await session.RecreateAsync(session.Activity);
		var result = await task.WaitAsync(operationTimeout);

		Assert.Null(hookFailure);
		Assert.NotNull(beforeDelivery);
		Assert.True(beforeDelivery.RegisteredWithTheNewActivity && beforeDelivery.HasLauncher && beforeDelivery.NewRegistryHasTheCallback, $"The request was not registered with the new activity when CreatedBehavior ran: {beforeDelivery}.");
		Assert.True(completedByDelivery, "The registry did not hand the answer to the request's callback when it was delivered.");
		Assert.Equal("content://files.test/restored", result.Data?.ToString());
		Assert.Same(received, result);
		Assert.Equal(0, FilesActivityResultManagerState.PendingCount);
		await AssertRegistryReleasedAsync(recreated);
	}

	[Fact]
	public async Task AResultDeliveredBeforeTheRequestRegistersWithTheNewActivityCompletesItInsideRegister()
	{
		var session = await StartSessionAsync();
		Intent? received = null;
		var task = await StartRequestAsync(new Intent(pickAction), onResult: result => received = result);
		var launch = await session.WaitForLaunchAsync();
		var request = FilesPendingRequest.FromTracked(Assert.Single(FilesActivityResultManagerState.PendingKeys));
		RequestSnapshot? beforeDelivery = null, afterDelivery = null;
		var completedByDelivery = true;
		Exception? hookFailure = null;
		// The new instance gets the answer while it is being created, after its registry restored the launched key and before the
		// lifecycle event that makes the toolkit register the request with it. The registry has no callback for the key yet, so it keeps
		// the answer as a pending result and hands it to the callback inside Register: the request completes before Register stored its
		// launcher, and Register releases the launcher that it has just created.
		FilesFakePicker.ContextAvailableBehavior = (activity, restored) =>
		{
			if (!restored)
			{
				return;
			}

			try
			{
				beforeDelivery = RequestSnapshot.Of(request, activity);
				activity.DeliverResult(launch.RequestCode, Result.Ok, AnswerWith("content://files.test/early"));
				afterDelivery = RequestSnapshot.Of(request, activity);
				completedByDelivery = task.IsCompleted;
			}
			catch (Exception ex)
			{
				hookFailure = ex;
			}
		};

		var recreated = await session.RecreateAsync(session.Activity);
		var result = await task.WaitAsync(operationTimeout);

		Assert.Null(hookFailure);
		Assert.NotNull(beforeDelivery);
		Assert.NotNull(afterDelivery);
		// The path this test is about: when the answer arrived, the request had no activity and no launcher, and the new registry had no
		// callback for it, so nothing could dispatch it yet.
		Assert.True(!beforeDelivery.HasActivity && !beforeDelivery.HasLauncher && !beforeDelivery.NewRegistryHasTheCallback && beforeDelivery.State == "Pending", $"The request was already registered with the new activity: {beforeDelivery}.");
		Assert.True(!completedByDelivery && afterDelivery.State == "Pending", $"The answer was dispatched when it was delivered: {afterDelivery}.");
		Assert.Equal("content://files.test/early", result.Data?.ToString());
		Assert.Same(received, result);
		Assert.Null(request.Launcher);
		Assert.Null(request.RegisteredActivity);
		Assert.Equal(0, FilesActivityResultManagerState.PendingCount);
		await AssertRegistryReleasedAsync(recreated);
	}

	[Fact]
	public async Task RecreatingTheActivityBehindAnotherKeepsItsPendingRequestAlive()
	{
		var session = await StartSessionAsync();
		var host = session.Activity;
		var task = await StartRequestAsync(new Intent(pickAction));
		await session.WaitForLaunchAsync();
		var front = await session.StartAnotherAsync();

		// The system destroys the activity behind (not finishing) and creates its new instance, which stays behind the front one.
		var recreated = await session.RecreateAsync(host, waitForResumed: false);

		// Whichever live activity the request is registered with now, it is not the destroyed instance, and the request still waits for
		// its result instead of being canceled with the activity.
		var registeredWith = Assert.Single(FilesActivityResultManagerState.RegisteredActivities);
		Assert.NotNull(registeredWith);
		Assert.NotSame(host, registeredWith);
		Assert.False(registeredWith.IsDestroyed);
		Assert.False(task.IsCompleted);
		Assert.NotSame(front, recreated);
	}

	[Fact]
	public async Task RecreatingTheActivityBehindAPlainActivityRegistersTheRequestWithTheNewInstance()
	{
		var session = await StartSessionAsync();
		var host = session.Activity;
		var task = await StartRequestAsync(new Intent(pickAction));
		var launch = await session.WaitForLaunchAsync();
		await session.StartPlainAsync();

		// The activity in front cannot receive results, so the request waits without an activity until the new instance is created.
		var recreated = await session.RecreateAsync(host, waitForResumed: false);

		Assert.Same(recreated, Assert.Single(FilesActivityResultManagerState.RegisteredActivities));
		Assert.False(task.IsCompleted);
		await FilesFakePicker.DeliverAsync(recreated, launch.RequestCode, Result.Ok, AnswerWith("content://files.test/answer"));
		Assert.Equal("content://files.test/answer", (await task.WaitAsync(operationTimeout)).Data?.ToString());
		Assert.Equal(0, FilesActivityResultManagerState.PendingCount);
	}

	[Fact]
	public async Task ARequestWhoseActivityIsRecreatedBehindAnotherCompletesWithTheResultDeliveredToTheRecreatedActivity()
	{
		var session = await StartSessionAsync();
		var host = session.Activity;
		var task = await StartRequestAsync(new Intent(pickAction));
		var launch = await session.WaitForLaunchAsync();
		await session.StartAnotherAsync();
		var recreated = await session.RecreateAsync(host, waitForResumed: false);

		// Android returns the picker's answer to the activity that asked for it, which is now the recreated instance.
		await FilesFakePicker.DeliverAsync(recreated, launch.RequestCode, Result.Ok, AnswerWith("content://files.test/answer"));

		await AssertionExtensions.AssertEventually(() => task.IsCompleted, timeout: 5000, message: $"The request did not complete with the result delivered to the recreated activity (status {task.Status}).");
		Assert.Equal("content://files.test/answer", (await task).Data?.ToString());
	}

	// The same rule when another ComponentActivity (here the runner's activity, of another type) is created after the activity that
	// started the request was destroyed without finishing and before its new instance: the request waits for the new instance (the same
	// type in the same task), which Android hands the result to, instead of registering with the other activity. The lifecycle events
	// are raised as MAUI raises them, with the host standing in for its own new instance.
	[Fact]
	public async Task ARequestWhoseActivityWasDestroyedIsNotTakenByAnotherActivityCreatedBeforeItsNewInstance()
	{
		var runnerActivity = Assert.IsType<ComponentActivity>(TestWindow.PlatformWindow, exactMatch: false);
		var session = await StartSessionAsync();
		var host = session.Activity;
		var task = await StartRequestAsync(new Intent(pickAction));
		var launch = await session.WaitForLaunchAsync();
		Assert.NotEqual(host.GetType(), runnerActivity.GetType());
		var key = Assert.Single(FilesActivityResultManagerState.PendingKeys);

		// A request that the runner's activity took keeps its callback in that registry, which later tests check: release it in any case
		AddCleanup(() => InvokeOnMainThreadAsync(() =>
		{
			if (FilesActivityResultManagerState.PendingKeys.Contains(key))
			{
				FilesPendingRequest.FromTracked(key).Dispose();
			}
		}));

		await InvokeOnMainThreadAsync(() =>
		{
			FilesActivityResultManagerState.RaiseActivityStateChanged(host, ActivityState.Destroyed);
			FilesActivityResultManagerState.RaiseActivityStateChanged(runnerActivity, ActivityState.Created);
		});

		Assert.Null(Assert.Single(FilesActivityResultManagerState.RegisteredActivities));

		await InvokeOnMainThreadAsync(() => FilesActivityResultManagerState.RaiseActivityStateChanged(host, ActivityState.Created));

		Assert.Same(host, Assert.Single(FilesActivityResultManagerState.RegisteredActivities));
		await session.DeliverAsync(launch, Result.Ok, AnswerWith("content://files.test/answer"));
		Assert.Equal("content://files.test/answer", (await task.WaitAsync(operationTimeout)).Data?.ToString());
		Assert.Equal(0, FilesActivityResultManagerState.PendingCount);
	}

	// An activity of the requester's type in another task (another window of the app, a document opened in its own task) is not the new instance
	// of a destroyed requester either: Android hands the result to the requester's task, so the request waits for the new instance there.
	// Catches: a pending request registering with any activity of the requester's type, whatever its task.
	[Fact]
	public async Task ARequestWhoseActivityWasDestroyedIsNotTakenByAnActivityOfTheSameTypeInAnotherTask()
	{
		var session = await StartSessionAsync();
		var host = session.Activity;
		var task = await StartRequestAsync(new Intent(pickAction));
		var launch = await session.WaitForLaunchAsync();
		var other = await session.StartInAnotherTaskAsync();
		Assert.Equal(host.GetType(), other.GetType());
		Assert.NotEqual(host.TaskId, other.TaskId);
		var key = Assert.Single(FilesActivityResultManagerState.PendingKeys);

		// A request that the other activity took keeps its callback in that registry, which later tests check: release it in any case
		AddCleanup(() => InvokeOnMainThreadAsync(() =>
		{
			if (FilesActivityResultManagerState.PendingKeys.Contains(key))
			{
				FilesPendingRequest.FromTracked(key).Dispose();
			}
		}));

		await InvokeOnMainThreadAsync(() =>
		{
			FilesActivityResultManagerState.RaiseActivityStateChanged(host, ActivityState.Destroyed);
			FilesActivityResultManagerState.RaiseActivityStateChanged(other, ActivityState.Created);
		});

		Assert.Null(Assert.Single(FilesActivityResultManagerState.RegisteredActivities));

		await InvokeOnMainThreadAsync(() => FilesActivityResultManagerState.RaiseActivityStateChanged(host, ActivityState.Created));

		Assert.Same(host, Assert.Single(FilesActivityResultManagerState.RegisteredActivities));
		await session.DeliverAsync(launch, Result.Ok, AnswerWith("content://files.test/answer"));
		Assert.Equal("content://files.test/answer", (await task.WaitAsync(operationTimeout)).Data?.ToString());
		Assert.Equal(0, FilesActivityResultManagerState.PendingCount);
	}

	[Fact]
	public async Task StartAsyncFromABackgroundThreadCompletesLikeFromTheUiThread()
	{
		var session = await StartSessionAsync();

		Task<Intent> task = null!;
		await Task.Run(() => { task = ActivityResultManager.StartAsync(new Intent(pickAction), AndroidRequestCode.RequestCodeFolderPicker); });
		var launch = await session.WaitForLaunchAsync();
		await session.DeliverAsync(launch, Result.Ok, AnswerWith("content://files.test/answer"));

		Assert.Equal("content://files.test/answer", (await task.WaitAsync(operationTimeout)).Data?.ToString());
		Assert.Equal(0, FilesActivityResultManagerState.PendingCount);
	}

	[Fact]
	public async Task ACompletedRequestIsNotKeptAliveByTheManagerOrTheRegistry()
	{
		var session = await StartSessionAsync();
		WeakReference callbackTarget = null!;
		Task<Intent> task = null!;
		await InvokeOnMainThreadAsync(() =>
		{
			var captured = new object();
			callbackTarget = new WeakReference(captured);
			task = ActivityResultManager.StartAsync(new Intent(pickAction), AndroidRequestCode.RequestCodeFolderPicker, _ => GC.KeepAlive(captured));
		});
		var launch = await session.WaitForLaunchAsync();
		await session.DeliverAsync(launch, Result.Ok, AnswerWith("content://files.test/answer"));
		await task.WaitAsync(operationTimeout);
		task = null!;

		await AssertionExtensions.WaitForGC(callbackTarget);
	}

	[Fact]
	public async Task ACanceledRequestIsNotKeptAliveOnceItsLateResultWasDrained()
	{
		var session = await StartSessionAsync();
		using var cancellation = new CancellationTokenSource();
		WeakReference callbackTarget = null!;
		Task<Intent> task = null!;
		await InvokeOnMainThreadAsync(() =>
		{
			var captured = new object();
			callbackTarget = new WeakReference(captured);
			task = ActivityResultManager.StartAsync(new Intent(pickAction), AndroidRequestCode.RequestCodeFolderPicker, _ => GC.KeepAlive(captured), cancellation.Token);
		});
		var launch = await session.WaitForLaunchAsync();
		await cancellation.CancelAsync();
		await session.DeliverAsync(launch, Result.Ok);
		await AssertNoPendingRequestsAsync();
		task = null!;

		await AssertionExtensions.WaitForGC(callbackTarget);
	}

	// PendingRequest, one method at a time. StartAsync always registers, launches and tracks in one go, so these cover what it cannot:
	// a request that was never registered, one that is failed or disposed, one registered twice, and the guard of the tracking table.

	[Fact]
	public async Task LaunchingARequestThatWasNeverRegisteredThrows()
	{
		await StartSessionAsync();
		var request = new FilesPendingRequest("files.test.key");

		var exception = Assert.Throws<InvalidOperationException>(() => request.Launch(new Intent(pickAction)));

		Assert.Equal("Unable to register the activity result launcher.", exception.Message);
	}

	[Fact]
	public async Task RegisteringARequestTwiceKeepsTheFirstActivity()
	{
		var session = await StartSessionAsync();
		var host = session.Activity;
		var another = await session.StartAnotherAsync();
		var request = new FilesPendingRequest($"files.test.{Guid.NewGuid():N}");

		await InvokeOnMainThreadAsync(() => request.Register(host));
		await InvokeOnMainThreadAsync(() => request.Register(another));

		Assert.Same(host, request.RegisteredActivity);
		Assert.NotNull(request.Launcher);
		await InvokeOnMainThreadAsync(request.Dispose);
	}

	[Fact]
	public async Task RegisteringADisposedRequestDoesNothing()
	{
		var session = await StartSessionAsync();
		var request = new FilesPendingRequest($"files.test.{Guid.NewGuid():N}");
		request.Dispose();

		await InvokeOnMainThreadAsync(() => request.Register(session.Activity));

		Assert.Null(request.RegisteredActivity);
		Assert.Null(request.Launcher);
		Assert.Equal("Completed", request.State);
	}

	[Fact]
	public async Task DisposingAPendingRequestReleasesItsLauncherButDoesNotCompleteTheTask()
	{
		var session = await StartSessionAsync();
		var request = new FilesPendingRequest($"files.test.{Guid.NewGuid():N}");
		await InvokeOnMainThreadAsync(() => request.Register(session.Activity));
		Assert.Contains(request.Key, await InvokeOnMainThreadAsync(() => FilesActivityResultRegistry.RegisteredKeys(session.Activity)));

		await InvokeOnMainThreadAsync(request.Dispose);
		await InvokeOnMainThreadAsync(request.Dispose);

		Assert.Equal("Completed", request.State);
		Assert.Null(request.RegisteredActivity);
		Assert.Null(request.Launcher);
		Assert.False(request.Task.IsCompleted);
		await AssertionExtensions.AssertEventuallyAsync(
			() => InvokeOnMainThreadAsync(() => !FilesActivityResultRegistry.RegisteredKeys(session.Activity).Contains(request.Key)),
			timeout: 5000,
			message: "Disposing the request did not unregister its launcher.");
	}

	[Fact]
	public async Task FailingARequestWithoutAnExceptionThrows()
	{
		await StartSessionAsync();
		var request = new FilesPendingRequest("files.test.key");

		var exception = Assert.Throws<ArgumentNullException>(() => request.Fail(null));

		Assert.Equal("exception", exception.ParamName);
		Assert.Equal("Pending", request.State);
	}

	[Fact]
	public async Task FailingAPendingRequestFaultsItsTaskOnce()
	{
		var session = await StartSessionAsync();
		var request = new FilesPendingRequest($"files.test.{Guid.NewGuid():N}");
		await InvokeOnMainThreadAsync(() => request.Register(session.Activity));
		var first = new InvalidOperationException("first");

		await InvokeOnMainThreadAsync(() => request.Fail(first));
		await InvokeOnMainThreadAsync(() => request.Fail(new InvalidOperationException("second")));

		Assert.Same(first, await Assert.ThrowsAsync<InvalidOperationException>(() => request.Task));
		Assert.Equal("Completed", request.State);
		Assert.Null(request.RegisteredActivity);
	}

	[Fact]
	public async Task FailingARequestThatWasAlreadyDisposedLeavesItsTaskPending()
	{
		var request = new FilesPendingRequest($"files.test.{Guid.NewGuid():N}");
		await StartSessionAsync();
		request.Dispose();

		request.Fail(new InvalidOperationException("too late"));

		Assert.False(request.Task.IsCompleted);
	}

	[Fact]
	public async Task DetachingFromAnotherActivityDoesNothing()
	{
		var session = await StartSessionAsync();
		var host = session.Activity;
		var another = await session.StartAnotherAsync();
		var request = new FilesPendingRequest($"files.test.{Guid.NewGuid():N}");
		await InvokeOnMainThreadAsync(() => request.Register(host));

		await InvokeOnMainThreadAsync(() => request.Detach(another, isFinishing: true));

		Assert.Same(host, request.RegisteredActivity);
		Assert.Equal("Pending", request.State);
		Assert.False(request.Task.IsCompleted);
		await InvokeOnMainThreadAsync(request.Dispose);
	}

	[Fact]
	public async Task DetachingFromTheActivityWithoutFinishingKeepsTheRequestPendingWithoutAnActivity()
	{
		var session = await StartSessionAsync();
		var host = session.Activity;
		var request = new FilesPendingRequest($"files.test.{Guid.NewGuid():N}");
		await InvokeOnMainThreadAsync(() => request.Register(host));

		await InvokeOnMainThreadAsync(() => request.Detach(host, isFinishing: false));

		Assert.Null(request.RegisteredActivity);
		Assert.Null(request.Launcher);
		Assert.Equal("Pending", request.State);
		Assert.False(request.Task.IsCompleted);
		await AssertionExtensions.AssertEventuallyAsync(
			() => InvokeOnMainThreadAsync(() => !FilesActivityResultRegistry.RegisteredKeys(host).Contains(request.Key)),
			timeout: 5000,
			message: "Detaching did not unregister the launcher.");
	}

	[Fact]
	public async Task DetachingFromTheFinishingActivityCancelsTheRequest()
	{
		var session = await StartSessionAsync();
		var request = new FilesPendingRequest($"files.test.{Guid.NewGuid():N}");
		await InvokeOnMainThreadAsync(() => request.Register(session.Activity));

		await InvokeOnMainThreadAsync(() => request.Detach(session.Activity, isFinishing: true));

		Assert.Equal("Completed", request.State);
		await Assert.ThrowsAsync<TaskCanceledException>(() => request.Task);
	}

	[Fact]
	public async Task CancellationRegisteredAfterTheRequestWasDisposedNeverCancelsIt()
	{
		await StartSessionAsync();
		using var cancellation = new CancellationTokenSource();
		var request = new FilesPendingRequest($"files.test.{Guid.NewGuid():N}", token: cancellation.Token);
		request.Dispose();

		request.RegisterCancellation();
		await cancellation.CancelAsync();

		Assert.False(request.Task.IsCompleted);
	}

	[Fact]
	public async Task CancellationRegisteredOnAPendingRequestCancelsIt()
	{
		await StartSessionAsync();
		using var cancellation = new CancellationTokenSource();
		var request = new FilesPendingRequest($"files.test.{Guid.NewGuid():N}", token: cancellation.Token);

		request.RegisterCancellation();
		await cancellation.CancelAsync();

		await Assert.ThrowsAsync<TaskCanceledException>(() => request.Task);
		Assert.Equal("Canceled", request.State);
	}

	[Fact]
	public async Task DisposingARequestRemovesOnlyTheEntryThatItOwns()
	{
		await StartSessionAsync();
		var key = $"files.test.{Guid.NewGuid():N}";
		var owner = new FilesPendingRequest(key);
		var impostor = new FilesPendingRequest(key);
		FilesActivityResultManagerState.Track(key, impostor);
		AddCleanup(() => FilesActivityResultManagerState.Untrack(key));

		owner.Dispose();

		Assert.Contains(key, FilesActivityResultManagerState.PendingKeys);

		impostor.Dispose();

		Assert.DoesNotContain(key, FilesActivityResultManagerState.PendingKeys);
	}

	[Fact]
	public async Task DisposingARequestThatTheTableOwnsRemovesIt()
	{
		await StartSessionAsync();
		var key = $"files.test.{Guid.NewGuid():N}";
		var request = new FilesPendingRequest(key);
		FilesActivityResultManagerState.Track(key, request);
		AddCleanup(() => FilesActivityResultManagerState.Untrack(key));

		request.Dispose();

		Assert.DoesNotContain(key, FilesActivityResultManagerState.PendingKeys);
	}

	[Fact]
	public async Task CancelingTheTokenWhileTheLaunchIsRunningReturnsACanceledTaskAndDrainsTheLateResult()
	{
		var session = await StartSessionAsync();
		using var cancellation = new CancellationTokenSource();
		var onResultCalls = 0;
		// The token is canceled inside Launch, after StartAsync checked it and before it registered for its cancellation.
		FilesFakePicker.LaunchBehavior = _ => cancellation.Cancel();

		var task = await StartRequestAsync(new Intent(pickAction), onResult: _ => onResultCalls++, token: cancellation.Token);
		var launch = await session.WaitForLaunchAsync();

		Assert.True(task.IsCanceled, $"The task is {task.Status}.");
		Assert.Single(FilesActivityResultManagerState.PendingKeys);
		await session.DeliverAsync(launch, Result.Ok, AnswerWith("content://files.test/late"));
		await AssertNoPendingRequestsAsync();
		await AssertRegistryReleasedAsync(session.Activity);
		Assert.Equal(0, onResultCalls);
	}

	[Fact]
	public async Task AResultForAnActivityWithoutAWindowReleasesTheLauncherAtOnce()
	{
		var session = await StartSessionAsync();
		var detached = await FilesFakePicker.CreateDetachedAsync();
		var request = new FilesPendingRequest($"files.test.{Guid.NewGuid():N}");
		await InvokeOnMainThreadAsync(() =>
		{
			request.Register(detached);
			request.Launch(new Intent(pickAction));
		});
		var launch = await FilesFakePicker.WaitForLaunchAsync();
		Assert.Same(detached, launch.Activity);
		Assert.Null(await InvokeOnMainThreadAsync(() => detached.Window));

		// With no window there is no view to post the cleanup to, so the launcher is unregistered while the result is dispatched.
		var keysRightAfterTheResult = await InvokeOnMainThreadAsync(() =>
		{
			detached.DeliverResult(launch.RequestCode, Result.Ok, AnswerWith("content://files.test/answer"));
			return FilesActivityResultRegistry.RegisteredKeys(detached);
		});

		Assert.DoesNotContain(request.Key, keysRightAfterTheResult);
		Assert.Equal("content://files.test/answer", (await request.Task.WaitAsync(operationTimeout)).Data?.ToString());
		Assert.True(request.IsCallbackReleased);
		Assert.Null(request.Launcher);
		Assert.Null(request.RegisteredActivity);
	}

	[Fact]
	public async Task AResultForAnActivityWithAWindowReleasesTheLauncherAfterTheResultWasDispatched()
	{
		var session = await StartSessionAsync();
		var task = await StartRequestAsync(new Intent(pickAction));
		var launch = await session.WaitForLaunchAsync();
		var key = Assert.Single(FilesActivityResultManagerState.PendingKeys);

		// AndroidX removes a launched key only after its callback ran, so the toolkit posts the cleanup to the activity's view.
		var keysRightAfterTheResult = await InvokeOnMainThreadAsync(() =>
		{
			launch.Activity.DeliverResult(launch.RequestCode, Result.Ok, AnswerWith("content://files.test/answer"));
			return FilesActivityResultRegistry.RegisteredKeys(launch.Activity);
		});

		Assert.Contains(key, keysRightAfterTheResult);
		Assert.Equal("content://files.test/answer", (await task.WaitAsync(operationTimeout)).Data?.ToString());
		await AssertRegistryReleasedAsync(session.Activity);
	}

	[Fact]
	public async Task AResultThatArrivesWhileTheRequestHasNoActivityCompletesItWithoutALauncher()
	{
		var session = await StartSessionAsync();
		Intent? received = null;
		var request = new FilesPendingRequest($"files.test.{Guid.NewGuid():N}", onResult: result => received = result);
		await InvokeOnMainThreadAsync(() =>
		{
			request.Register(session.Activity);
			request.Launch(new Intent(pickAction));
		});
		var launch = await session.WaitForLaunchAsync();
		using var release = new ManualResetEventSlim();
		var uiThreadBusy = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		// Detach hands its launcher to the UI thread to unregister it, and Activity.RunOnUiThread only posts when it is called from
		// another thread. While the UI thread is busy the request's callback is therefore still registered, and an answer that the
		// registry dispatches then reaches a request that has already let go of its activity (and of its launcher).
		MainThread.BeginInvokeOnMainThread(() =>
		{
			uiThreadBusy.TrySetResult();
			release.Wait(operationTimeout);
		});

		try
		{
			await uiThreadBusy.Task.WaitAsync(operationTimeout);
			await Task.Run(() => request.Detach(session.Activity, isFinishing: false));
			Assert.Null(request.RegisteredActivity);
			Assert.Null(request.Launcher);
			await Task.Run(() => launch.Activity.DeliverResult(launch.RequestCode, Result.Ok, AnswerWith("content://files.test/answer")));
		}
		finally
		{
			release.Set();
		}

		var result = await request.Task.WaitAsync(operationTimeout);
		Assert.Equal("content://files.test/answer", result.Data?.ToString());
		Assert.Same(received, result);
		Assert.Equal("Completed", request.State);
		Assert.True(request.IsCallbackReleased);
		await AssertionExtensions.AssertEventuallyAsync(
			() => InvokeOnMainThreadAsync(() => !FilesActivityResultRegistry.RegisteredKeys(session.Activity).Contains(request.Key)),
			timeout: 5000,
			message: "The launcher that Detach handed to the UI thread was not unregistered.");
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task AResultThatIsNotAnActivityResultCancelsTheRequestWithoutCallingOnResult(bool resultIsNull)
	{
		var session = await StartSessionAsync();
		var onResultCalls = 0;
		var key = $"files.test.{Guid.NewGuid():N}";
		var request = new FilesPendingRequest(key, onResult: _ => onResultCalls++);
		FilesActivityResultManagerState.Track(key, request);
		AddCleanup(() => FilesActivityResultManagerState.Untrack(key));
		await InvokeOnMainThreadAsync(() => request.Register(session.Activity));

		await InvokeOnMainThreadAsync(() => request.DeliverToCallback(resultIsNull ? null : new Java.Lang.String("not an ActivityResult")));

		await Assert.ThrowsAsync<TaskCanceledException>(() => request.Task.WaitAsync(operationTimeout));
		Assert.Equal(0, onResultCalls);
		Assert.Equal("Completed", request.State);
		Assert.DoesNotContain(key, FilesActivityResultManagerState.PendingKeys);
		await AssertionExtensions.AssertEventuallyAsync(
			() => InvokeOnMainThreadAsync(() => !FilesActivityResultRegistry.RegisteredKeys(session.Activity).Contains(key)),
			timeout: 5000,
			message: "The launcher was not unregistered after the result.");
	}

	[Fact]
	public async Task ASecondResultForARequestThatIsAlreadyCompletedChangesNothing()
	{
		var session = await StartSessionAsync();
		var onResultCalls = 0;
		var task = await StartRequestAsync(new Intent(pickAction), onResult: _ => onResultCalls++);
		var launch = await session.WaitForLaunchAsync();
		var request = FilesPendingRequest.FromTracked(Assert.Single(FilesActivityResultManagerState.PendingKeys));
		await session.DeliverAsync(launch, Result.Ok, AnswerWith("content://files.test/first"));
		var first = await task.WaitAsync(operationTimeout);
		await AssertRegistryReleasedAsync(session.Activity);

		// Only a direct call can hand the callback a second result: the registry forgets the key once it dispatched the first one.
		await InvokeOnMainThreadAsync(() => request.DeliverToCallback(new ActivityResult((int)Result.Ok, AnswerWith("content://files.test/second"))));

		Assert.Equal("content://files.test/first", first.Data?.ToString());
		Assert.Same(first, await task);
		Assert.Equal(1, onResultCalls);
		Assert.Equal("Completed", request.State);
		Assert.Equal(0, FilesActivityResultManagerState.PendingCount);
	}

	[Fact]
	public async Task CancelingARequestThatIsNoLongerPendingChangesNothing()
	{
		await StartSessionAsync();
		using var cancellation = new CancellationTokenSource();
		var disposed = new FilesPendingRequest($"files.test.{Guid.NewGuid():N}", token: cancellation.Token);
		var canceled = new FilesPendingRequest($"files.test.{Guid.NewGuid():N}", token: cancellation.Token);
		disposed.Dispose();
		canceled.Cancel();
		Assert.Equal("Canceled", canceled.State);

		// Cancel is what the token's registration calls; the registration is disposed when a request completes, so only a race (or this
		// direct call) reaches it on a request that is no longer pending.
		disposed.Cancel();
		canceled.Cancel();

		Assert.Equal("Completed", disposed.State);
		Assert.False(disposed.Task.IsCompleted);
		Assert.Equal("Canceled", canceled.State);
		Assert.True(canceled.Task.IsCanceled);
	}

	[Fact]
	public async Task RegisteringWithAnActivityThatCannotBeUsedThrowsAndLeavesTheRequestWithoutAnActivity()
	{
		await StartSessionAsync();
		var unusable = await FilesFakePicker.CreateDetachedAsync();
		await InvokeOnMainThreadAsync(unusable.Dispose);
		var expected = Record.Exception(() => { _ = unusable.ActivityResultRegistry; });
		Assert.NotNull(expected);
		var request = new FilesPendingRequest($"files.test.{Guid.NewGuid():N}");

		var thrown = await Record.ExceptionAsync(() => InvokeOnMainThreadAsync(() => request.Register(unusable)));

		Assert.NotNull(thrown);
		Assert.IsType(expected.GetType(), thrown);
		Assert.Null(request.RegisteredActivity);
		Assert.Null(request.Launcher);
		Assert.Equal("Pending", request.State);
	}

	[Fact]
	public async Task ARequestThatCannotRegisterWithTheNewActivityFailsWithTheErrorInsteadOfWaitingForever()
	{
		var session = await StartSessionAsync();
		var key = $"files.test.{Guid.NewGuid():N}";
		var waiting = new FilesPendingRequest(key);
		FilesActivityResultManagerState.Track(key, waiting);
		AddCleanup(() => FilesActivityResultManagerState.Untrack(key));
		var unusable = await FilesFakePicker.CreateDetachedAsync();
		await InvokeOnMainThreadAsync(unusable.Dispose);
		var expected = Record.Exception(() => { _ = unusable.ActivityResultRegistry; });
		Assert.NotNull(expected);

		// A request without an activity registers with a ComponentActivity that is created (this one, built without StartAsync, does not
		// know the activity that started it, so any activity may take it; a request of StartAsync waits for the new instance of its own).
		await InvokeOnMainThreadAsync(() => FilesActivityResultManagerState.RaiseActivityStateChanged(unusable, ActivityState.Created));

		var failure = await Record.ExceptionAsync(() => waiting.Task.WaitAsync(operationTimeout));
		Assert.NotNull(failure);
		Assert.IsType(expected.GetType(), failure);
		Assert.Equal("Completed", waiting.State);
		Assert.DoesNotContain(key, FilesActivityResultManagerState.PendingKeys);
		Assert.Null(waiting.RegisteredActivity);
		Assert.NotNull(session.Activity);
	}

	[Fact]
	public async Task FailingARequestThatWasCanceledKeepsItCanceledAndRemovesIt()
	{
		await StartSessionAsync();
		using var cancellation = new CancellationTokenSource();
		var key = $"files.test.{Guid.NewGuid():N}";
		var request = new FilesPendingRequest(key, token: cancellation.Token);
		FilesActivityResultManagerState.Track(key, request);
		AddCleanup(() => FilesActivityResultManagerState.Untrack(key));
		request.RegisterCancellation();
		await cancellation.CancelAsync();
		Assert.Equal("Canceled", request.State);

		request.Fail(new InvalidOperationException("too late"));

		Assert.True(request.Task.IsCanceled);
		Assert.Equal("Completed", request.State);
		Assert.DoesNotContain(key, FilesActivityResultManagerState.PendingKeys);
	}

	// What a pending request looks like from an activity that is being created (read on the UI thread, inside its OnCreate).
	sealed record RequestSnapshot(bool HasActivity, bool RegisteredWithTheNewActivity, bool HasLauncher, bool NewRegistryHasTheCallback, string State)
	{
		public static RequestSnapshot Of(FilesPendingRequest request, ComponentActivity activity) => new(
			request.RegisteredActivity is not null,
			ReferenceEquals(request.RegisteredActivity, activity),
			request.Launcher is not null,
			FilesActivityResultRegistry.RegisteredKeys(activity).Contains(request.Key),
			request.State);
	}

	// StartAsync validates its arguments before it returns a Task, so those failures are thrown, not faulted.
	static void Start(Intent intent, CancellationToken token = default) =>
		_ = ActivityResultManager.StartAsync(intent, AndroidRequestCode.RequestCodeFolderPicker, null, token);

	static Intent AnswerWith(string uri) => new Intent().SetData(AndroidUri.Parse(uri));

	static bool IsToolkitKey(string key) => key.StartsWith(toolkitKeyPrefix, StringComparison.Ordinal);

	async Task<FilesPickerSession> StartSessionAsync()
	{
		var session = await FilesPickerSession.StartAsync();
		AddCleanup(() => session.DisposeAsync().AsTask());
		AddCleanup(() => FilesActivityResultManagerState.PendingKeys.ToList().ForEach(FilesActivityResultManagerState.Untrack));
		return session;
	}

	static async Task<Task<Intent>> StartRequestAsync(Intent intent, AndroidRequestCode requestCode = AndroidRequestCode.RequestCodeFolderPicker, Action<Intent>? onResult = null, CancellationToken token = default)
	{
		Task<Intent> task = null!;
		await InvokeOnMainThreadAsync(() => { task = ActivityResultManager.StartAsync(intent, requestCode, onResult, token); });
		return task;
	}

	static Task AssertNoPendingRequestsAsync() =>
		AssertionExtensions.AssertEventually(() => FilesActivityResultManagerState.PendingCount is 0, timeout: 5000, message: "The manager still tracks a request.");

	Task AssertRegistryReleasedAsync(ComponentActivity activity) =>
		AssertionExtensions.AssertEventuallyAsync(
			() => InvokeOnMainThreadAsync(() => !FilesActivityResultRegistry.RegisteredKeys(activity).Any(IsToolkitKey)),
			timeout: 5000,
			message: "The activity's registry still holds a callback of the toolkit.");
}