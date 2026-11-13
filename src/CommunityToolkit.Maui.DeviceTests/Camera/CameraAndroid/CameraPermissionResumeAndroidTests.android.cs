using System.Runtime.CompilerServices;
using Android.App;
using Android.Content;
using AndroidX.Camera.View;
using AndroidX.Lifecycle;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.DeviceTests;

// Revoking the camera permission would kill the test app, so a ConfigurableContext denies it to the manager while the app keeps it. Since
// Android 16 QPR2 (API 36.1) Activity.requestPermissions answers a request for permissions that the app already has without showing the
// permission activity, so the tests pause and resume the runner's activity with a translucent activity of their own, which finishes at once.
[Category(TestCategory.CameraPermissionResumeAndroid)]
[Collection(TestCollections.PermanentAndroidGrants)]
public class CameraPermissionResumeAndroidTests : CameraAndroidTestBase
{
	const string cameraPermissionNotGrantedMessage = "Camera permission is not granted. Please enable it in the app settings.";

	[Fact]
	public async Task ACameraViewThatConnectedWithoutTheCameraPermissionStartsItsPreviewWhenTheActivityResumesWithIt()
	{
		SetupBuilder();
		EnsureCameraPermissions();
		var context = new ConfigurableContext(AppContext);
		context.DeniedPermissions.Add(CameraPermission);
		var view = new CameraView();
		using var errors = new CameraErrorRecorder(view);

		var handler = await InvokeOnMainThreadAsync(() =>
		{
			var created = CreateHandler<CameraViewHandler>(view, new TestMauiContext(MauiContext, context));
			RegisterHandlerCleanup(created);
			return created;
		});
		PreviewView previewView = handler.PlatformView;

		await HostAsync(async () =>
		{
			await AssertionExtensions.AssertEventually(() => errors.Errors.Count > 0, 30000, 50, "The connection without the camera permission was not reported.");
			Assert.Equal(cameraPermissionNotGrantedMessage, Assert.IsType<CameraException>(Assert.Single(errors.Errors)).Message);
			Assert.False(IsStreaming(previewView));

			context.DeniedPermissions.Clear();
			await PauseAndResumeTheActivityAsync();

			await WaitForStreamingAsync(previewView, errors: errors);
			Assert.Single(errors.Errors);
			Assert.Empty(UnhandledAsyncVoidExceptions);
		}, previewView);
	}

