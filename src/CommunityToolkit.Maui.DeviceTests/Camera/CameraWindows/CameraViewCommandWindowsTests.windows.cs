using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.DeviceTests;

// An exception that escaped a command would end the app: WindowsRecordingSynchronizationContext receives it here. The handler does not connect
// the camera (WindowsNoConnectCameraViewHandler), so the tests need no camera.
[Category(TestCategory.CameraWindows)]
[Collection(TestCollections.DisableParallelization)]
public class CameraViewCommandWindowsTests : CameraHandlerTestBase
{
	static readonly TimeSpan asyncVoidTimeout = TimeSpan.FromSeconds(30);

	[Fact]
	public async Task CaptureImageCommandWithoutAMediaCaptureRaisesMediaCaptureFailedOnceAndLetsNothingEscape()
	{
		await RunAsync(async (view, _, context) =>
		{
			var recorder = new WindowsMediaRecorder(view);

			using (context.Install())
			{
				view.CaptureImageCommand.Execute(CancellationToken.None);
			}

			var unhandled = await context.DrainUntilAsyncVoidMethodsEndAsync(asyncVoidTimeout);

			Assert.Empty(unhandled);
			Assert.Equal(["Unable to capture an image because the camera is not initialized. Start the camera preview first."], recorder.Failures);
			Assert.Empty(recorder.Captured);
			GC.KeepAlive(recorder);
		});
	}

	// The cancelled token stops the initialization of a real camera (as in
	// CameraViewExtensionsWindowsTests.InitializeCameraForCameraViewObservesCancellation), while a camera that no device has could fail first.
	[Fact]
	public async Task StartCameraPreviewCommandWithACancelledTokenEndsWithoutAnEventAndLetsNothingEscape()
	{
		DeviceTestCapabilities.Require(DeviceTestCapabilities.Camera, await WindowsCameraEnvironment.HasVideoCaptureDeviceAsync(), $"Needs a camera whose initialization the token cancels ({await WindowsCameraEnvironment.DescribeCameraAccessAsync()}).");
		var cameras = await WindowsCameraEnvironment.DiscoverCamerasAsync();
		Skip.If(cameras.Count is 0, "The camera provider found no camera that can be opened.");

		await RunAsync(async (view, access, context) =>
		{
			using var errors = new CameraErrorRecorder(view);
			view.SelectedCamera = cameras[0];
			using var cancellationTokenSource = new CancellationTokenSource();
			await cancellationTokenSource.CancelAsync();

			using (context.Install())
			{
				view.StartCameraPreviewCommand.Execute(cancellationTokenSource.Token);
			}

			var unhandled = await context.DrainUntilAsyncVoidMethodsEndAsync(asyncVoidTimeout);

			Assert.Empty(unhandled);
			Assert.Empty(errors.Errors);
			Assert.False(access.IsInitialized);
			Assert.Null(access.MediaCapture);
		});
	}

	// MediaCapture records into an IRandomAccessStream, and Stream.AsRandomAccessStream refuses a stream that cannot seek (here a compression
	// stream), so StartVideoRecording throws NotSupportedException. The manager looks started, with a MediaCapture that is never used.
	[Fact]
	public async Task StartVideoRecordingCommandWithAStreamThatCannotSeekRaisesMediaCaptureFailedOnceAndLetsNothingEscape()
	{
		await RunAsync(async (view, access, context) =>
		{
			var recorder = new WindowsMediaRecorder(view);
			access.MediaCapture = WindowsCameraEnvironment.CreateMediaCapture();
			access.IsInitialized = true;
			using var destination = new System.IO.Compression.DeflateStream(new MemoryStream(), System.IO.Compression.CompressionMode.Compress);

			using (context.Install())
			{
				view.StartVideoRecordingCommand.Execute(destination);
			}

			var unhandled = await context.DrainUntilAsyncVoidMethodsEndAsync(asyncVoidTimeout);

			Assert.Empty(unhandled);
			Assert.False(string.IsNullOrWhiteSpace(Assert.Single(recorder.Failures)));
			Assert.Null(access.VideoCaptureStream);
			GC.KeepAlive(recorder);
		});
	}

	async Task RunAsync(Func<CameraView, WindowsCameraManagerAccessor, WindowsRecordingSynchronizationContext, Task> test)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var view = new CameraView();
			var handler = CreateHandler<WindowsNoConnectCameraViewHandler>(view);

			try
			{
				await test(view, new WindowsCameraManagerAccessor(handler.CameraManager), new WindowsRecordingSynchronizationContext());
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