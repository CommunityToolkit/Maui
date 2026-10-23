using System.Runtime.CompilerServices;
using Android.App;
using AndroidX.Camera.View;
using AndroidX.Core.App;
using AndroidX.Lifecycle;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.DeviceTests;

// Revoking the camera permission would kill the test app, so a ConfigurableContext denies it to the manager while the app keeps it. Asking for
// the permission that the app has still shows the permission activity in front of the runner's, which finishes at once: the runner's activity
// pauses and resumes as it does behind the permission dialog.
[Category(TestCategory.CameraPermissionResumeAndroid)]
[Collection(TestCollections.DisableParallelization)]
public class CameraPermissionResumeAndroidTests : CameraAndroidTestBase
{
	const string cameraPermissionNotGrantedMessage = "Camera permission is not granted. Please enable it in the app settings.";
	const int cameraPermissionRequestCode = 4321;

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
			await AskForTheCameraPermissionAsync();

			await WaitForStreamingAsync(previewView);
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

			await AskForTheCameraPermissionAsync();
			await WaitForTheListenersOfCameraXAsync();

			Assert.Single(errors.Errors);
			await AssertThePreviewIsStoppedAsync(session);

			context.DeniedPermissions.Clear();
			await AskForTheCameraPermissionAsync();

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

			await AskForTheCameraPermissionAsync();
			await WaitForTheListenersOfCameraXAsync();

			Assert.Equal(2, errors.Errors.Count);
			Assert.Equal(0, session.LoadedCount);
			Assert.False(await IsNativelyBoundAsync(session.CameraPreview), "The resume started the preview after a start that failed for another reason than the camera permission.");
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

			await AskForTheCameraPermissionAsync();
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

			await AskForTheCameraPermissionAsync(() =>
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

	[Fact]
	public async Task AStartThatTheResumeRetriesAndThatFailsRaisesErrorOccurred()
	{
		var (context, provider) = await SetupWithoutTheCameraPermissionAsync();

		await RunWithSessionAsync(async session =>
		{
			using var errors = await ConnectWithoutTheCameraPermissionAsync(session);
			context.DeniedPermissions.Clear();
			session.View.SelectedCamera = CreateCameraThatCanNotBeBound();

			await AskForTheCameraPermissionAsync();
			await AssertionExtensions.AssertEventually(() => errors.Errors.Count > 1, 30000, 50, "The failure of the retried start was not reported.");

			Assert.IsType<Java.Lang.IllegalArgumentException>(errors.Errors[1], exactMatch: false);
			Assert.Equal(2, errors.Errors.Count);
			Assert.Equal(0, session.LoadedCount);
			Assert.Empty(UnhandledAsyncVoidExceptions);
		}, provider, new TestMauiContext(MauiContext, context));
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
	static async Task AskForTheCameraPermissionAsync(Action? onResumed = null)
	{
		var activity = (Activity)AppContext;
		var lifecycle = ((ILifecycleOwner)activity).Lifecycle;
		using var observer = new PauseThenResumeObserver(onResumed);

		await InvokeOnMainThreadAsync(() => lifecycle.AddObserver(observer));
		try
		{
			await InvokeOnMainThreadAsync(() => ActivityCompat.RequestPermissions(activity, [CameraPermission], cameraPermissionRequestCode));

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