	[Fact]
	public async Task AResumeWithoutTheCameraPermissionKeepsThePreviewStoppedAndALaterResumeWithItStartsThePreview()
	{
		var (context, provider) = await SetupWithoutTheCameraPermissionAsync();

		await RunWithSessionAsync(async session =>
		{
			using var errors = await ConnectWithoutTheCameraPermissionAsync(session);

			await PauseAndResumeTheActivityAsync();
			await WaitForTheListenersOfCameraXAsync();

			Assert.Single(errors.Errors);
			await AssertThePreviewIsStoppedAsync(session);

			context.DeniedPermissions.Clear();
			await PauseAndResumeTheActivityAsync();

			await WaitForNativeCameraStateAsync(session.View.SelectedCamera?.DeviceId ?? throw new InvalidOperationException("No camera is selected."), "OPEN");
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView ?? throw new InvalidOperationException("The session has no PreviewView."));
			Assert.Equal(1, session.LoadedCount);
			Assert.Single(errors.Errors);
		}, provider, new TestMauiContext(MauiContext, context));
	}

	[Fact]
	public async Task AStartThatFailedForAnotherReasonIsNotRetriedWhenTheActivityResumes()
	{
		var (context, provider) = await SetupWithoutTheCameraPermissionAsync();

		await RunWithSessionAsync(async session =>
		{
			using var errors = await ConnectWithoutTheCameraPermissionAsync(session);
			context.DeniedPermissions.Clear();
			session.View.SelectedCamera = CreateCameraThatCanNotBeBound();
			await session.Manager.StartCameraPreview(CancellationToken.None).WaitAsync(CameraTimeout);
			Assert.Equal(2, errors.Errors.Count);

			await PauseAndResumeTheActivityAsync();
			await WaitForTheListenersOfCameraXAsync();

			Assert.Equal(2, errors.Errors.Count);
			Assert.Equal(0, session.LoadedCount);
			Assert.False(await IsNativelyBoundAsync(session.CameraPreview), "The resume started the preview after a start that failed for another reason than the camera permission.");
		}, provider, new TestMauiContext(MauiContext, context));
	}

	// The app has the permission by the time of the later start, so only the retry of the start of the connection could start the preview when
	// the activity resumes.
	[Fact]
	public async Task ALaterStartThatItsTokenCancelledKeepsThePreviewStoppedWhenTheActivityResumesWithTheCameraPermission()
	{
		var (context, provider) = await SetupWithoutTheCameraPermissionAsync();

		await RunWithSessionAsync(async session =>
		{
			using var errors = await ConnectWithoutTheCameraPermissionAsync(session);
			context.DeniedPermissions.Clear();
			using var cancellation = new CancellationTokenSource();
			await cancellation.CancelAsync();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.Manager.StartCameraPreview(cancellation.Token).WaitAsync(CameraTimeout));

			await PauseAndResumeTheActivityAsync();
			await WaitForTheListenersOfCameraXAsync();

			Assert.Single(errors.Errors);
			await AssertThePreviewIsStoppedAsync(session);
		}, provider, new TestMauiContext(MauiContext, context));
	}

	[Fact]
	public async Task StopCameraPreviewAfterAStartWithoutTheCameraPermissionKeepsThePreviewStoppedWhenTheActivityResumesWithIt()
	{
		var (context, provider) = await SetupWithoutTheCameraPermissionAsync();

		await RunWithSessionAsync(async session =>
		{
			using var errors = await ConnectWithoutTheCameraPermissionAsync(session);
			session.Manager.StopCameraPreview();
			context.DeniedPermissions.Clear();

			await PauseAndResumeTheActivityAsync();
			await WaitForTheListenersOfCameraXAsync();

			Assert.Single(errors.Errors);
			await AssertThePreviewIsStoppedAsync(session);
		}, provider, new TestMauiContext(MauiContext, context));
	}

	// The manager observed the activity first, so it starts the preview before the test's observer of the same resume runs: the stop lands while
	// that start waits for CameraX, which answers in a later UI-thread message.
	[Fact]
	public async Task StopCameraPreviewWhileTheStartThatTheResumeRetriesWaitsForCameraXKeepsThePreviewStopped()
	{
		var (context, provider) = await SetupWithoutTheCameraPermissionAsync();

		await RunWithSessionAsync(async session =>
		{
			using var errors = await ConnectWithoutTheCameraPermissionAsync(session);
			context.DeniedPermissions.Clear();
			bool? wasStillWaitingForThePermission = null;

			await PauseAndResumeTheActivityAsync(() =>
			{
				wasStillWaitingForThePermission = GetPrivateField<bool>(session.Manager, "isWaitingForCameraPermission");
				session.Manager.StopCameraPreview();
			});
			await WaitForTheListenersOfCameraXAsync();

			// Had the manager not retried the start first, the stop would have cancelled nothing and the test would pass without the cancellation.
			Assert.False(wasStillWaitingForThePermission, "The resume did not retry the start before the test stopped the preview.");
			Assert.Single(errors.Errors);
			await AssertThePreviewIsStoppedAsync(session);
		}, provider, new TestMauiContext(MauiContext, context));
	}

	// The connection checks the camera permission in the listener of CameraX, which runs in a later UI-thread message than the stop and before
	// the test grants the permission: had the cancelled start still checked it, the resume would have retried that start.
	[Fact]
	public async Task StopCameraPreviewWhileTheViewConnectsWithoutTheCameraPermissionKeepsThePreviewStoppedWhenTheActivityResumesWithIt()
	{
		var (context, provider) = await SetupWithoutTheCameraPermissionAsync();

		await RunWithSessionAsync(async session =>
		{
			using var errors = new CameraErrorRecorder(session.View);
			await session.Manager.UpdateCaptureResolution(Size.Zero, CancellationToken.None);

			var connect = session.Manager.ConnectCamera(CancellationToken.None);
			Assert.False(connect.IsCompleted, "ConnectCamera completed without waiting for CameraX.");
			session.Manager.StopCameraPreview();
			await connect.WaitAsync(CameraTimeout);
			await WaitForTheListenersOfCameraXAsync();
			Assert.Empty(errors.Errors);

			context.DeniedPermissions.Clear();
			await PauseAndResumeTheActivityAsync();
			await WaitForTheListenersOfCameraXAsync();

			Assert.Empty(errors.Errors);
			await AssertThePreviewIsStoppedAsync(session);
		}, provider, new TestMauiContext(MauiContext, context));
	}

	// The app starts the preview itself as soon as it has the permission, in the same resume that makes the manager retry its start: the app's
	// start comes while the retry still waits for CameraX, and binding the camera for both would load the view twice.
	[Fact]
	public async Task AStartOfTheAppWhileTheStartThatTheResumeRetriesWaitsForCameraXJoinsItAndLoadsTheViewOnce()
	{
		var (context, provider) = await SetupWithoutTheCameraPermissionAsync();

		await RunWithSessionAsync(async session =>
		{
			using var errors = await ConnectWithoutTheCameraPermissionAsync(session);
			context.DeniedPermissions.Clear();
			Task? startOfTheApp = null;

			await PauseAndResumeTheActivityAsync(() => startOfTheApp = session.Manager.StartCameraPreview(CancellationToken.None));

			await (startOfTheApp ?? throw new InvalidOperationException("The test's observer did not run.")).WaitAsync(CameraTimeout);
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView ?? throw new InvalidOperationException("The session has no PreviewView."));
			await WaitForTheListenersOfCameraXAsync();

			Assert.Equal(1, session.LoadedCount);
			Assert.Single(errors.Errors);
			Assert.Empty(UnhandledAsyncVoidExceptions);
		}, provider, new TestMauiContext(MauiContext, context));
	}

	[Fact]
	public async Task AStartThatTheResumeRetriesAndThatFailsRaisesErrorOccurred()
	{
		var (context, provider) = await SetupWithoutTheCameraPermissionAsync();

		await RunWithSessionAsync(async session =>
		{
			using var errors = await ConnectWithoutTheCameraPermissionAsync(session);
			context.DeniedPermissions.Clear();
			session.View.SelectedCamera = CreateCameraThatCanNotBeBound();

			await PauseAndResumeTheActivityAsync();
			await AssertionExtensions.AssertEventually(() => errors.Errors.Count > 1, 30000, 50, "The failure of the retried start was not reported.");

			Assert.IsType<Java.Lang.IllegalArgumentException>(errors.Errors[1], exactMatch: false);
			Assert.Equal(2, errors.Errors.Count);
			Assert.Equal(0, session.LoadedCount);
			Assert.Empty(UnhandledAsyncVoidExceptions);
		}, provider, new TestMauiContext(MauiContext, context));
	}

	// The connection reports its failure to the view: the start that waited for the connection and then fails the same way throws that failure
	// to its caller, so the app that awaits the start and listens to ErrorOccurred hears about one failure once.
	[Fact]
	public async Task AStartThatWaitsForTheConnectionWithoutTheCameraPermissionThrowsWithoutRaisingErrorOccurredASecondTime()
	{
		var (context, provider) = await SetupWithoutTheCameraPermissionAsync();

		await RunWithSessionAsync(async session =>
		{
			using var errors = new CameraErrorRecorder(session.View);
			await session.Manager.UpdateCaptureResolution(Size.Zero, CancellationToken.None);

			var connect = session.Manager.ConnectCamera(CancellationToken.None);
			var start = session.Manager.StartCameraPreview(CancellationToken.None);
			Assert.False(start.IsCompleted, "StartCameraPreview completed without waiting for the connection.");

			await connect.WaitAsync(CameraTimeout);
			var exception = await Assert.ThrowsAsync<CameraException>(() => start.WaitAsync(CameraTimeout));
			await WaitForTheListenersOfCameraXAsync();

			Assert.Equal(cameraPermissionNotGrantedMessage, exception.Message);
			Assert.Equal(cameraPermissionNotGrantedMessage, Assert.IsType<CameraException>(Assert.Single(errors.Errors)).Message);
			Assert.Equal(0, session.LoadedCount);
		}, provider, new TestMauiContext(MauiContext, context));
	}

	// The command reports the failures that StartCameraPreview throws, but not the failure that the connection reported and the start threw again.
	[Fact]
	public async Task StartCameraPreviewCommandThatWaitsForTheConnectionWithoutTheCameraPermissionRaisesErrorOccurredOnce()
	{
		SetupBuilder();
		EnsureCameraPermissions();
		var provider = new GatedCameraProvider(await GetRealCamerasAsync());
		UseCameraProvider(provider);
		var context = new ConfigurableContext(AppContext);
		context.DeniedPermissions.Add(CameraPermission);
		var view = new CameraView();
		using var errors = new CameraErrorRecorder(view);

		var handler = await InvokeOnMainThreadAsync(() =>
		{
			var created = CreateHandler<CameraViewHandler>(view, new TestMauiContext(MauiContext, context));
			RegisterHandlerCleanup(created);
			return created;
		});
		PreviewView previewView = handler.PlatformView;

		await HostAsync(async () =>
		{
			// The connection waits for the camera discovery, which the test holds back: the start of the command waits for the connection
			await provider.RefreshStarted.WaitAsync(CameraTimeout);
			await InvokeOnMainThreadAsync(() => view.StartCameraPreviewCommand.Execute(CancellationToken.None));
			provider.Release();

			await AssertionExtensions.AssertEventually(() => errors.Errors.Count > 0, 30000, 50, "The connection without the camera permission was not reported.");
			await WaitForTheListenersOfCameraXAsync();
			await SettleUiThreadAsync();

			Assert.Equal(cameraPermissionNotGrantedMessage, Assert.IsType<CameraException>(Assert.Single(errors.Errors)).Message);
			Assert.Empty(UnhandledAsyncVoidExceptions);
		}, previewView);
	}

	async Task<(ConfigurableContext Context, StubCameraProvider Provider)> SetupWithoutTheCameraPermissionAsync([CallerMemberName] string? testName = null)
	{
		SetupBuilder(testName: testName);
		EnsureCameraPermissions();
		var provider = new StubCameraProvider { AvailableCameras = await GetRealCamerasAsync() };
		var context = new ConfigurableContext(AppContext);
		context.DeniedPermissions.Add(CameraPermission);

		return (context, provider);
	}

	static async Task<CameraErrorRecorder> ConnectWithoutTheCameraPermissionAsync(ManagerSession session)
	{
		var errors = new CameraErrorRecorder(session.View);

		await session.ConnectAsync().WaitAsync(CameraTimeout);

		Assert.Equal(cameraPermissionNotGrantedMessage, Assert.IsType<CameraException>(Assert.Single(errors.Errors)).Message);
		Assert.Equal(0, session.LoadedCount);
		return errors;
	}

	static async Task AssertThePreviewIsStoppedAsync(ManagerSession session)
	{
		Assert.Equal(0, session.LoadedCount);
		Assert.False(await IsNativelyBoundAsync(session.CameraPreview), "The resume started the preview.");
		await AssertEveryCameraIsClosedAsync();
	}

	// Observers hear an event in the order in which they started to observe the lifecycle, so onResumed runs after the manager heard the resume.
	static async Task PauseAndResumeTheActivityAsync(Action? onResumed = null)
	{
		var activity = (Activity)AppContext;
		var lifecycle = ((ILifecycleOwner)activity).Lifecycle;
		using var observer = new PauseThenResumeObserver(onResumed);

		await InvokeOnMainThreadAsync(() => lifecycle.AddObserver(observer));
		try
		{
			await InvokeOnMainThreadAsync(() => activity.StartActivity(new Intent(activity, typeof(TranslucentPauseActivity))));

			await observer.Paused.WaitAsync(CameraTimeout);
			await observer.Resumed.WaitAsync(CameraTimeout);
		}
		finally
		{
			await InvokeOnMainThreadAsync(() => lifecycle.RemoveObserver(observer));
		}
	}

	// Observing a lifecycle replays its events up to its current state, so a resume only counts once the activity paused.
	sealed class PauseThenResumeObserver(Action? onResumed) : Java.Lang.Object, ILifecycleEventObserver
	{
		readonly TaskCompletionSource paused = new(TaskCreationOptions.RunContinuationsAsynchronously);
		readonly TaskCompletionSource resumed = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public Task Paused => paused.Task;

		public Task Resumed => resumed.Task;

		public void OnStateChanged(ILifecycleOwner source, Lifecycle.Event e)
		{
			if (e.Equals(Lifecycle.Event.OnPause))
			{
				paused.TrySetResult();
			}
			else if (paused.Task.IsCompleted && !resumed.Task.IsCompleted && e.Equals(Lifecycle.Event.OnResume))
			{
				onResumed?.Invoke();
				resumed.TrySetResult();
			}
		}
	}
}

sealed class GatedCameraProvider(IReadOnlyList<CommunityToolkit.Maui.Core.CameraInfo> cameras) : CommunityToolkit.Maui.Core.ICameraProvider
{
	readonly TaskCompletionSource refreshStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
	readonly TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

	public IReadOnlyList<CommunityToolkit.Maui.Core.CameraInfo>? AvailableCameras { get; private set; }

	public Task RefreshStarted => refreshStarted.Task;

	public event EventHandler<IReadOnlyList<CommunityToolkit.Maui.Core.CameraInfo>?>? AvailableCamerasChanged;

	public async Task RefreshAvailableCameras(CancellationToken token)
	{
		refreshStarted.TrySetResult();
		await gate.Task.WaitAsync(token);
		AvailableCameras = cameras;
		AvailableCamerasChanged?.Invoke(this, AvailableCameras);
	}

	public void Release() => gate.TrySetResult();

	public void Dispose()
	{
	}
}