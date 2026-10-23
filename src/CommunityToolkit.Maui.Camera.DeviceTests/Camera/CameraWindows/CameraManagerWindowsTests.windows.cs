using CommunityToolkit.Maui.Core;
using Windows.Media.Capture;

namespace CommunityToolkit.Maui.DeviceTests;

// These tests run without a camera, so the state that a started camera would set up (initialized preview) is seeded through
// WindowsCameraManagerAccessor.
[Category(TestCategory.CameraWindows)]
[Collection(TestCollections.DisableParallelization)]
public class CameraManagerWindowsTests : CameraHandlerTestBase
{
	internal const string CameraThatCannotBeOpenedMessage = "Unable to open the selected camera. It may be unavailable or in use by another application.";

	static readonly CameraInfo cameraWithoutFrameSourceGroup = new(
		"Camera without frame source group", "fake-no-group", CameraPosition.Rear, false, 1f, 4f, [new Size(640, 480)], null!, []);

	static CameraInfo CreateCameraWithPhotoSize(uint width, uint height) =>
		new("Camera with one photo size", "fake-photo-size", CameraPosition.Rear, false, 1f, 4f, [new Size(width, height)], null!,
			[WindowsCameraEnvironment.CreateJpegProperties(width, height)]);

	[Fact]
	public async Task CreatePlatformViewReturnsAMediaPlayerElementThatTheManagerKeeps()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new WindowsCameraManagerScope(MauiContext);
			Assert.Null(scope.Access.MediaElement);

			var platformView = scope.Manager.CreatePlatformView();

