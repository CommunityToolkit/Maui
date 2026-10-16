using AVFoundation;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Views;
using ObjCRuntime;

namespace CommunityToolkit.Maui.DeviceTests;

// The capture paths of CameraManager.macios.cs that need a camera AND the camera permission: starting the preview, taking a photo,
// recording a movie, switching cameras and the real CameraViewHandler. They run only when the app already has the camera permission
// (a headless run cannot answer the system prompt) and the machine has a camera, e.g. on a Mac after the permission was granted once
// with CameraAppleUserInteractionTests; everywhere else, including the simulators and the CI runners, they skip with the reason.
[Category(TestCategory.CameraApple)]
[Collection(TestCollections.DisableParallelization)]
public class CameraManagerAppleCaptureTests : CameraHandlerTestBase
{
	static readonly TimeSpan captureTimeout = TimeSpan.FromSeconds(20);

	[Fact]
	public async Task ConnectCameraStartsARunningSessionWithTheSelectedCameraAndAPhotoOutput()
	{
		await RunWithConnectedCameraAsync((scope, camera) =>
		{
			var session = scope.Access.CaptureSession ?? throw new InvalidOperationException("No capture session.");

			Assert.True(session.Running);
			var input = Assert.IsType<AVCaptureDeviceInput>(Assert.Single(session.Inputs), exactMatch: false);
			Assert.Equal(camera.DeviceId, input.Device.UniqueID);
			Assert.Equal(camera.DeviceId, scope.Access.CaptureDevice?.UniqueID);
			Assert.Single(session.Outputs.OfType<AVCapturePhotoOutput>());
			Assert.Contains(scope.Access.PhotoOutput, session.Outputs);
			Assert.True(scope.Access.IsInitialized);
			Assert.Equal(1, scope.LoadedCount);
			Assert.NotNull(scope.Access.RotationCoordinator);

			return Task.CompletedTask;
		});
	}

	[Fact]
	public async Task StopCameraPreviewStopsTheSessionAndStartCameraPreviewResumesItWithoutDuplicatingTheOutput()
	{
		await RunWithConnectedCameraAsync(async (scope, camera) =>
		{
			var session = scope.Access.CaptureSession ?? throw new InvalidOperationException("No capture session.");
			var photoOutput = scope.Access.PhotoOutput;

			scope.Manager.StopCameraPreview();
			Assert.False(session.Running);
			Assert.False(scope.Access.IsInitialized);

			await scope.Manager.StartCameraPreview(CancellationToken.None);

			Assert.True(session.Running);
			Assert.True(scope.Access.IsInitialized);
			Assert.Equal(2, scope.LoadedCount);
			Assert.Single(session.Inputs);
			Assert.Single(session.Outputs.OfType<AVCapturePhotoOutput>());
			Assert.Same(photoOutput, scope.Access.PhotoOutput);
			Assert.Equal(camera.DeviceId, Assert.IsType<AVCaptureDeviceInput>(session.Inputs[0], exactMatch: false).Device.UniqueID);
		});
	}

	[Fact]
	public async Task ARunningPreviewHasAConnectionThatFollowsTheWindowOrientation()
	{
		await RunWithConnectedCameraAsync((scope, _) =>
		{
			var platformView = scope.PlatformView ?? throw new InvalidOperationException("No platform view.");
			var previewLayer = Assert.IsType<AVCaptureVideoPreviewLayer>(platformView.Layer, exactMatch: false);
			var connection = previewLayer.Connection;
			Assert.NotNull(connection);

			AppleCameraEnvironment.PostOrientationDidChangeNotification();
			platformView.SetNeedsLayout();
			platformView.LayoutIfNeeded();

			Assert.NotNull(previewLayer.Connection);
			Assert.True(scope.Access.TryConfigureAVCaptureConnection(Assert.IsType<AVCapturePhotoOutput>(scope.Access.PhotoOutput), out var errorMessage), errorMessage);

			return Task.CompletedTask;
		});
	}

	[Fact]
	public async Task TakePictureRaisesMediaCapturedWithJpegData()
	{
		await RunWithConnectedCameraAsync(async (scope, _) =>
		{
			var recorder = new AppleMediaRecorder(scope.View);

			await scope.Manager.TakePicture(CancellationToken.None);
			var stream = await recorder.FirstCapture.Task.WaitAsync(captureTimeout);

			Assert.True(stream.Length > 1000);
			Assert.Equal(0xFF, stream.ReadByte());
			Assert.Equal(0xD8, stream.ReadByte());
			GC.KeepAlive(recorder);
		});
	}

	[Theory]
	[InlineData(CameraFlashMode.Off)]
	[InlineData(CameraFlashMode.On)]
	[InlineData(CameraFlashMode.Auto)]
	public async Task TakePictureWithAnyFlashModeFallsBackToASupportedModeAndCaptures(CameraFlashMode flashMode)
	{
		await RunWithConnectedCameraAsync(async (scope, _) =>
		{
			var recorder = new AppleMediaRecorder(scope.View);
			scope.Manager.UpdateFlashMode(flashMode);

			await scope.Manager.TakePicture(CancellationToken.None);
			var stream = await recorder.FirstCapture.Task.WaitAsync(captureTimeout);

			Assert.True(stream.Length > 1000);
			GC.KeepAlive(recorder);
		});
	}

