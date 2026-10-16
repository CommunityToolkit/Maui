using AVFoundation;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

// CameraManager.macios.cs and the shared CameraManager.shared.cs members on iOS and Mac Catalyst, in places without a camera or
// without the camera permission: a real AVCaptureSession and preview layer are created, and the state that a running capture
// session would set up (initialized preview, an output that is recording) is seeded through CameraManagerAccessor.
[Category(TestCategory.CameraApple)]
[Collection(TestCollections.DisableParallelization)]
public class CameraManagerAppleTests : CameraHandlerTestBase
{
	const string cameraNotStartedMessage = "Unable to capture an image because the camera is not initialized. Start the camera preview first.";

	static readonly CameraInfo cameraWithoutCaptureDevice = new(
		"Camera without capture device", "fake-no-device", CameraPosition.Rear, false, 1f, 4f, [new Size(640, 480)], null!, []);

	[Fact]
	public async Task CreatePlatformViewReturnsPreviewViewBackedByAPhotoSession()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);

			var platformView = scope.Manager.CreatePlatformView();

			var previewLayer = Assert.IsType<AVCaptureVideoPreviewLayer>(platformView.Layer, exactMatch: false);
			Assert.Equal(AVLayerVideoGravity.ResizeAspectFill, previewLayer.VideoGravity);

			var session = Assert.IsType<AVCaptureSession>(previewLayer.Session, exactMatch: false);
			Assert.Same(scope.Access.CaptureSession, session);
			Assert.Equal(AVCaptureSession.PresetPhoto, session.SessionPreset);
			Assert.False(session.Running);
			Assert.Empty(session.Inputs);
			Assert.Empty(session.Outputs);
		});
	}

	[Fact]
	public async Task CreatePlatformViewInitializesVideoOrientationFromTheWindowScene()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			scope.Access.VideoOrientation = (AVCaptureVideoOrientation)99;

			scope.Manager.CreatePlatformView();

			Assert.Equal(GetExpectedVideoOrientation(GetWindowScene()), scope.Access.VideoOrientation);
		});
	}

	[Fact]
	public async Task OrientationChangeNotificationUpdatesTheVideoOrientation()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			scope.Manager.CreatePlatformView();
			scope.Access.VideoOrientation = (AVCaptureVideoOrientation)99;

			AppleCameraEnvironment.PostOrientationDidChangeNotification();

			Assert.Equal(GetExpectedVideoOrientation(GetWindowScene()), scope.Access.VideoOrientation);
		});
	}

	[Fact]
	public async Task OrientationChangeNotificationIsIgnoredAfterDispose()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			scope.Manager.CreatePlatformView();

			scope.Manager.Dispose();
			scope.Access.VideoOrientation = (AVCaptureVideoOrientation)99;
			AppleCameraEnvironment.PostOrientationDidChangeNotification();

			Assert.Equal((AVCaptureVideoOrientation)99, scope.Access.VideoOrientation);
		});
	}

	[Fact]
	public async Task DisposeReleasesTheNativeSessionAndPreviewView()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			var platformView = scope.Manager.CreatePlatformView();
			var session = scope.Access.CaptureSession;
			Assert.NotNull(session);
			Assert.NotEqual(NativeHandle.Zero, platformView.Handle);
			Assert.NotEqual(NativeHandle.Zero, session.Handle);

			scope.Manager.Dispose();

			Assert.Equal(NativeHandle.Zero, platformView.Handle);
			Assert.Equal(NativeHandle.Zero, session.Handle);
			Assert.Null(scope.Access.CaptureSession);
			Assert.Null(scope.Access.OrientationDidChangeObserver);
		});
	}

	[Fact]
	public async Task DisposeStopsARunningSession()
	{
		await AppleCameraEnvironment.SkipUnlessCaptureSessionsStartPromptlyAsync();
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			scope.Manager.CreatePlatformView();
			var session = scope.Access.CaptureSession;
			Assert.NotNull(session);
			session.StartRunning();
			Assert.True(session.Running);

			scope.Manager.Dispose();

			Assert.Equal(NativeHandle.Zero, session.Handle);
		});
	}

	[Fact]
	public async Task DisposeTwiceAndBeforeCreatingThePlatformViewDoesNotThrow()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var neverCreated = new AppleCameraManagerScope(MauiContext);
			Assert.Null(Record.Exception(neverCreated.Manager.Dispose));
			Assert.Null(Record.Exception(neverCreated.Manager.Dispose));
			Assert.Null(neverCreated.Access.CaptureSession);
			Assert.Null(neverCreated.Access.PreviewView);

			using var created = new AppleCameraManagerScope(MauiContext);
			var platformView = created.Manager.CreatePlatformView();
			var session = created.Access.CaptureSession ?? throw new InvalidOperationException("No capture session.");
			Assert.Null(Record.Exception(created.Manager.Dispose));
			Assert.Equal(NativeHandle.Zero, platformView.Handle);
			Assert.Equal(NativeHandle.Zero, session.Handle);

			// The second Dispose finds nothing left to release.
			Assert.Null(Record.Exception(created.Manager.Dispose));
			Assert.Null(created.Access.CaptureSession);
			Assert.Null(created.Access.PreviewView);
			Assert.Null(created.Access.OrientationDidChangeObserver);
		});
	}

	[Theory]
	[InlineData(CameraFlashMode.Off, AVCaptureFlashMode.Off)]
	[InlineData(CameraFlashMode.On, AVCaptureFlashMode.On)]
	[InlineData(CameraFlashMode.Auto, AVCaptureFlashMode.Auto)]
	public async Task UpdateFlashModeStoresTheMatchingCaptureFlashMode(CameraFlashMode flashMode, AVCaptureFlashMode expected)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			scope.Manager.UpdateFlashMode(flashMode is CameraFlashMode.Auto ? CameraFlashMode.On : CameraFlashMode.Auto);

			scope.Manager.UpdateFlashMode(flashMode);

			Assert.Equal(expected, scope.Access.FlashMode);
		});
	}

	[Fact]
	public async Task UpdateFlashModeWithAnUndefinedValueThrowsAndKeepsThePreviousMode()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			scope.Manager.UpdateFlashMode(CameraFlashMode.On);

			var exception = Assert.Throws<NotSupportedException>(() => scope.Manager.UpdateFlashMode((CameraFlashMode)42));

			Assert.Equal("42 is not yet supported", exception.Message);
			Assert.Equal(AVCaptureFlashMode.On, scope.Access.FlashMode);
		});
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(true, false)]
	[InlineData(false, true)]
	[InlineData(true, true)]
	public async Task UpdateIsTorchOnWithoutAUsableCaptureDeviceDoesNothing(bool isInitialized, bool isTorchOn)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			scope.Access.IsInitialized = isInitialized;
			scope.Access.CaptureDevice = null;

			scope.Manager.UpdateIsTorchOn(isTorchOn);

			Assert.Null(scope.Access.CaptureDevice);
		});
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task UpdateZoomWithoutAUsableCaptureDeviceDoesNothing(bool isInitialized)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			scope.Access.IsInitialized = isInitialized;
			scope.Access.CaptureDevice = null;

			scope.Manager.UpdateZoom(2f);

			Assert.Null(scope.Access.CaptureDevice);
		});
	}

	[Fact]
	public async Task UpdateCaptureResolutionWithoutSelectedCameraOrCaptureDeviceCompletesImmediately()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);

			var withoutCamera = scope.Manager.UpdateCaptureResolution(new Size(640, 480), CancellationToken.None);
			Assert.True(withoutCamera.IsCompletedSuccessfully);
			await withoutCamera;

			scope.View.SelectedCamera = cameraWithoutCaptureDevice;
			var withoutDevice = scope.Manager.UpdateCaptureResolution(new Size(640, 480), CancellationToken.None);
			Assert.True(withoutDevice.IsCompletedSuccessfully);
			await withoutDevice;

			Assert.Null(scope.Access.CaptureDevice);
		});
	}

	[Theory]
	[InlineData(0.0, -1.0, AVCaptureVideoOrientation.Portrait)]
	[InlineData(0.0, 1.0, AVCaptureVideoOrientation.PortraitUpsideDown)]
	[InlineData(1.0, 0.0, AVCaptureVideoOrientation.LandscapeLeft)]
	[InlineData(-1.0, 0.0, AVCaptureVideoOrientation.LandscapeRight)]
	[InlineData(0.0, 0.0, AVCaptureVideoOrientation.Portrait)]
	[InlineData(0.5, -0.5, AVCaptureVideoOrientation.Portrait)]
	[InlineData(-0.5, 0.5, AVCaptureVideoOrientation.PortraitUpsideDown)]
	[InlineData(0.7, 0.2, AVCaptureVideoOrientation.LandscapeLeft)]
	[InlineData(-0.7, -0.2, AVCaptureVideoOrientation.LandscapeRight)]
	public void AccelerometerReadingMapsToTheDominantAxisOrientation(double x, double y, AVCaptureVideoOrientation expected)
	{
		Assert.Equal(expected, CameraManagerAccessor.GetVideoOrientationFromAccelerometer(x, y));
	}

	[Fact]
	public async Task VideoOrientationFollowsTheCurrentWindowSceneOrientation()
	{
		SetupBuilder();

		var (actual, expected) = await InvokeOnMainThreadAsync(() =>
			(CameraManagerAccessor.GetVideoOrientation(), GetExpectedVideoOrientation(GetWindowScene())));

		Assert.Equal(expected, actual);
	}

	[Theory]
	[InlineData(UIInterfaceOrientationMask.LandscapeLeft, UIInterfaceOrientation.LandscapeLeft, AVCaptureVideoOrientation.LandscapeLeft)]
	[InlineData(UIInterfaceOrientationMask.LandscapeRight, UIInterfaceOrientation.LandscapeRight, AVCaptureVideoOrientation.LandscapeRight)]
	[InlineData(UIInterfaceOrientationMask.Portrait, UIInterfaceOrientation.Portrait, AVCaptureVideoOrientation.Portrait)]
	public async Task VideoOrientationFollowsARotatedWindowScene(UIInterfaceOrientationMask requestedMask, UIInterfaceOrientation expectedInterfaceOrientation, AVCaptureVideoOrientation expectedVideoOrientation)
	{
		Skip.If(OperatingSystem.IsMacCatalyst(), "Mac Catalyst windows cannot be rotated programmatically ('The current windowing mode does not allow for programmatic changes to interface orientation', verified).");
		SetupBuilder();

		try
		{
			await RotateWindowSceneAsync(requestedMask, expectedInterfaceOrientation);

			var actual = await InvokeOnMainThreadAsync(CameraManagerAccessor.GetVideoOrientation);

			Assert.Equal(expectedVideoOrientation, actual);
		}
		finally
		{
			await RotateWindowSceneAsync(UIInterfaceOrientationMask.Portrait, UIInterfaceOrientation.Portrait);
		}
	}

	[Fact]
	public async Task OrientationChangeNotificationFollowsARotatedWindowScene()
	{
		Skip.If(OperatingSystem.IsMacCatalyst(), "Mac Catalyst windows cannot be rotated programmatically ('The current windowing mode does not allow for programmatic changes to interface orientation', verified).");
		SetupBuilder();

		try
		{
			await RotateWindowSceneAsync(UIInterfaceOrientationMask.LandscapeLeft, UIInterfaceOrientation.LandscapeLeft);

			await InvokeOnMainThreadAsync(() =>
			{
				using var scope = new AppleCameraManagerScope(MauiContext);
				scope.Manager.CreatePlatformView();
				Assert.Equal(AVCaptureVideoOrientation.LandscapeLeft, scope.Access.VideoOrientation);

				scope.Access.VideoOrientation = AVCaptureVideoOrientation.Portrait;
				AppleCameraEnvironment.PostOrientationDidChangeNotification();

				Assert.Equal(AVCaptureVideoOrientation.LandscapeLeft, scope.Access.VideoOrientation);
			});
		}
		finally
		{
			await RotateWindowSceneAsync(UIInterfaceOrientationMask.Portrait, UIInterfaceOrientation.Portrait);
		}
	}

	[Fact]
	public async Task StopCameraPreviewWithoutAPlatformViewDoesNothing()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			scope.Access.IsInitialized = true;

			scope.Manager.StopCameraPreview();

			Assert.True(scope.Access.IsInitialized);
		});
	}

	[Fact]
	public async Task StopCameraPreviewMarksTheManagerAsNotInitializedWhenTheSessionIsIdle()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			scope.Manager.CreatePlatformView();
			scope.Access.IsInitialized = true;

			scope.Manager.StopCameraPreview();

			Assert.False(scope.Access.IsInitialized);
			Assert.False(scope.Access.CaptureSession?.Running);
		});
	}

	[Fact]
	public async Task StopCameraPreviewStopsARunningSession()
	{
		await AppleCameraEnvironment.SkipUnlessCaptureSessionsStartPromptlyAsync();
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			scope.Manager.CreatePlatformView();
			var session = scope.Access.CaptureSession;
			Assert.NotNull(session);
			session.StartRunning();
			scope.Access.IsInitialized = true;
			Assert.True(session.Running);

			scope.Manager.StopCameraPreview();

			Assert.False(session.Running);
			Assert.False(scope.Access.IsInitialized);
		});
	}

	[Fact]
	public async Task DisconnectDoesNotStopOrReleaseTheSession()
	{
		await AppleCameraEnvironment.SkipUnlessCaptureSessionsStartPromptlyAsync();
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			var platformView = scope.Manager.CreatePlatformView();
			var session = scope.Access.CaptureSession;
			Assert.NotNull(session);
			session.StartRunning();
			scope.Access.IsInitialized = true;

			scope.Manager.Disconnect();

			Assert.True(session.Running);
			Assert.True(scope.Access.IsInitialized);
			Assert.NotEqual(NativeHandle.Zero, platformView.Handle);
			session.StopRunning();
		});
	}

	[Fact]
	public async Task StartCameraPreviewWithoutAPlatformViewDoesNothing()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var provider = new AppleFakeCameraProvider();
			using var scope = new AppleCameraManagerScope(MauiContext, provider);

			await scope.Manager.StartCameraPreview(CancellationToken.None);

			Assert.Equal(0, provider.RefreshCount);
			Assert.Null(scope.View.SelectedCamera);
			Assert.Equal(0, scope.LoadedCount);
			Assert.False(scope.Access.IsInitialized);
		});
	}

	[Fact]
	public async Task StartCameraPreviewWithoutAnyCameraThrowsCameraException()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var provider = new AppleFakeCameraProvider { AvailableCameras = [] };
			using var scope = new AppleCameraManagerScope(MauiContext, provider);
			scope.Manager.CreatePlatformView();

			var exception = await StartCameraPreviewExpectingFailureAsync(scope);

			Assert.Equal("No camera available on device", exception.Message);
			Assert.Null(scope.View.SelectedCamera);
			Assert.Equal(0, scope.LoadedCount);
			Assert.False(scope.Access.IsInitialized);
			Assert.False(scope.Access.CaptureSession?.Running);
		});
	}

	[Fact]
	public async Task StartCameraPreviewWithoutProviderCamerasThrowsCameraException()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var provider = new AppleFakeCameraProvider { AvailableCameras = null };
			using var scope = new AppleCameraManagerScope(MauiContext, provider);
			scope.Manager.CreatePlatformView();

			var exception = await StartCameraPreviewExpectingFailureAsync(scope);

			Assert.Equal("No camera available on device", exception.Message);
			Assert.Equal(0, provider.RefreshCount);
		});
	}

	[Fact]
	public async Task StartCameraPreviewSelectsTheFirstProviderCameraAndThrowsWhenItHasNoCaptureDevice()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var provider = new AppleFakeCameraProvider { AvailableCameras = [cameraWithoutCaptureDevice] };
			using var scope = new AppleCameraManagerScope(MauiContext, provider);
			scope.Manager.CreatePlatformView();

			var exception = await StartCameraPreviewExpectingFailureAsync(scope);

			Assert.Equal("No Camera found", exception.Message);
			Assert.Same(cameraWithoutCaptureDevice, scope.View.SelectedCamera);
			Assert.Null(scope.Access.CaptureInput);
			Assert.Empty(scope.Access.CaptureSession?.Inputs ?? []);
			Assert.Equal(0, scope.LoadedCount);
			Assert.False(scope.Access.IsInitialized);
		});
	}

	[Fact]
	public async Task StartCameraPreviewKeepsTheExplicitlySelectedCamera()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var other = new CameraInfo("Other", "fake-other", CameraPosition.Front, false, 1f, 1f, [], null!, []);
			var provider = new AppleFakeCameraProvider { AvailableCameras = [other] };
			using var scope = new AppleCameraManagerScope(MauiContext, provider);
			scope.Manager.CreatePlatformView();
			scope.View.SelectedCamera = cameraWithoutCaptureDevice;

			await StartCameraPreviewExpectingFailureAsync(scope);

			Assert.Same(cameraWithoutCaptureDevice, scope.View.SelectedCamera);
		});
	}

	// PlatformStartCameraPreview calls captureSession.BeginConfiguration() before it can fail (no camera, camera without capture device); if
	// it did not commit the configuration, CameraManager.Dispose() -> captureSession.StopRunning() would raise NSGenericException 'stopRunning
	// may not be called between calls to beginConfiguration and commitConfiguration' on this real session.
	[Fact]
	public async Task DisposeAfterAFailedStartCameraPreviewDoesNotThrow()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var provider = new AppleFakeCameraProvider { AvailableCameras = [] };
			var scope = new AppleCameraManagerScope(MauiContext, provider);
			var platformView = scope.Manager.CreatePlatformView();
			await Assert.ThrowsAsync<CameraException>(() => scope.Manager.StartCameraPreview(CancellationToken.None));

			scope.Dispose();

			Assert.Equal(NativeHandle.Zero, platformView.Handle);
		});
	}

	[Fact]
	public async Task ConnectCameraRefreshesTheProviderWhenItDoesNotKnowTheCamerasYet()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var provider = new AppleFakeCameraProvider { AvailableCameras = null, CamerasAfterRefresh = [cameraWithoutCaptureDevice] };
			using var scope = new AppleCameraManagerScope(MauiContext, provider);

			await scope.Manager.ConnectCamera(CancellationToken.None);

			Assert.Equal(1, provider.RefreshCount);
			Assert.Same(cameraWithoutCaptureDevice, scope.View.SelectedCamera);
			Assert.False(scope.Access.IsInitialized);
		});
	}

	[Fact]
	public async Task ConnectCameraDoesNotRefreshAProviderThatKnowsTheCameras()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var provider = new AppleFakeCameraProvider { AvailableCameras = [cameraWithoutCaptureDevice] };
			using var scope = new AppleCameraManagerScope(MauiContext, provider);

			await scope.Manager.ConnectCamera(CancellationToken.None);

			Assert.Equal(0, provider.RefreshCount);
			Assert.Same(cameraWithoutCaptureDevice, scope.View.SelectedCamera);
		});
	}

	[Fact]
	public async Task ConnectCameraKeepsTheExplicitlySelectedCamera()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var other = new CameraInfo("Other", "fake-other", CameraPosition.Front, false, 1f, 1f, [], null!, []);
			var provider = new AppleFakeCameraProvider { AvailableCameras = [other] };
			using var scope = new AppleCameraManagerScope(MauiContext, provider);
			scope.View.SelectedCamera = cameraWithoutCaptureDevice;

			await scope.Manager.ConnectCamera(CancellationToken.None);

			Assert.Same(cameraWithoutCaptureDevice, scope.View.SelectedCamera);
		});
	}

	[Fact]
	public async Task ConnectCameraWithoutAnyCameraRaisesErrorOccurredBeforeTouchingTheSession()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var provider = new AppleFakeCameraProvider { AvailableCameras = null, CamerasAfterRefresh = [] };
			using var scope = new AppleCameraManagerScope(MauiContext, provider);
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
			var provider = new AppleFakeCameraProvider { AvailableCameras = null, CamerasAfterRefresh = [cameraWithoutCaptureDevice] };
			using var scope = new AppleCameraManagerScope(MauiContext, provider);
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
			var provider = new AppleFakeCameraProvider { AvailableCameras = [] };
			using var scope = new AppleCameraManagerScope(MauiContext, provider);
			scope.Manager.CreatePlatformView();
			scope.Access.IsInitialized = true;

			await scope.Manager.UpdateCurrentCamera(null, CancellationToken.None);

			Assert.True(scope.Access.IsInitialized);
			Assert.Equal(0, provider.RefreshCount);
		});
	}

	[Fact]
	public async Task UpdateCurrentCameraBeforeInitializationDoesNotStartThePreview()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			scope.Manager.CreatePlatformView();

			await scope.Manager.UpdateCurrentCamera(cameraWithoutCaptureDevice, CancellationToken.None);

			Assert.False(scope.Access.IsInitialized);
			Assert.Equal(0, scope.LoadedCount);
			Assert.Empty(scope.Access.CaptureSession?.Inputs ?? []);
		});
	}

	// On a real AVCaptureSession that never started (CameraManagerAppleSessionTests checks the calls of a switch on a fake session).
	[Fact]
	public async Task UpdateCurrentCameraWhenInitializedToACameraWithoutACaptureDeviceThrowsCameraExceptionAndStaysInitialized()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			scope.Manager.CreatePlatformView();
			scope.Access.IsInitialized = true;
			scope.View.SelectedCamera = cameraWithoutCaptureDevice;

			var exception = await Assert.ThrowsAsync<CameraException>(async () => await scope.Manager.UpdateCurrentCamera(cameraWithoutCaptureDevice, CancellationToken.None));

			// The restart failed on the camera without device, and the manager stays initialized, so that the next camera change starts the
			// preview again (UpdateCurrentCameraAfterASwitchThatFailedStartsTheNextSelectedCamera).
			Assert.Equal("No Camera found", exception.Message);
			Assert.True(scope.Access.IsInitialized);
			Assert.Equal(0, scope.LoadedCount);
		});
	}

	// A camera that never started has no photo output (StartCameraPreview creates it). The capture is reported as MediaCaptureFailed, which
	// CameraView.CaptureImage turns into a CameraException, as on Windows; it used to throw ArgumentNullException for the photo output.
	[Fact]
	public async Task TakePictureWithoutAPhotoOutputRaisesMediaCaptureFailed()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			scope.Manager.CreatePlatformView();
			var recorder = new AppleMediaRecorder(scope.View);

			await scope.Manager.TakePicture(CancellationToken.None);

			Assert.Equal([cameraNotStartedMessage], recorder.Failures);
			Assert.Empty(recorder.Captured);
			Assert.Null(scope.Access.PhotoOutput);
			GC.KeepAlive(recorder);
		});
	}

	// The missing photo output is reported before the token is looked at, as on Windows.
	[Fact]
	public async Task TakePictureWithACancelledTokenAndNoPhotoOutputStillRaisesMediaCaptureFailed()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			var recorder = new AppleMediaRecorder(scope.View);
			using var cancellationTokenSource = new CancellationTokenSource();
			await cancellationTokenSource.CancelAsync();

			await scope.Manager.TakePicture(cancellationTokenSource.Token);

			Assert.Equal([cameraNotStartedMessage], recorder.Failures);
			Assert.Empty(recorder.Captured);
			GC.KeepAlive(recorder);
		});
	}

	[Fact]
	public async Task TryConfigureCaptureConnectionFailsForOutputsWithoutAVideoConnection()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			scope.Manager.CreatePlatformView();
			using var photoOutput = new AVCapturePhotoOutput();
			using var movieOutput = new AVCaptureMovieFileOutput();

			Assert.False(scope.Access.TryConfigureAVCaptureConnection(photoOutput, out var photoError));
			Assert.False(scope.Access.TryConfigureAVCaptureConnection(movieOutput, out var movieError));

			Assert.Equal("Unable to determine video connection from media type.", photoError);
			Assert.Equal("Unable to determine video connection from media type.", movieError);
		});
	}

	[Fact]
	public async Task PreviewViewWithoutAConnectionAcceptsOrientationUpdatesAndLayout()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			var platformView = scope.Manager.CreatePlatformView();
			var previewLayer = Assert.IsType<AVCaptureVideoPreviewLayer>(platformView.Layer, exactMatch: false);
			Assert.Null(previewLayer.Connection);

			var updateOrientation = platformView.GetType().GetMethod("UpdatePreviewVideoOrientation")
				?? throw new MissingMethodException(platformView.GetType().FullName, "UpdatePreviewVideoOrientation");
			updateOrientation.Invoke(platformView, [AVCaptureVideoOrientation.LandscapeRight]);

			platformView.Frame = new CoreGraphics.CGRect(0, 0, 120, 80);
			platformView.SetNeedsLayout();
			platformView.LayoutIfNeeded();

			Assert.Null(previewLayer.Connection);
			Assert.Equal(120, platformView.Layer.Bounds.Width);
			Assert.Equal(80, platformView.Layer.Bounds.Height);
		});
	}

	[Fact]
	public async Task StopVideoRecordingWithoutARecordingReturnsTheNullStream()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);

			var withoutSession = await scope.Manager.StopVideoRecording(CancellationToken.None);
			scope.Manager.CreatePlatformView();
			var withSession = await scope.Manager.StopVideoRecording(CancellationToken.None);

			Assert.Same(Stream.Null, withoutSession);
			Assert.Same(Stream.Null, withSession);
		});
	}

	[Theory]
	[InlineData("fileName")]
	[InlineData("output")]
	[InlineData("stream")]
	[InlineData("tcs")]
	public async Task StopVideoRecordingWithAnyMissingRecordingPartReturnsTheNullStream(string missingPart)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			scope.Manager.CreatePlatformView();
			using var stream = new MemoryStream();
			using var output = new AVCaptureMovieFileOutput();
			scope.Access.VideoRecordingFileName = missingPart is "fileName" ? null : Path.Combine(Path.GetTempPath(), $"fake-{Guid.NewGuid()}.mov");
			scope.Access.VideoOutput = missingPart is "output" ? null : output;
			scope.Access.VideoRecordingStream = missingPart is "stream" ? null : stream;
			scope.Access.VideoRecordingFinalizeTcs = missingPart is "tcs" ? null : new TaskCompletionSource();

			var result = await scope.Manager.StopVideoRecording(CancellationToken.None);

			Assert.Same(Stream.Null, result);
			Assert.Equal(0, stream.Length);
			scope.Access.VideoOutput = null;
		});
	}

	[Fact]
	public async Task StopVideoRecordingCopiesTheRecordedFileIntoTheStreamAndCleansUp()
	{
		SetupBuilder();
		var recordedBytes = Enumerable.Range(0, 2048).Select(index => (byte)(index % 251)).ToArray();
		var recordedFile = Path.Combine(Path.GetTempPath(), $"fake-{Guid.NewGuid()}.mov");
		await File.WriteAllBytesAsync(recordedFile, recordedBytes);

		try
		{
			await InvokeOnMainThreadAsync(async () =>
			{
				using var scope = new AppleCameraManagerScope(MauiContext);
				scope.Manager.CreatePlatformView();
				using var destination = new MemoryStream();
				destination.Write([1, 2, 3]);
				var output = SeedRecordingState(scope, recordedFile, destination, out var finalizeTcs);
				finalizeTcs.SetResult();

				var result = await scope.Manager.StopVideoRecording(CancellationToken.None);

				Assert.Same(destination, result);
				Assert.Equal(0, result.Position);
				byte[] expectedBytes = [1, 2, 3, .. recordedBytes];
				Assert.Equal(expectedBytes, destination.ToArray());
				Assert.False(File.Exists(recordedFile));
				Assert.Null(scope.Access.VideoRecordingFileName);
				Assert.Null(scope.Access.VideoOutput);
				Assert.Null(scope.Access.VideoRecordingFinalizeTcs);
				Assert.Null(scope.Access.VideoRecordingDelegate);
				Assert.Equal(NativeHandle.Zero, output.Handle);
				Assert.Empty(scope.Access.CaptureSession?.Outputs ?? []);
			});
		}
		finally
		{
			File.Delete(recordedFile);
		}
	}

	[Fact]
	public async Task StopVideoRecordingDoesNotRewindANonSeekableStream()
	{
		SetupBuilder();
		var recordedBytes = new byte[] { 9, 8, 7, 6, 5, 4 };
		var recordedFile = Path.Combine(Path.GetTempPath(), $"fake-{Guid.NewGuid()}.mov");
		await File.WriteAllBytesAsync(recordedFile, recordedBytes);

		try
		{
			await InvokeOnMainThreadAsync(async () =>
			{
				using var scope = new AppleCameraManagerScope(MauiContext);
				scope.Manager.CreatePlatformView();
				using var destination = new NonSeekableWriteStream();
				SeedRecordingState(scope, recordedFile, destination, out var finalizeTcs);
				finalizeTcs.SetResult();

				var result = await scope.Manager.StopVideoRecording(CancellationToken.None);

				Assert.Same(destination, result);
				Assert.Equal(recordedBytes, destination.WrittenBytes);
				Assert.False(File.Exists(recordedFile));
			});
		}
		finally
		{
			File.Delete(recordedFile);
		}
	}

	[Fact]
	public async Task StopVideoRecordingWithoutARecordedFileReturnsTheUntouchedStreamAndCleansUp()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			scope.Manager.CreatePlatformView();
			using var destination = new MemoryStream();
			var missingFile = Path.Combine(Path.GetTempPath(), $"fake-missing-{Guid.NewGuid()}.mov");
			SeedRecordingState(scope, missingFile, destination, out var finalizeTcs);
			finalizeTcs.SetResult();

			var result = await scope.Manager.StopVideoRecording(CancellationToken.None);

			Assert.Same(destination, result);
			Assert.Equal(0, destination.Length);
			Assert.Null(scope.Access.VideoRecordingFileName);
			Assert.Null(scope.Access.VideoOutput);
			Assert.Null(scope.Access.VideoRecordingFinalizeTcs);
		});
	}

	[Fact]
	public async Task StopVideoRecordingWaitsForTheRecordingToFinishAndHonoursCancellation()
	{
		SetupBuilder();
		var recordedFile = Path.Combine(Path.GetTempPath(), $"fake-{Guid.NewGuid()}.mov");
		await File.WriteAllBytesAsync(recordedFile, [1, 2, 3]);

		try
		{
			await InvokeOnMainThreadAsync(async () =>
			{
				using var scope = new AppleCameraManagerScope(MauiContext);
				scope.Manager.CreatePlatformView();
				using var destination = new MemoryStream();
				SeedRecordingState(scope, recordedFile, destination, out var finalizeTcs);
				using var cancellationTokenSource = new CancellationTokenSource();

				var stopTask = scope.Manager.StopVideoRecording(cancellationTokenSource.Token);
				Assert.False(stopTask.IsCompleted);
				await cancellationTokenSource.CancelAsync();

				await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stopTask);
				Assert.False(finalizeTcs.Task.IsCompleted);
				Assert.Equal(0, destination.Length);
			});
		}
		finally
		{
			File.Delete(recordedFile);
		}
	}

	[Fact]
	public async Task StopVideoRecordingCompletesOnlyOnceTheRecordingDelegateReportsFinished()
	{
		SetupBuilder();
		var recordedFile = Path.Combine(Path.GetTempPath(), $"fake-{Guid.NewGuid()}.mov");
		await File.WriteAllBytesAsync(recordedFile, [4, 5, 6]);

		try
		{
			await InvokeOnMainThreadAsync(async () =>
			{
				using var scope = new AppleCameraManagerScope(MauiContext);
				scope.Manager.CreatePlatformView();
				using var destination = new MemoryStream();
				var output = SeedRecordingState(scope, recordedFile, destination, out var finalizeTcs);
				var recordingDelegate = new AVCaptureMovieFileOutputRecordingDelegate(finalizeTcs);

				var stopTask = scope.Manager.StopVideoRecording(CancellationToken.None);
				Assert.False(stopTask.IsCompleted);
				recordingDelegate.FinishedRecording(output, NSUrl.FromFilename(recordedFile), [], null);
				var result = await stopTask;

				Assert.Same(destination, result);
				byte[] expectedBytes = [4, 5, 6];
				Assert.Equal(expectedBytes, destination.ToArray());
			});
		}
		finally
		{
			File.Delete(recordedFile);
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task RecordingDelegateCompletesTheFinalizeTaskWithOrWithoutAnError(bool hasError)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var output = new AVCaptureMovieFileOutput();
			var finalizeTcs = new TaskCompletionSource();
			var recordingDelegate = new AVCaptureMovieFileOutputRecordingDelegate(finalizeTcs);
			var error = hasError ? new NSError(new NSString("test-domain"), 7) : null;

			Assert.False(finalizeTcs.Task.IsCompleted);
			recordingDelegate.FinishedRecording(output, NSUrl.FromFilename("/tmp/test.mov"), [], error);

			// The task completes either way, so that StopVideoRecording, which waits for it, does not hang. The delegate keeps why the recording
			// failed, which StopVideoRecording throws as a CameraException when the recording left no usable movie
			// (CameraManagerAppleRecordingTests.StopVideoRecordingAfterARecordingWithoutAUsableMovieThrowsCameraException).
			Assert.True(finalizeTcs.Task.IsCompletedSuccessfully);
			if (error is null)
			{
				Assert.Null(recordingDelegate.FailureReason);
			}
			else
			{
				Assert.NotNull(recordingDelegate.FailureReason);
				Assert.StartsWith(error.LocalizedDescription, recordingDelegate.FailureReason);
			}
		});
	}

	[Fact]
	public async Task PhotoCaptureDelegateWrapperCompletesItsTaskWithTheCaptureResult()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var output = new AVCapturePhotoOutput();
			var wrapper = CameraManagerAccessor.CreatePhotoCaptureDelegateWrapper();
			var resultTask = CameraManagerAccessor.GetPhotoCaptureResultAsync(wrapper);
			Assert.False(resultTask.IsCompleted);
			var error = new NSError(new NSString("test-domain"), 42);

			wrapper.DidFinishProcessingPhoto(output, null!, error);
			var result = await resultTask;

			Assert.Same(output, result.Output);
			Assert.Null(result.Photo);
			Assert.Same(error, result.Error);
		});
	}

	[Fact]
	public async Task PhotoCaptureDelegateWrapperKeepsTheFirstResultWhenCalledTwice()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var firstOutput = new AVCapturePhotoOutput();
			using var secondOutput = new AVCapturePhotoOutput();
			var wrapper = CameraManagerAccessor.CreatePhotoCaptureDelegateWrapper();
			var resultTask = CameraManagerAccessor.GetPhotoCaptureResultAsync(wrapper);

			wrapper.DidFinishProcessingPhoto(firstOutput, null!, null);
			wrapper.DidFinishProcessingPhoto(secondOutput, null!, new NSError(new NSString("test-domain"), 1));
			var result = await resultTask;

			Assert.Same(firstOutput, result.Output);
			Assert.Null(result.Error);
		});
	}

	// Seeds what StartVideoRecording leaves behind: a movie output attached to the session, the file the recording writes to,
	// the destination stream and the TaskCompletionSource that the recording delegate completes.
	static AVCaptureMovieFileOutput SeedRecordingState(AppleCameraManagerScope scope, string recordedFile, Stream destination, out TaskCompletionSource finalizeTcs)
	{
		var session = scope.Access.CaptureSession ?? throw new InvalidOperationException("The manager has no capture session.");
		var output = new AVCaptureMovieFileOutput();
		Assert.True(session.CanAddOutput(output));
		session.AddOutput(output);

		finalizeTcs = new TaskCompletionSource();
		scope.Access.VideoOutput = output;
		scope.Access.VideoRecordingFileName = recordedFile;
		scope.Access.VideoRecordingStream = destination;
		scope.Access.VideoRecordingFinalizeTcs = finalizeTcs;

		return output;
	}

	static async Task<CameraException> StartCameraPreviewExpectingFailureAsync(AppleCameraManagerScope scope) =>
		await Assert.ThrowsAsync<CameraException>(() => scope.Manager.StartCameraPreview(CancellationToken.None));

	static UIWindowScene GetWindowScene()
	{
		IEnumerable<UIScene> scenes = UIApplication.SharedApplication.ConnectedScenes;

		return scenes.FirstOrDefault() as UIWindowScene ?? throw new InvalidOperationException("The test app has no UIWindowScene.");
	}

	// The independent oracle: the scene's own orientation, mapped by an explicit table.
	static AVCaptureVideoOrientation GetExpectedVideoOrientation(UIWindowScene windowScene)
	{
		var interfaceOrientation = OperatingSystem.IsIOSVersionAtLeast(26) || OperatingSystem.IsMacCatalystVersionAtLeast(26)
			? windowScene.EffectiveGeometry.InterfaceOrientation
			: windowScene.InterfaceOrientation;

		return interfaceOrientation switch
		{
			UIInterfaceOrientation.Portrait => AVCaptureVideoOrientation.Portrait,
			UIInterfaceOrientation.PortraitUpsideDown => AVCaptureVideoOrientation.PortraitUpsideDown,
			UIInterfaceOrientation.LandscapeLeft => AVCaptureVideoOrientation.LandscapeLeft,
			UIInterfaceOrientation.LandscapeRight => AVCaptureVideoOrientation.LandscapeRight,
			_ => AVCaptureVideoOrientation.Portrait
		};
	}

	async Task RotateWindowSceneAsync(UIInterfaceOrientationMask requestedMask, UIInterfaceOrientation expectedInterfaceOrientation)
	{
		await InvokeOnMainThreadAsync(() =>
		{
			var geometryPreferences = new UIWindowSceneGeometryPreferencesIOS(requestedMask);
			GetWindowScene().RequestGeometryUpdate(geometryPreferences, static _ =>
			{
				// The error handler runs on an arbitrary thread; a failed request shows up as the rotation timeout below.
			});
		});

		await AssertHelpers.AssertEventually(
			async () => await InvokeOnMainThreadAsync(() => GetWindowScene().InterfaceOrientation) == expectedInterfaceOrientation,
			timeout: 5000,
			message: $"The window scene did not rotate to {expectedInterfaceOrientation}.");
	}
}