using AVFoundation;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Foundation;
using ObjCRuntime;

namespace CommunityToolkit.Maui.DeviceTests;

// The commands of CameraView on iOS and Mac Catalyst. ICommand.Execute cannot return the task of the work that a command starts: an exception
// that escaped a command would reach no caller and end the app (AppleRecordingSynchronizationContext receives it here), and an app that binds
// the command in XAML cannot catch it. A command reports its failure once instead, through the event that describes it: MediaCaptureFailed for
// the capture and the recording, ErrorOccurred for the preview. The handler does not connect the camera (AppleNoConnectCameraViewHandler) and
// every failure is scripted, so the tests need no camera and no permission.
[Category(TestCategory.CameraApple)]
[Collection(TestCollections.DisableParallelization)]
public class CameraViewCommandAppleTests : CameraHandlerTestBase
{
	// The platform reports a capture that cannot start (no photo output before the preview started) through MediaCaptureFailed itself: the
	// command must not report it a second time, and the CameraException of CaptureImage must not escape.
	[Fact]
	public async Task CaptureImageCommandOfACameraThatNeverStartedRaisesMediaCaptureFailedOnceAndLetsNothingEscape()
	{
		await RunAsync((view, _, context) =>
		{
			var recorder = new AppleMediaRecorder(view);

			using (context.Install())
			{
				view.CaptureImageCommand.Execute(CancellationToken.None);
			}

			Assert.Empty(context.Drain());
			Assert.Equal(["Unable to capture an image because the camera is not initialized. Start the camera preview first."], recorder.Failures);
			Assert.Empty(recorder.Captured);
			GC.KeepAlive(recorder);

			return Task.CompletedTask;
		});
	}

	// StartCameraPreview reports a preview that cannot start through ErrorOccurred itself: the command must not report it a second time.
	[Fact]
	public async Task StartCameraPreviewCommandOfACameraThatCannotStartRaisesErrorOccurredOnceAndLetsNothingEscape()
	{
		await RunAsync((view, _, context) =>
		{
			using var errors = new CameraErrorRecorder(view);
			view.SelectedCamera = AppleFakeCameras.WithoutCaptureDevice("fake-command-without-device");

			using (context.Install())
			{
				view.StartCameraPreviewCommand.Execute(CancellationToken.None);
			}

			Assert.Empty(context.Drain());
			Assert.Equal("No Camera found", Assert.IsType<CameraException>(Assert.Single(errors.Errors)).Message);

			return Task.CompletedTask;
		});
	}

	// StopCameraPreviewCommand is a synchronous Command: an exception would escape to the caller of ICommand.Execute, which is MAUI when the
	// command is bound in XAML. The session refuses to stop like AVFoundation does between beginConfiguration and commitConfiguration.
	[Fact]
	public async Task StopCameraPreviewCommandThatTheSessionRefusesRaisesErrorOccurredOnceAndLetsNothingEscape()
	{
		await RunAsync((view, access, context) =>
		{
			using var errors = new CameraErrorRecorder(view);
			var refusal = new ObjCException(new NSException("NSGenericException", "*** -[AVCaptureSession stopRunning] stopRunning may not be called between calls to beginConfiguration and commitConfiguration", null));
			var session = new FakeCaptureSession { IsRunning = true };
			session.Failures["stop"] = refusal;
			access.CaptureSession = session;
			access.IsInitialized = true;
			Exception? escaped;

			using (context.Install())
			{
				escaped = Record.Exception(() => view.StopCameraPreviewCommand.Execute(null));
			}

			// The handler stops the session again when it is disconnected.
			session.Failures.Clear();
			Assert.Null(escaped);
			Assert.Empty(context.Drain());
			Assert.Same(refusal, Assert.Single(errors.Errors));

			return Task.CompletedTask;
		});
	}

	// StartVideoRecording throws CameraException without the camera permission (the prompt's answer is scripted).
	[Fact]
	public async Task StartVideoRecordingCommandWithoutTheCameraPermissionRaisesMediaCaptureFailedOnceAndLetsNothingEscape()
	{
		await RunAsync((view, access, context) =>
		{
			var recorder = new AppleMediaRecorder(view);
			using var permission = new AppleRequestAccessStub(videoAnswer: false);
			using var destination = new MemoryStream();

			using (context.Install())
			{
				view.StartVideoRecordingCommand.Execute(destination);
			}

			Assert.Empty(context.Drain());
			Assert.Equal(["Camera permission is not granted. Please enable it in the app settings."], recorder.Failures);
			Assert.Null(access.VideoOutput);
			GC.KeepAlive(recorder);

			return Task.CompletedTask;
		});
	}

	// AVFoundation ended the recording with an error before the app stopped it (here the disk filled up) and wrote no movie: StopVideoRecording
	// throws CameraException. The recording runs on a FakeCaptureSession without a microphone, and the movie output's recording is scripted.
	[Fact]
	public async Task StopVideoRecordingCommandOfARecordingThatFailedRaisesMediaCaptureFailedOnceAndLetsNothingEscape()
	{
		await RunAsync(async (view, access, context) =>
		{
			var recorder = new AppleMediaRecorder(view);
			access.CaptureSession = new FakeCaptureSession();
			using var permission = new AppleRequestAccessStub(videoAnswer: true);
			using var defaultDevices = new AppleDefaultDeviceStub(video: null, audio: null);
			using var connections = new AppleMovieOutputConnectionStub(new FakeCaptureConnection());
			using var movies = new AppleMovieRecordingStub { MovieBytes = null };
			using var destination = new MemoryStream();
			await view.StartVideoRecording(destination, CancellationToken.None);
			movies.Finish(Assert.Single(movies.Recordings), CreateDiskFullError());

			using (context.Install())
			{
				view.StopVideoRecordingCommand.Execute(CancellationToken.None);
			}

			Assert.Empty(context.Drain());
			Assert.Equal(["Video recording failed: The recording could not be completed."], recorder.Failures);
			Assert.Equal(0, destination.Length);
			Assert.Null(access.VideoOutput);
			GC.KeepAlive(recorder);
		});
	}

	// The error with which AVFoundation reports a recording that stopped without a usable movie because the disk is full.
	static NSError CreateDiskFullError()
	{
		var userInfo = NSDictionary.FromObjectsAndKeys(
			new NSObject[] { NSNumber.FromBoolean(false), new NSString("The recording could not be completed.") },
			new NSObject[] { AVErrorKeys.RecordingSuccessfullyFinished, NSError.LocalizedDescriptionKey });

		return new NSError(AVErrorKeys.ErrorDomain, (nint)(long)AVError.DiskFull, userInfo);
	}

	// Runs the test on the UI thread with a new CameraView whose handler does not connect the camera, and a recording SynchronizationContext for
	// the commands. The handler is disconnected afterwards (the manager's Dispose stops and releases the session).
	async Task RunAsync(Func<CameraView, CameraManagerAccessor, AppleRecordingSynchronizationContext, Task> test)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var view = new CameraView();
			var handler = CreateHandler<AppleNoConnectCameraViewHandler>(view);

			try
			{
				await test(view, new CameraManagerAccessor(handler.CameraManager), new AppleRecordingSynchronizationContext());
			}
			finally
			{
				view.Handler?.DisconnectHandler();
				view.Handler = null;
				view.Dispose();
			}
		});
	}
}