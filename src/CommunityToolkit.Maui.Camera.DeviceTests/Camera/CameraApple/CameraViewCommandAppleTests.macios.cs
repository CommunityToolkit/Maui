using AVFoundation;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Foundation;
using ObjCRuntime;

namespace CommunityToolkit.Maui.DeviceTests;

// An exception that escaped a command would end the app: AppleRecordingSynchronizationContext receives it here. The handler does not connect
// the camera (AppleNoConnectCameraViewHandler) and every failure is scripted, so the tests need no camera and no permission.
[Category(TestCategory.CameraApple)]
[Collection(TestCollections.DisableParallelization)]
public class CameraViewCommandAppleTests : CameraHandlerTestBase
{
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

	// The session refuses to stop like AVFoundation does between beginConfiguration and commitConfiguration.
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

	// MediaCaptureFailed reports the failures of the recording commands as well as the platform's answer to a capture that failed.
	[Fact]
	public async Task CaptureImageReturnsThePhotoAlthoughARecordingCommandReportsAFailureWhileItWaits()
	{
		await RunAsync(async (view, access, context) =>
		{
			var recorder = new AppleMediaRecorder(view);
			var pendingCompletions = new List<(FakePhotoOutput Output, AVCapturePhotoCaptureDelegate Completion)>();
			access.PhotoOutput = new FakePhotoOutput { OnCapture = (fakeOutput, completion) => pendingCompletions.Add((fakeOutput, completion)) };
			using var permission = new AppleRequestAccessStub(videoAnswer: false);
			using var destination = new MemoryStream();

			var capture = view.CaptureImage(CancellationToken.None);
			var (output, completion) = Assert.Single(pendingCompletions);

			using (context.Install())
			{
				view.StartVideoRecordingCommand.Execute(destination);
			}

			Assert.Empty(context.Drain());
			Assert.Equal(["Camera permission is not granted. Please enable it in the app settings."], recorder.Failures);

			completion.DidFinishProcessingPhoto(output, new FakeCapturePhoto(() => NSData.FromArray([1, 2, 3])), null!);
			var photo = await capture.WaitAsync(TimeSpan.FromSeconds(5));

			Assert.Equal(3, photo.Length);
			Assert.Same(photo, Assert.Single(recorder.Captured));
			Assert.Equal(["Camera permission is not granted. Please enable it in the app settings."], recorder.Failures);
			Assert.Empty(context.Drain());
			GC.KeepAlive(recorder);
		});
	}

	// The recording runs on a FakeCaptureSession without a microphone, and the movie output's recording is scripted.
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