			Assert.NotNull(platformView);
			Assert.Same(platformView, scope.Access.MediaElement);
			Assert.Null(platformView.Source);
		});
	}

	[Fact]
	public async Task StopCameraPreviewClearsTheSourceReleasesTheMediaCaptureAndMarksTheManagerNotInitialized()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new WindowsCameraManagerScope(MauiContext);
			var mediaElement = scope.Manager.CreatePlatformView();
			WindowsCameraEnvironment.AssignPlaceholderSource(mediaElement);
			var mediaCapture = WindowsCameraEnvironment.CreateMediaCapture();
			scope.Access.MediaCapture = mediaCapture;
			scope.Access.IsInitialized = true;
			Assert.NotNull(mediaElement.Source);
			Assert.False(await WindowsCameraEnvironment.IsClosedAsync(mediaCapture));

			scope.Manager.StopCameraPreview();

			Assert.Null(mediaElement.Source);
			Assert.Null(scope.Access.MediaCapture);
			Assert.False(scope.Access.IsInitialized);
			Assert.True(await WindowsCameraEnvironment.IsClosedAsync(mediaCapture));
		});
	}

	[Fact]
	public async Task StopCameraPreviewWithoutAPlatformViewDoesNothing()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new WindowsCameraManagerScope(MauiContext);
			scope.Access.IsInitialized = true;

			scope.Manager.StopCameraPreview();

			Assert.True(scope.Access.IsInitialized);
		});
	}

	[Fact]
	public async Task DisposeStopsThePreviewAndReleasesTheMediaCapture()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new WindowsCameraManagerScope(MauiContext);
			var mediaElement = scope.Manager.CreatePlatformView();
			WindowsCameraEnvironment.AssignPlaceholderSource(mediaElement);
			scope.Access.MediaCapture = WindowsCameraEnvironment.CreateMediaCapture();
			scope.Access.IsInitialized = true;

			scope.Manager.Dispose();

			Assert.Null(mediaElement.Source);
			Assert.Null(scope.Access.MediaCapture);
			Assert.False(scope.Access.IsInitialized);
		});
	}

	[Fact]
	public async Task DisposeWithoutAPlatformViewStillReleasesTheMediaCapture()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new WindowsCameraManagerScope(MauiContext);
			var mediaCapture = WindowsCameraEnvironment.CreateMediaCapture();
			scope.Access.MediaCapture = mediaCapture;
			Assert.False(await WindowsCameraEnvironment.IsClosedAsync(mediaCapture));

			// Without a platform view StopCameraPreview returns early, so only Dispose itself can release the capture.
			scope.Manager.Dispose();

			Assert.True(await WindowsCameraEnvironment.IsClosedAsync(mediaCapture));
		});
	}

	[Fact]
	public async Task DisposeTwiceAndBeforeCreatingThePlatformViewDoesNotThrow()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var neverCreated = new WindowsCameraManagerScope(MauiContext);
			Assert.Null(Record.Exception(neverCreated.Manager.Dispose));
			Assert.Null(Record.Exception(neverCreated.Manager.Dispose));

			using var created = new WindowsCameraManagerScope(MauiContext);
			var mediaElement = created.Manager.CreatePlatformView();
			WindowsCameraEnvironment.AssignPlaceholderSource(mediaElement);
			created.Access.MediaCapture = WindowsCameraEnvironment.CreateMediaCapture();
			created.Access.IsInitialized = true;
			Assert.NotNull(mediaElement.Source);

			Assert.Null(Record.Exception(created.Manager.Dispose));
			Assert.Null(mediaElement.Source);
			Assert.Null(created.Access.MediaCapture);
			Assert.False(created.Access.IsInitialized);

			Assert.Null(Record.Exception(created.Manager.Dispose));
			Assert.Null(mediaElement.Source);
			Assert.Null(created.Access.MediaCapture);
		});
	}

	[Theory]
	[InlineData(CameraFlashMode.Off)]
	[InlineData(CameraFlashMode.On)]
	[InlineData(CameraFlashMode.Auto)]
	[InlineData((CameraFlashMode)42)]
	public async Task UpdateFlashModeIsIgnoredWhileTheManagerIsNotInitialized(CameraFlashMode flashMode)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new WindowsCameraManagerScope(MauiContext);
			// A MediaCapture that was never initialized: past the isInitialized guard the manager would read its VideoDeviceController.
			var mediaCapture = WindowsCameraEnvironment.CreateMediaCapture();
			scope.Access.MediaCapture = mediaCapture;

			var exception = Record.Exception(() => scope.Manager.UpdateFlashMode(flashMode));

			Assert.Null(exception);
			Assert.Same(mediaCapture, scope.Access.MediaCapture);
		});
	}

	[Theory]
	[InlineData(CameraFlashMode.Off)]
	[InlineData(CameraFlashMode.On)]
	[InlineData(CameraFlashMode.Auto)]
	[InlineData((CameraFlashMode)42)]
	public async Task UpdateFlashModeIsIgnoredWithoutAMediaCapture(CameraFlashMode flashMode)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new WindowsCameraManagerScope(MauiContext);
			scope.Access.IsInitialized = true;

			scope.Manager.UpdateFlashMode(flashMode);

			Assert.Null(scope.Access.MediaCapture);
		});
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task UpdateIsTorchOnIsIgnoredWhileTheManagerIsNotInitialized(bool isTorchOn)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new WindowsCameraManagerScope(MauiContext);
			var mediaCapture = WindowsCameraEnvironment.CreateMediaCapture();
			scope.Access.MediaCapture = mediaCapture;

			var exception = Record.Exception(() => scope.Manager.UpdateIsTorchOn(isTorchOn));

			Assert.Null(exception);
			Assert.Same(mediaCapture, scope.Access.MediaCapture);
		});
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task UpdateIsTorchOnIsIgnoredWithoutAMediaCapture(bool isTorchOn)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new WindowsCameraManagerScope(MauiContext);
			scope.Access.IsInitialized = true;

			var exception = Record.Exception(() => scope.Manager.UpdateIsTorchOn(isTorchOn));

			Assert.Null(exception);
			Assert.Null(scope.Access.MediaCapture);
		});
	}

	[Fact]
	public async Task UpdateZoomIsIgnoredWhileTheManagerIsNotInitialized()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new WindowsCameraManagerScope(MauiContext);
			var mediaCapture = WindowsCameraEnvironment.CreateMediaCapture();
			scope.Access.MediaCapture = mediaCapture;

			var exception = Record.Exception(() => scope.Manager.UpdateZoom(2f));

			Assert.Null(exception);
			Assert.Same(mediaCapture, scope.Access.MediaCapture);
		});
	}

	[Fact]
	public async Task UpdateZoomIsIgnoredWithoutAMediaCapture()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new WindowsCameraManagerScope(MauiContext);
			scope.Access.IsInitialized = true;

			var exception = Record.Exception(() => scope.Manager.UpdateZoom(2f));

			Assert.Null(exception);
			Assert.Null(scope.Access.MediaCapture);
		});
	}

	[Fact]
	public async Task UpdateCaptureResolutionWithoutASelectedCameraCompletes()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new WindowsCameraManagerScope(MauiContext);
			scope.Access.MediaCapture = WindowsCameraEnvironment.CreateMediaCapture();
			scope.Access.IsInitialized = true;

			await scope.Manager.UpdateCaptureResolution(new Size(640, 480), CancellationToken.None);

			Assert.Null(scope.View.SelectedCamera);
		});
	}

	[Fact]
	public async Task UpdateCaptureResolutionWhileNotInitializedCompletes()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new WindowsCameraManagerScope(MauiContext);
			var mediaCapture = WindowsCameraEnvironment.CreateMediaCapture();
			// A camera with a photo size that fits: past the guard the manager would apply it to the (uninitialized) MediaCapture.
			scope.View.SelectedCamera = CreateCameraWithPhotoSize(640, 480);
			scope.Access.MediaCapture = mediaCapture;

			var exception = await Record.ExceptionAsync(async () => await scope.Manager.UpdateCaptureResolution(new Size(640, 480), CancellationToken.None));

			Assert.Null(exception);
			Assert.False(scope.Access.IsInitialized);
			Assert.Same(mediaCapture, scope.Access.MediaCapture);
		});
	}

	[Fact]
	public async Task UpdateCaptureResolutionWithoutAMediaCaptureCompletes()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new WindowsCameraManagerScope(MauiContext);
			// A camera with a photo size that fits: past the guard the manager would dereference the missing MediaCapture.
			scope.View.SelectedCamera = CreateCameraWithPhotoSize(640, 480);
			scope.Access.IsInitialized = true;

			var exception = await Record.ExceptionAsync(async () => await scope.Manager.UpdateCaptureResolution(new Size(640, 480), CancellationToken.None));

			Assert.Null(exception);
			Assert.Null(scope.Access.MediaCapture);
		});
	}

	[Fact]
	public async Task UpdateCaptureResolutionWithACameraWithoutPhotoSizesLeavesTheMediaCaptureAlone()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new WindowsCameraManagerScope(MauiContext);
			scope.View.SelectedCamera = cameraWithoutFrameSourceGroup;
			Assert.Empty(cameraWithoutFrameSourceGroup.ImageEncodingProperties);
			scope.Access.MediaCapture = WindowsCameraEnvironment.CreateMediaCapture();
			scope.Access.IsInitialized = true;

			// The capture is not initialized, so an access to its VideoDeviceController would throw.
			await scope.Manager.UpdateCaptureResolution(new Size(640, 480), CancellationToken.None);
			await scope.Manager.UpdateCaptureResolution(Size.Zero, CancellationToken.None);

			Assert.True(scope.Access.IsInitialized);
			Assert.NotNull(scope.Access.MediaCapture);
		});
	}

	[Fact]
	public async Task TakePictureWithoutAMediaCaptureCapturesNothing()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new WindowsCameraManagerScope(MauiContext);
			var recorder = new WindowsMediaRecorder(scope.View);

			// How the manager reports the missing MediaCapture is not checked here: see TakePictureWithoutAMediaCaptureReportsTheFailure.
			await Record.ExceptionAsync(async () => await scope.Manager.TakePicture(CancellationToken.None));

			Assert.Empty(recorder.Captured);
			GC.KeepAlive(recorder);
		});
	}

	// Without a MediaCapture (which StopCameraPreview leaves behind) PlatformTakePicture must raise MediaCaptureFailed or throw: CameraView.
	// CaptureImage waits for MediaCaptured or MediaCaptureFailed with its caller's token and keeps its capture semaphore meanwhile, so a capture
	// that reports nothing would make it, and every later capture, wait forever.
	[Fact]
	public async Task TakePictureWithoutAMediaCaptureReportsTheFailure()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new WindowsCameraManagerScope(MauiContext);
			var recorder = new WindowsMediaRecorder(scope.View);

			var exception = await Record.ExceptionAsync(async () => await scope.Manager.TakePicture(CancellationToken.None));

			Assert.True(exception is not null || recorder.Failures.Count > 0, "TakePicture neither threw nor raised MediaCaptureFailed, so CaptureImage would wait forever.");
			GC.KeepAlive(recorder);
		});
	}

	[Fact]
	public async Task TakePictureWithACancelledTokenThrowsBeforeCapturing()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new WindowsCameraManagerScope(MauiContext);
			scope.Access.MediaCapture = WindowsCameraEnvironment.CreateMediaCapture();
			var recorder = new WindowsMediaRecorder(scope.View);
			using var cancellationTokenSource = new CancellationTokenSource();
			await cancellationTokenSource.CancelAsync();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await scope.Manager.TakePicture(cancellationTokenSource.Token));

			Assert.Empty(recorder.Captured);
			Assert.Empty(recorder.Failures);
			GC.KeepAlive(recorder);
		});
	}

	// When CapturePhotoToStreamAsync fails, TakePicture reports the reason through MediaCaptureFailed and returns, like on Android, iOS and
	// Mac Catalyst, so that CaptureImage throws its CameraException
	// (CameraViewHandlerWindowsTests.CaptureImageWhenTheCaptureFailsThrowsCameraExceptionWithTheReason).
	[Fact]
	public async Task TakePictureWithAnUninitializedMediaCaptureReportsTheFailure()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new WindowsCameraManagerScope(MauiContext);
			scope.Access.MediaCapture = WindowsCameraEnvironment.CreateMediaCapture();
			var recorder = new WindowsMediaRecorder(scope.View);

			await scope.Manager.TakePicture(CancellationToken.None);

			Assert.Empty(recorder.Captured);
			Assert.False(string.IsNullOrWhiteSpace(Assert.Single(recorder.Failures)));
			GC.KeepAlive(recorder);
		});
	}

	[Fact]
	public async Task StartCameraPreviewWithoutAPlatformViewDoesNothing()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var provider = new WindowsFakeCameraProvider();
			using var scope = new WindowsCameraManagerScope(MauiContext, provider);

			await scope.Manager.StartCameraPreview(CancellationToken.None);

			Assert.Equal(0, provider.RefreshCount);
			Assert.Null(scope.View.SelectedCamera);
			Assert.Null(scope.Access.MediaCapture);
			Assert.Equal(0, scope.LoadedCount);
			Assert.False(scope.Access.IsInitialized);
		});
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task StartCameraPreviewWithoutAnyCameraRaisesErrorOccurred(bool providerKnowsAnEmptyList)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var provider = new WindowsFakeCameraProvider { AvailableCameras = providerKnowsAnEmptyList ? [] : null };
			using var scope = new WindowsCameraManagerScope(MauiContext, provider);
			using var errors = new CameraErrorRecorder(scope.View);
			scope.Manager.CreatePlatformView();

			await scope.Manager.StartCameraPreview(CancellationToken.None);

			var exception = Assert.IsType<CameraException>(Assert.Single(errors.Errors));
			Assert.Equal("Couldn't start camera preview; no cameras available", exception.Message);
			Assert.Equal(0, provider.RefreshCount);
			Assert.Null(scope.View.SelectedCamera);
			Assert.Null(scope.Access.MediaCapture);
			Assert.Equal(0, scope.LoadedCount);
			Assert.False(scope.Access.IsInitialized);
		});
	}

	[Fact]
	public async Task ConnectCameraRefreshesTheProviderWhenItDoesNotKnowTheCamerasYet()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var provider = new WindowsFakeCameraProvider { AvailableCameras = null, CamerasAfterRefresh = [cameraWithoutFrameSourceGroup] };
			using var scope = new WindowsCameraManagerScope(MauiContext, provider);

			await scope.Manager.ConnectCamera(CancellationToken.None);

			Assert.Equal(1, provider.RefreshCount);
			Assert.Same(cameraWithoutFrameSourceGroup, scope.View.SelectedCamera);
			Assert.False(scope.Access.IsInitialized);
		});
	}

	[Fact]
	public async Task ConnectCameraDoesNotRefreshAProviderThatKnowsTheCameras()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var provider = new WindowsFakeCameraProvider { AvailableCameras = [cameraWithoutFrameSourceGroup] };
			using var scope = new WindowsCameraManagerScope(MauiContext, provider);

			await scope.Manager.ConnectCamera(CancellationToken.None);

			Assert.Equal(0, provider.RefreshCount);
			Assert.Same(cameraWithoutFrameSourceGroup, scope.View.SelectedCamera);
		});
	}

	[Fact]
	public async Task ConnectCameraKeepsTheExplicitlySelectedCamera()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var other = new CameraInfo("Other", "fake-other", CameraPosition.Front, false, 1f, 1f, [], null!, []);
			var provider = new WindowsFakeCameraProvider { AvailableCameras = [other] };
			using var scope = new WindowsCameraManagerScope(MauiContext, provider);
			scope.View.SelectedCamera = cameraWithoutFrameSourceGroup;

			await scope.Manager.ConnectCamera(CancellationToken.None);

			Assert.Same(cameraWithoutFrameSourceGroup, scope.View.SelectedCamera);
		});
	}

	[Fact]
	public async Task ConnectCameraWithoutAnyCameraRaisesErrorOccurred()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var provider = new WindowsFakeCameraProvider { AvailableCameras = null, CamerasAfterRefresh = [] };
			using var scope = new WindowsCameraManagerScope(MauiContext, provider);
			using var errors = new CameraErrorRecorder(scope.View);
			scope.Manager.CreatePlatformView();

			await scope.Manager.ConnectCamera(CancellationToken.None);

			var exception = Assert.IsType<CameraException>(Assert.Single(errors.Errors));
			Assert.Equal("Couldn't connect to camera; no cameras available", exception.Message);
			Assert.Equal(1, provider.RefreshCount);
			Assert.Null(scope.View.SelectedCamera);
			Assert.False(scope.Access.IsInitialized);
		});
	}

	[Fact]
	public async Task ConnectCameraPropagatesACancelledProviderRefresh()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var provider = new WindowsFakeCameraProvider { AvailableCameras = null, CamerasAfterRefresh = [cameraWithoutFrameSourceGroup] };
			using var scope = new WindowsCameraManagerScope(MauiContext, provider);
			using var cancellationTokenSource = new CancellationTokenSource();
			await cancellationTokenSource.CancelAsync();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => scope.Manager.ConnectCamera(cancellationTokenSource.Token));

			Assert.Null(scope.View.SelectedCamera);
			Assert.Equal(0, provider.RefreshCount);
		});
	}

	[Fact]
	public async Task UpdateCurrentCameraWithNullDoesNothingEvenWhenInitialized()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new WindowsCameraManagerScope(MauiContext);
			var mediaElement = scope.Manager.CreatePlatformView();
			WindowsCameraEnvironment.AssignPlaceholderSource(mediaElement);
			scope.Access.IsInitialized = true;

			await scope.Manager.UpdateCurrentCamera(null, CancellationToken.None);

			Assert.True(scope.Access.IsInitialized);
			Assert.NotNull(mediaElement.Source);
		});
	}

	[Fact]
	public async Task UpdateCurrentCameraBeforeInitializationDoesNotStartThePreview()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var provider = new WindowsFakeCameraProvider();
			using var scope = new WindowsCameraManagerScope(MauiContext, provider);
			scope.Manager.CreatePlatformView();

			await scope.Manager.UpdateCurrentCamera(cameraWithoutFrameSourceGroup, CancellationToken.None);

			Assert.False(scope.Access.IsInitialized);
			Assert.Null(scope.Access.MediaCapture);
			Assert.Equal(0, scope.LoadedCount);
			Assert.Equal(0, provider.RefreshCount);
		});
	}

	// A camera switch that fails (here on a camera that no device has: Media Foundation reports the unknown device id as a COMException, which
	// InitializeCameraForCameraView turns into a camera that cannot be opened) leaves the preview stopped but the manager initialized, like on
	// Android, iOS and Mac Catalyst: the next camera change tries to open its camera instead of being ignored, as it was when the failed switch
	// left the manager stopped. On Windows the failure is reported through ErrorOccurred instead of an exception.
	[Fact]
	public async Task UpdateCurrentCameraAfterASwitchThatFailedOpensTheNextSelectedCamera()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new WindowsCameraManagerScope(MauiContext);
			using var errors = new CameraErrorRecorder(scope.View);
			var mediaElement = scope.Manager.CreatePlatformView();
			WindowsCameraEnvironment.AssignPlaceholderSource(mediaElement);
			scope.Access.MediaCapture = WindowsCameraEnvironment.CreateMediaCapture();
			scope.Access.IsInitialized = true;
			var first = new CameraInfo("Unknown camera", "fake-unknown-first-device", CameraPosition.Rear, false, 1f, 1f, [], null!, []);
			var next = new CameraInfo("Other unknown camera", "fake-unknown-next-device", CameraPosition.Front, false, 1f, 1f, [], null!, []);
			scope.View.SelectedCamera = first;

			await scope.Manager.UpdateCurrentCamera(first, CancellationToken.None);

			Assert.Equal(CameraThatCannotBeOpenedMessage, Assert.IsType<CameraException>(Assert.Single(errors.Errors)).Message);
			Assert.True(scope.Access.IsInitialized);
			Assert.Null(scope.Access.MediaCapture);
			Assert.Null(mediaElement.Source);
			Assert.Equal(0, scope.LoadedCount);

			scope.View.SelectedCamera = next;
			await scope.Manager.UpdateCurrentCamera(next, CancellationToken.None);

			Assert.Equal(2, errors.Errors.Count);
			Assert.Equal(CameraThatCannotBeOpenedMessage, Assert.IsType<CameraException>(errors.Errors[1]).Message);
			Assert.True(scope.Access.IsInitialized);
			Assert.Null(scope.Access.MediaCapture);
		});
	}

	[Theory]
	[InlineData("notInitialized")]
	[InlineData("noMediaCapture")]
	[InlineData("noPlatformView")]
	public async Task StartVideoRecordingWithoutAStartedPreviewDoesNothing(string missingPart)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new WindowsCameraManagerScope(MauiContext);
			if (missingPart is not "noPlatformView")
			{
				scope.Manager.CreatePlatformView();
			}

			scope.Access.IsInitialized = missingPart is not "notInitialized";
			scope.Access.MediaCapture = missingPart is "noMediaCapture" ? null : WindowsCameraEnvironment.CreateMediaCapture();
			using var destination = new MemoryStream();

			await scope.Manager.StartVideoRecording(destination, CancellationToken.None);

			Assert.Null(scope.Access.VideoCaptureStream);
			Assert.Equal(0, destination.Length);
		});
	}

	// Only the missing recording can be produced without a camera (a LowLagMediaRecording comes from an initialized MediaCapture). The
	// not-initialized and no-platform-view guards with a recording are in CameraManagerWindowsCaptureTests.
	[Fact]
	public async Task StopVideoRecordingWithoutARecordingReturnsTheNullStream()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new WindowsCameraManagerScope(MauiContext);
			scope.Manager.CreatePlatformView();
			scope.Access.IsInitialized = true;
			using var destination = new MemoryStream();
			scope.Access.VideoCaptureStream = destination;
			Assert.Null(scope.Access.MediaRecording);

			var result = await scope.Manager.StopVideoRecording(CancellationToken.None);

			Assert.Same(Stream.Null, result);
			Assert.Equal(0, destination.Length);
		});
	}

	// InitializeCameraForCameraView tolerates the COMException of MediaCapture.InitializeAsync ("Camera already initialized") for the camera
	// discovery, which also hides a camera that does not exist or that another app holds (Media Foundation reports the unknown device id as a
	// COMException). PlatformStartCameraPreview must not go on with a MediaCapture that was never initialized (its FrameSources access throws a
	// raw WinRT exception, or the preview "starts" empty): it reports a CameraException through ErrorOccurred and releases that MediaCapture,
	// so that TakePicture reports a camera that is not initialized instead of capturing from it.
	[Fact]
	public async Task StartCameraPreviewOfACameraThatCannotBeOpenedRaisesErrorOccurred()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var unknownCamera = new CameraInfo("Unknown camera", "fake-unknown-device", CameraPosition.Rear, false, 1f, 1f, [], null!, []);
			var provider = new WindowsFakeCameraProvider { AvailableCameras = [unknownCamera] };
			using var scope = new WindowsCameraManagerScope(MauiContext, provider);
			using var errors = new CameraErrorRecorder(scope.View);
			scope.Manager.CreatePlatformView();

			await scope.Manager.StartCameraPreview(CancellationToken.None);

			Assert.Equal(CameraThatCannotBeOpenedMessage, Assert.IsType<CameraException>(Assert.Single(errors.Errors)).Message);
			Assert.Null(scope.Access.MediaCapture);
			Assert.Equal(0, scope.LoadedCount);

			// The camera cannot be opened, but the preview counts as started: the next SelectedCamera tries to open its camera.
			Assert.True(scope.Access.IsInitialized);
		});
	}

	// The next SelectedCamera is a camera that no device has too: its second error shows that the manager tried to open it.
	[Fact]
	public async Task StartCameraPreviewOfACameraThatCannotBeOpenedOpensTheNextSelectedCamera()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var first = new CameraInfo("Unknown camera", "fake-unknown-first-start-device", CameraPosition.Rear, false, 1f, 1f, [], null!, []);
			var next = new CameraInfo("Other unknown camera", "fake-unknown-after-first-start-device", CameraPosition.Front, false, 1f, 1f, [], null!, []);
			var provider = new WindowsFakeCameraProvider { AvailableCameras = [first, next] };
			using var scope = new WindowsCameraManagerScope(MauiContext, provider);
			using var errors = new CameraErrorRecorder(scope.View);
			scope.Manager.CreatePlatformView();

			await scope.Manager.StartCameraPreview(CancellationToken.None);

			Assert.Same(first, scope.View.SelectedCamera);
			Assert.Equal(CameraThatCannotBeOpenedMessage, Assert.IsType<CameraException>(Assert.Single(errors.Errors)).Message);
			Assert.True(scope.Access.IsInitialized);

			scope.View.SelectedCamera = next;
			await scope.Manager.UpdateCurrentCamera(next, CancellationToken.None);

			Assert.Equal(2, errors.Errors.Count);
			Assert.Equal(CameraThatCannotBeOpenedMessage, Assert.IsType<CameraException>(errors.Errors[1]).Message);
			Assert.True(scope.Access.IsInitialized);
			Assert.Null(scope.Access.MediaCapture);
			Assert.Equal(0, scope.LoadedCount);
		});
	}

	[Fact]
	public async Task DisconnectDoesNotStopThePreview()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new WindowsCameraManagerScope(MauiContext);
			var mediaElement = scope.Manager.CreatePlatformView();
			WindowsCameraEnvironment.AssignPlaceholderSource(mediaElement);
			scope.Access.IsInitialized = true;

			scope.Manager.Disconnect();

			Assert.NotNull(mediaElement.Source);
			Assert.True(scope.Access.IsInitialized);
		});
	}
}