	[Fact]
	public async Task StartAndStopVideoRecordingCopiesTheRecordedMovieIntoTheStream()
	{
		Skip.If(AVCaptureDevice.GetAuthorizationStatus(AVAuthorizationMediaType.Audio) is AVAuthorizationStatus.NotDetermined,
			"Starting a recording creates an audio input, which would show the microphone permission prompt that a headless run cannot answer.");

		await RunWithConnectedCameraAsync(async (scope, _) =>
		{
			using var destination = new MemoryStream();

			await scope.Manager.StartVideoRecording(destination, CancellationToken.None);
			var movieOutput = scope.Access.VideoOutput ?? throw new InvalidOperationException("No movie output.");
			var recordedFile = scope.Access.VideoRecordingFileName ?? throw new InvalidOperationException("No recording file.");
			await AssertHelpers.AssertEventually(() => movieOutput.Recording && movieOutput.RecordedDuration.Seconds > 0.3, timeout: 10000, message: "The movie output did not start recording.");

			var result = await scope.Manager.StopVideoRecording(CancellationToken.None);

			Assert.Same(destination, result);
			Assert.Equal(0, result.Position);
			Assert.True(destination.Length > 100);
			var header = destination.ToArray().AsSpan(4, 4);
			Assert.Equal("ftyp"u8.ToArray(), header.ToArray());
			Assert.False(File.Exists(recordedFile));
			Assert.Null(scope.Access.VideoOutput);
		});
	}

	[Fact]
	public async Task UpdateCurrentCameraSwitchesThePreviewToTheOtherCamera()
	{
		var cameras = await AppleCameraEnvironment.DiscoverCamerasAsync();
		DeviceTestCapabilities.Require(DeviceTestCapabilities.SecondCamera, cameras.Count >= 2, $"Needs two cameras (found {cameras.Count}).");

		await RunWithConnectedCameraAsync(async (scope, first) =>
		{
			var other = cameras.First(camera => camera.DeviceId != first.DeviceId);
			scope.View.SelectedCamera = other;

			await scope.Manager.UpdateCurrentCamera(other, CancellationToken.None);

			var session = scope.Access.CaptureSession ?? throw new InvalidOperationException("No capture session.");
			Assert.True(session.Running);
			Assert.Equal(other.DeviceId, Assert.IsType<AVCaptureDeviceInput>(Assert.Single(session.Inputs), exactMatch: false).Device.UniqueID);
			Assert.Equal(other.DeviceId, scope.Access.CaptureDevice?.UniqueID);
			Assert.Equal(2, scope.LoadedCount);
		});
	}

	[Fact]
	public async Task TheHandlerConnectsTheCameraByItselfAndCaptureImageReturnsAJpeg()
	{
		SkipUnlessACameraIsAuthorized();
		SetupBuilder();
		var view = new CameraView { WidthRequest = 320, HeightRequest = 240 };

		await AttachAndRun<CameraViewHandler>(view, async handler =>
		{
			await view.OnLoadedAsync();
			var access = new CameraManagerAccessor(handler.CameraManager);
			await AssertHelpers.AssertEventually(() => access.CaptureSession?.Running is true, timeout: 20000, message: "The handler did not start the camera session.");

			Assert.True(view.IsAvailable);
			Assert.Equal(1f, view.ZoomFactor);
			Assert.NotNull(view.SelectedCamera);
			var stream = await view.CaptureImage(CancellationToken.None).WaitAsync(captureTimeout);
			Assert.True(stream.Length > 1000);
			Assert.Equal(0xFF, stream.ReadByte());
			Assert.Equal(0xD8, stream.ReadByte());
		});

		await InvokeOnMainThreadAsync(async () =>
		{
			await view.OnUnloadedAsync();
			var session = new CameraManagerAccessor(((CameraViewHandler)view.Handler!).CameraManager).CaptureSession;
			view.Handler!.DisconnectHandler();
			view.Handler = null;
			Assert.Equal(NativeHandle.Zero, session?.Handle);
		});
	}

	async Task RunWithConnectedCameraAsync(Func<AppleCameraManagerScope, CameraInfo, Task> test)
	{
		SkipUnlessACameraIsAuthorized();
		var cameras = await AppleCameraEnvironment.DiscoverCamerasAsync();
		Skip.If(cameras.Count is 0, "The camera provider found no camera.");
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var provider = new AppleFakeCameraProvider { AvailableCameras = cameras };
			using var scope = new AppleCameraManagerScope(MauiContext, provider);
			scope.PlatformView = scope.Manager.CreatePlatformView();

			try
			{
				await scope.Manager.ConnectCamera(CancellationToken.None);
				await test(scope, scope.View.SelectedCamera ?? throw new InvalidOperationException("ConnectCamera selected no camera."));
			}
			finally
			{
				scope.Manager.StopCameraPreview();
			}
		});
	}

	static void SkipUnlessACameraIsAuthorized()
	{
		DeviceTestCapabilities.Require(DeviceTestCapabilities.Camera, AppleCameraEnvironment.HasVideoCaptureDevice, $"Needs a camera ({AppleCameraEnvironment.DescribeCameraAccess()}).");
		Skip.IfNot(AppleCameraEnvironment.IsCameraAuthorized,
			$"Needs the camera permission to be granted ({AppleCameraEnvironment.DescribeCameraAccess()}). A headless run cannot answer the system prompt: run CameraAppleUserInteractionTests once from the visual runner and allow the camera.");
	}
}