using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.DeviceTests;

// The start of the preview runs synchronously on iOS and Mac Catalyst, so a StopCameraPreview can only land while the connection yields to
// MAUI's property mapping or while a recording waits for the camera permission. The session, the camera and the permission are scripted, so
// the tests need no camera and show no prompt.
[Category(TestCategory.CameraApple)]
[Collection(TestCollections.DisableParallelization)]
public class CameraStartStopAppleTests : CameraHandlerTestBase
{
	static readonly TimeSpan timeout = TimeSpan.FromSeconds(5);

	[Fact]
	public async Task StartCameraPreviewWithACancelledTokenThrowsAndKeepsTheSessionStopped()
	{
		await RunAsync(async harness =>
		{
			using var errors = new CameraErrorRecorder(harness.Scope.View);
			using var cancellation = new CancellationTokenSource();
			await cancellation.CancelAsync();

			var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => harness.Scope.Manager.StartCameraPreview(cancellation.Token));

			Assert.Equal(cancellation.Token, exception.CancellationToken);
			Assert.Empty(harness.Session.Calls);
			Assert.Empty(harness.Inputs.Devices);
			Assert.False(harness.Scope.Access.IsInitialized);
			Assert.Equal(0, harness.Scope.LoadedCount);
			Assert.Empty(errors.Errors);
		});
	}

	[Fact]
	public async Task StopCameraPreviewWhileTheCameraConnectsKeepsTheSessionStopped()
	{
		await RunAsync(async harness =>
		{
			using var errors = new CameraErrorRecorder(harness.Scope.View);

			var connect = harness.Scope.Manager.ConnectCamera(CancellationToken.None);
			Assert.False(connect.IsCompleted, "ConnectCamera started the camera before MAUI could map the properties of the view.");
			harness.Scope.Manager.StopCameraPreview();
			await connect.WaitAsync(timeout);

			Assert.Empty(harness.Session.Calls);
			Assert.Empty(harness.Inputs.Devices);
			Assert.False(harness.Scope.Access.IsInitialized);
			Assert.Equal(0, harness.Scope.LoadedCount);
			Assert.Empty(errors.Errors);
		});
	}

	// A start that fails because of its camera leaves the preview started for the next selected camera (decision P): the stop must clear that too.
	[Fact]
	public async Task StopCameraPreviewWhileTheCameraConnectsToACameraThatCannotStartKeepsThePreviewStoppedWhenAnotherCameraIsSelected()
	{
		await RunAsync(async harness =>
		{
			using var errors = new CameraErrorRecorder(harness.Scope.View);
			harness.Scope.View.SelectedCamera = AppleFakeCameras.WithoutCaptureDevice("fake-start-stop-without-device");

			var connect = harness.Scope.Manager.ConnectCamera(CancellationToken.None);
			harness.Scope.Manager.StopCameraPreview();
			await connect.WaitAsync(timeout);
			harness.Scope.View.SelectedCamera = harness.Camera;
			await harness.Scope.Manager.UpdateCurrentCamera(harness.Camera, CancellationToken.None);

			Assert.Empty(harness.Session.Calls);
			Assert.Empty(harness.Inputs.Devices);
			Assert.False(harness.Scope.Access.IsInitialized);
			Assert.Equal(0, harness.Scope.LoadedCount);
			Assert.Empty(errors.Errors);
		});
	}

	// The real handler connects the camera as soon as it gets its platform view; the connection's start is posted to the recording context, and
	// the command stops the preview before that start runs.
	[Fact]
	public async Task StopCameraPreviewCommandRightAfterTheHandlerConnectedTheCameraKeepsThePreviewStoppedWithoutAnError()
	{
		var provider = new AppleFakeCameraProvider { AvailableCameras = [AppleFakeCameras.WithoutCaptureDevice("fake-start-stop-handler")] };
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new CameraView();
			using var errors = new CameraErrorRecorder(view);
			var context = new AppleRecordingSynchronizationContext();

			try
			{
				CameraViewHandler handler;
				using (AppleCameraProviderOverride.With(provider))
				using (context.Install())
				{
					handler = CreateHandler<CameraViewHandler>(view);
					view.StopCameraPreviewCommand.Execute(null);
				}

				Assert.Empty(context.Drain());
				Assert.Empty(errors.Errors);
				Assert.False(new CameraManagerAccessor(handler.CameraManager).IsInitialized);

				using (context.Install())
				{
					view.SelectedCamera = AppleFakeCameras.WithoutCaptureDevice("fake-start-stop-handler-next");
				}

				Assert.Empty(context.Drain());
				Assert.Empty(errors.Errors);
				Assert.False(new CameraManagerAccessor(handler.CameraManager).IsInitialized);
			}
			finally
			{
				view.Handler?.DisconnectHandler();
				view.Handler = null;
				view.Dispose();
			}
		});
	}

	[Fact]
	public async Task StopCameraPreviewWhileStartVideoRecordingWaitsForTheCameraPermissionCancelsTheRecording()
	{
		await RunAsync(async harness =>
		{
			using var permission = new AppleRequestAccessStub(videoAnswer: null);
			using var defaultDevices = new AppleDefaultDeviceStub(video: null, audio: null);
			using var destination = new MemoryStream();

			var recording = harness.Scope.Manager.StartVideoRecording(destination, CancellationToken.None);
			Assert.False(recording.IsCompleted, "StartVideoRecording did not wait for the camera permission.");
			harness.Scope.Manager.StopCameraPreview();

			var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => recording.WaitAsync(timeout));
			Assert.True(exception.CancellationToken.IsCancellationRequested, "The exception does not name the token that cancelled the recording.");
			Assert.Empty(harness.Session.Calls);
			Assert.Null(harness.Scope.Access.VideoOutput);
			Assert.Null(harness.Scope.Access.VideoRecordingStream);
		});
	}

	// AVFoundation answers on a queue of its own, so the start goes on in a later message of its own thread, which can come after a stop. The
	// recording context holds that continuation until the stop has run.
	[Fact]
	public async Task StopCameraPreviewAfterTheCameraPermissionWasGrantedButBeforeStartVideoRecordingWentOnCancelsTheRecording()
	{
		await RunAsync(async harness =>
		{
			using var permission = new AppleRequestAccessStub(videoAnswer: null);
			using var defaultDevices = new AppleDefaultDeviceStub(video: null, audio: null);
			using var destination = new MemoryStream();
			var context = new AppleRecordingSynchronizationContext();
			Task recording;

			using (context.Install())
			{
				recording = harness.Scope.Manager.StartVideoRecording(destination, CancellationToken.None);
			}

			permission.AnswerPending(isGranted: true);
			harness.Scope.Manager.StopCameraPreview();
			Assert.Empty(context.Drain());

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => recording.WaitAsync(timeout));
			Assert.Empty(harness.Session.Calls);
			Assert.Null(harness.Scope.Access.VideoOutput);
			Assert.Null(harness.Scope.Access.VideoRecordingStream);
		});
	}

	// The token of the start is linked with the token that StopCameraPreview cancels: the exception still has the type of the platform's
	// cancellation and names the caller's token.
	[Fact]
	public async Task StartVideoRecordingThatItsTokenCancelsWhileItWaitsForTheCameraPermissionThrowsWithThatToken()
	{
		await RunAsync(async harness =>
		{
			using var permission = new AppleRequestAccessStub(videoAnswer: null);
			using var destination = new MemoryStream();
			using var cancellation = new CancellationTokenSource();

			var recording = harness.Scope.Manager.StartVideoRecording(destination, cancellation.Token);
			cancellation.Cancel();

			var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => recording.WaitAsync(timeout));
			Assert.IsType<TaskCanceledException>(exception);
			Assert.Equal(cancellation.Token, exception.CancellationToken);
		});
	}

	// Without the stop, the granted permission would start the recording: the movie output that the session cannot connect fails it, and the
	// command reports that through MediaCaptureFailed.
	[Fact]
	public async Task StopCameraPreviewCommandWhileTheStartVideoRecordingCommandWaitsForTheCameraPermissionRaisesNoEvent()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new CameraView();
			var handler = CreateHandler<AppleNoConnectCameraViewHandler>(view);
			var access = new CameraManagerAccessor(handler.CameraManager);
			access.CaptureSession = new FakeCaptureSession();
			var recorder = new AppleMediaRecorder(view);
			using var errors = new CameraErrorRecorder(view);
			using var permission = new AppleRequestAccessStub(videoAnswer: null);
			using var defaultDevices = new AppleDefaultDeviceStub(video: null, audio: null);
			using var destination = new MemoryStream();
			var context = new AppleRecordingSynchronizationContext();

			try
			{
				using (context.Install())
				{
					view.StartVideoRecordingCommand.Execute(destination);
					view.StopCameraPreviewCommand.Execute(null);
				}

				permission.AnswerPending(isGranted: true);

				Assert.Empty(context.Drain());
				Assert.Empty(recorder.Failures);
				Assert.Empty(errors.Errors);
				Assert.Null(access.VideoOutput);
				GC.KeepAlive(recorder);
			}
			finally
			{
				view.Handler?.DisconnectHandler();
				view.Handler = null;
				view.Dispose();
			}
		});
	}

	async Task RunAsync(Func<Harness, Task> test)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var device = new FakeCaptureDevice { Identifier = "fake-start-stop-camera" };
			var camera = AppleFakeCameras.Create(device, CameraPosition.Rear, new FakeCaptureDeviceFormat(640, 480));
			var provider = new AppleFakeCameraProvider { AvailableCameras = [camera] };
			using var scope = new AppleCameraManagerScope(MauiContext, provider);
			scope.PlatformView = scope.Manager.CreatePlatformView();
			var session = new FakeCaptureSession();
			scope.Access.CaptureSession = session;
			using var inputs = new AppleDeviceInputStub();
			using var coordinators = new AppleRotationCoordinatorStub();

			await test(new Harness(scope, session, camera, inputs));
		});
	}

	sealed record Harness(AppleCameraManagerScope Scope, FakeCaptureSession Session, CameraInfo Camera, AppleDeviceInputStub Inputs);
}