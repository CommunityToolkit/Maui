using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Views;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;

namespace CommunityToolkit.Maui.DeviceTests;

// The capture paths of CameraManager.windows.cs that need a camera: starting the preview, taking a photo, recording an MP4, zoom and
// capture resolution, switching cameras and the real CameraViewHandler. They skip visibly on machines without a camera, or when the
// Windows privacy settings deny the app access to it. The CI runners have no camera, so these tests only run on a Windows machine
// that has one.
//
// Device IDs are compared ignoring case: they are device interface paths, which Windows treats case-insensitively, and MediaCapture reports
// the ID it opened in lower case (\\?\usb#vid_...), while DeviceInformation and MediaFrameSourceGroup list it in upper case (\\?\USB#VID_...).
[Category(TestCategory.CameraWindows)]
[Collection(TestCollections.DisableParallelization)]
public class CameraManagerWindowsCaptureTests : CameraHandlerTestBase
{
	static readonly TimeSpan captureTimeout = TimeSpan.FromSeconds(30);

	[Fact]
	public async Task ConnectCameraInitializesTheMediaCaptureForTheSelectedCamera()
	{
		await RunWithConnectedCameraAsync((scope, camera) =>
		{
			var mediaCapture = scope.Access.MediaCapture ?? throw new InvalidOperationException("No MediaCapture.");

			Assert.Equal(camera.DeviceId, mediaCapture.MediaCaptureSettings.VideoDeviceId, ignoreCase: true);
			Assert.True(scope.Access.IsInitialized);
			Assert.Equal(1, scope.LoadedCount);

			return Task.CompletedTask;
		});
	}

	// AutoPlay is what makes the preview start when the source is assigned, and a MediaPlayerElement keeps it only while it is in a live visual
	// tree: outside one, setting it to true reads back false (Windows 11 26200), so the element is in a window, as in an app.
	[Fact]
	public async Task ConnectCameraAssignsTheColorFrameSourceToThePlayer()
	{
		await RunWithConnectedCameraAsync((scope, _) =>
		{
			var mediaCapture = scope.Access.MediaCapture ?? throw new InvalidOperationException("No MediaCapture.");
			Skip.IfNot(mediaCapture.FrameSources.Any(source => source.Value.Info.MediaStreamType == MediaStreamType.VideoRecord && source.Value.Info.SourceKind == MediaFrameSourceKind.Color),
				"The camera has no color frame source for MediaStreamType.VideoRecord, so the preview has no source.");

			var element = scope.Access.MediaElement ?? throw new InvalidOperationException("No MediaPlayerElement.");
			Assert.NotNull(element.Source);
			Assert.True(element.AutoPlay, $"AutoPlay is false (its MediaPlayer: {(element.MediaPlayer is null ? "none" : $"AutoPlay {element.MediaPlayer.AutoPlay}, {element.MediaPlayer.PlaybackSession.PlaybackState}")}).");

			return Task.CompletedTask;
		}, hostPlatformView: true);
	}

	[Fact]
	public async Task StopCameraPreviewReleasesTheMediaCaptureAndStartCameraPreviewCreatesANewOne()
	{
		await RunWithConnectedCameraAsync(async (scope, camera) =>
		{
			var firstCapture = scope.Access.MediaCapture;
			Assert.NotNull(firstCapture);

			scope.Manager.StopCameraPreview();
			Assert.Null(scope.Access.MediaCapture);
			Assert.False(scope.Access.IsInitialized);
			Assert.Null(scope.Access.MediaElement?.Source);

			await scope.Manager.StartCameraPreview(CancellationToken.None);

			var secondCapture = scope.Access.MediaCapture;
			Assert.NotNull(secondCapture);
			Assert.NotSame(firstCapture, secondCapture);
			Assert.Equal(camera.DeviceId, secondCapture.MediaCaptureSettings.VideoDeviceId, ignoreCase: true);
			Assert.True(scope.Access.IsInitialized);
			Assert.Equal(2, scope.LoadedCount);
		});
	}

	[Fact]
	public async Task TakePictureRaisesMediaCapturedWithJpegData()
	{
		await RunWithConnectedCameraAsync(async (scope, _) =>
		{
			var recorder = new WindowsMediaRecorder(scope.View);

			await scope.Manager.TakePicture(CancellationToken.None);
			var stream = await recorder.FirstCapture.Task.WaitAsync(captureTimeout);

			Assert.Equal(0, stream.Position);
			Assert.True(stream.Length > 1000);
			Assert.Equal(0xFF, stream.ReadByte());
			Assert.Equal(0xD8, stream.ReadByte());
			Assert.Empty(recorder.Failures);
			GC.KeepAlive(recorder);
		});
	}

	[Fact]
	public async Task UpdateCaptureResolutionAppliesTheLargestPhotoSizeThatFitsTheRequest()
	{
		await RunWithConnectedCameraAsync(async (scope, camera) =>
		{
			var mediaCapture = scope.Access.MediaCapture ?? throw new InvalidOperationException("No MediaCapture.");
			Skip.If(camera.ImageEncodingProperties.Count < 3, "The camera offers fewer than three photo sizes.");
			var requested = camera.ImageEncodingProperties[camera.ImageEncodingProperties.Count / 2];

			await scope.Manager.UpdateCaptureResolution(new Size(requested.Width, requested.Height), CancellationToken.None);

			var applied = Assert.IsType<ImageEncodingProperties>(mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.Photo), exactMatch: false);
			Assert.Equal((requested.Width, requested.Height), (applied.Width, applied.Height));
		});
	}

	[Fact]
	public async Task UpdateCaptureResolutionFallsBackToTheLargestPhotoSizeWhenNoSizeFitsTheRequest()
	{
		await RunWithConnectedCameraAsync(async (scope, camera) =>
		{
			var mediaCapture = scope.Access.MediaCapture ?? throw new InvalidOperationException("No MediaCapture.");
			Skip.If(camera.ImageEncodingProperties.Count is 0, "The camera offers no photo sizes: its photo stream lists only VideoEncodingProperties (it takes photos from its video stream), and CameraProvider lists only ImageEncodingProperties.");
			var largest = camera.ImageEncodingProperties.OrderByDescending(properties => properties.Width * properties.Height).First();

			await scope.Manager.UpdateCaptureResolution(new Size(1, 1), CancellationToken.None);

			var applied = Assert.IsType<ImageEncodingProperties>(mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.Photo), exactMatch: false);
			Assert.Equal((largest.Width, largest.Height), (applied.Width, applied.Height));
		});
	}

	[Fact]
	public async Task UpdateZoomAppliesTheRequestedFactorRoundedUpToTheNextStep()
	{
		await RunWithConnectedCameraAsync((scope, _) =>
		{
			var zoomControl = (scope.Access.MediaCapture ?? throw new InvalidOperationException("No MediaCapture.")).VideoDeviceController.ZoomControl;
			Skip.IfNot(zoomControl.Supported, "The camera does not support zoom.");
			var step = zoomControl.Step;
			// With a step of 0 the expected value below would be NaN, which this test must not accept: see UpdateZoomWithACameraThatReportsNoZoomStepAppliesTheRequestedFactor.
			Skip.If(step <= 0, "The camera reports no zoom step.");
			var requested = zoomControl.Min + (step / 2f);
			Skip.If(requested > zoomControl.Max, "The camera's zoom range is smaller than one step.");
			var expected = requested % step != 0 ? (float)Math.Ceiling(requested / step) * step : requested;

			scope.Manager.UpdateZoom(requested);

			Assert.Equal(expected, zoomControl.Value, precision: 2);

			return Task.CompletedTask;
		});
	}

	[Fact]
	public async Task UpdateZoomAppliesAFactorThatIsAMultipleOfTheStepAsIs()
	{
		await RunWithConnectedCameraAsync((scope, _) =>
		{
			var zoomControl = (scope.Access.MediaCapture ?? throw new InvalidOperationException("No MediaCapture.")).VideoDeviceController.ZoomControl;
			Skip.IfNot(zoomControl.Supported, "The camera does not support zoom.");
			var step = zoomControl.Step;
			Skip.If(step <= 0, "The camera reports no zoom step.");
			var requested = (float)Math.Ceiling(zoomControl.Min / step) * step;
			Skip.If(requested > zoomControl.Max, "No multiple of the zoom step lies in the camera's zoom range.");

			scope.Manager.UpdateZoom(requested);

			Assert.Equal(requested, zoomControl.Value, precision: 2);

			return Task.CompletedTask;
		});
	}

	// UpdateZoom divides by ZoomControl.Step without a guard (CameraManager.windows.cs:83-90): for a supported zoom control that reports a step
	// of 0, `zoomLevel % 0` and `Math.Ceiling(zoomLevel / 0) * 0` are NaN, so the manager would write NaN to ZoomControl.Value. Whether a camera
	// driver reports such a step is unknown (the step tests above skip on one), so this test only runs on such a camera; there it expects the
	// requested factor, which fails if the missing guard matters.
	[Fact]
	public async Task UpdateZoomWithACameraThatReportsNoZoomStepAppliesTheRequestedFactor()
	{
		await RunWithConnectedCameraAsync((scope, _) =>
		{
			var zoomControl = (scope.Access.MediaCapture ?? throw new InvalidOperationException("No MediaCapture.")).VideoDeviceController.ZoomControl;
			Skip.IfNot(zoomControl.Supported, "The camera does not support zoom.");
			Skip.IfNot(zoomControl.Step == 0, $"The camera reports a zoom step of {zoomControl.Step}: the step tests above cover it.");
			Skip.If(zoomControl.Max <= zoomControl.Min, "The camera's zoom range is empty.");
			var requested = zoomControl.Max;

			scope.Manager.UpdateZoom(requested);

			Assert.False(float.IsNaN(zoomControl.Value), "UpdateZoom wrote NaN to ZoomControl.Value.");
			Assert.Equal(requested, zoomControl.Value, precision: 2);

			return Task.CompletedTask;
		});
	}

	[Theory]
	[InlineData(CameraFlashMode.Off, false, false)]
	[InlineData(CameraFlashMode.On, true, false)]
	[InlineData(CameraFlashMode.Auto, true, true)]
	public async Task UpdateFlashModeAppliesTheModeToTheFlashControl(CameraFlashMode flashMode, bool expectedEnabled, bool expectedAuto)
	{
		await RunWithConnectedCameraAsync((scope, _) =>
		{
			var flashControl = (scope.Access.MediaCapture ?? throw new InvalidOperationException("No MediaCapture.")).VideoDeviceController.FlashControl;
			Skip.IfNot(flashControl.Supported, "The camera has no flash.");
			// Off leaves Auto alone, so start from the opposite of what the mode sets.
			flashControl.Enabled = !expectedEnabled;
			flashControl.Auto = !expectedAuto;

			scope.Manager.UpdateFlashMode(flashMode);

			Assert.Equal(expectedEnabled, flashControl.Enabled);
			Assert.Equal(flashMode is CameraFlashMode.Off ? !expectedAuto : expectedAuto, flashControl.Auto);

			return Task.CompletedTask;
		});
	}

	[Fact]
	public async Task UpdateFlashModeWithAnUndefinedValueThrowsNotSupportedException()
	{
		await RunWithConnectedCameraAsync((scope, _) =>
		{
			var flashControl = (scope.Access.MediaCapture ?? throw new InvalidOperationException("No MediaCapture.")).VideoDeviceController.FlashControl;
			Skip.IfNot(flashControl.Supported, "The camera has no flash.");

			var exception = Assert.Throws<NotSupportedException>(() => scope.Manager.UpdateFlashMode((CameraFlashMode)42));

			Assert.Equal("42 is not yet supported", exception.Message);

			return Task.CompletedTask;
		});
	}

	[Fact]
	public async Task UpdateFlashModeIsIgnoredWhenTheCameraHasNoFlash()
	{
		await RunWithConnectedCameraAsync((scope, _) =>
		{
			var flashControl = (scope.Access.MediaCapture ?? throw new InvalidOperationException("No MediaCapture.")).VideoDeviceController.FlashControl;
			Skip.If(flashControl.Supported, "The camera has a flash: UpdateFlashModeAppliesTheModeToTheFlashControl covers it.");
			var enabledBefore = ReadControlState(() => flashControl.Enabled);

			var exception = Record.Exception(() =>
			{
				scope.Manager.UpdateFlashMode((CameraFlashMode)42);
				scope.Manager.UpdateFlashMode(CameraFlashMode.On);
			});

			// Past the guard the manager would throw NotSupportedException for the undefined mode and write Enabled, which an unsupported
			// FlashControl refuses (COMException "The request is not supported", which reading Enabled throws too).
			Assert.Null(exception);
			Assert.Equal(enabledBefore, ReadControlState(() => flashControl.Enabled));

			return Task.CompletedTask;
		});
	}

	[Fact]
	public async Task UpdateIsTorchOnTurnsTheTorchOnAndOff()
	{
		await RunWithConnectedCameraAsync((scope, _) =>
		{
			var torchControl = (scope.Access.MediaCapture ?? throw new InvalidOperationException("No MediaCapture.")).VideoDeviceController.TorchControl;
			Skip.IfNot(torchControl.Supported, "The camera has no torch.");
			try
			{
				scope.Manager.UpdateIsTorchOn(true);
				Assert.True(torchControl.Enabled);

				scope.Manager.UpdateIsTorchOn(true);
				Assert.True(torchControl.Enabled);

				scope.Manager.UpdateIsTorchOn(false);
				Assert.False(torchControl.Enabled);
			}
			finally
			{
				torchControl.Enabled = false;
			}

			return Task.CompletedTask;
		});
	}

	[Fact]
	public async Task UpdateIsTorchOnIsIgnoredWhenTheCameraHasNoTorch()
	{
		await RunWithConnectedCameraAsync((scope, _) =>
		{
			var torchControl = (scope.Access.MediaCapture ?? throw new InvalidOperationException("No MediaCapture.")).VideoDeviceController.TorchControl;
			Skip.If(torchControl.Supported, "The camera has a torch: UpdateIsTorchOnTurnsTheTorchOnAndOff covers it.");
			var enabledBefore = ReadControlState(() => torchControl.Enabled);

			var exception = Record.Exception(() => scope.Manager.UpdateIsTorchOn(true));

			// Past the guard the manager would read and write Enabled, which an unsupported TorchControl refuses (COMException "The request is
			// not supported").
			Assert.Null(exception);
			Assert.Equal(enabledBefore, ReadControlState(() => torchControl.Enabled));

			return Task.CompletedTask;
		});
	}

	[Fact]
	public async Task UpdateZoomIsIgnoredWhenTheCameraHasNoZoom()
	{
		await RunWithConnectedCameraAsync((scope, _) =>
		{
			var zoomControl = (scope.Access.MediaCapture ?? throw new InvalidOperationException("No MediaCapture.")).VideoDeviceController.ZoomControl;
			Skip.If(zoomControl.Supported, "The camera supports zoom: UpdateZoomAppliesTheRequestedFactorRoundedUpToTheNextStep covers it.");
			var valueBefore = ReadZoomValue(zoomControl);

			var exception = Record.Exception(() => scope.Manager.UpdateZoom(2f));

			// Past the guard the manager would divide by the control's step and write the result to Value, which throws or changes it.
			Assert.Null(exception);
			Assert.Equal(valueBefore, ReadZoomValue(zoomControl));

			return Task.CompletedTask;
		});
	}

	[Fact]
	public async Task UpdateFlashModeIsIgnoredWhileTheCameraIsNotInitialized()
	{
		await RunWithConnectedCameraAsync((scope, _) =>
		{
			var flashControl = (scope.Access.MediaCapture ?? throw new InvalidOperationException("No MediaCapture.")).VideoDeviceController.FlashControl;
			Skip.IfNot(flashControl.Supported, "The camera has no flash.");
			flashControl.Enabled = false;
			scope.Access.IsInitialized = false;

			scope.Manager.UpdateFlashMode(CameraFlashMode.On);

			Assert.False(flashControl.Enabled);
			scope.Access.IsInitialized = true;

			return Task.CompletedTask;
		});
	}

	[Fact]
	public async Task UpdateIsTorchOnIsIgnoredWhileTheCameraIsNotInitialized()
	{
		await RunWithConnectedCameraAsync((scope, _) =>
		{
			var torchControl = (scope.Access.MediaCapture ?? throw new InvalidOperationException("No MediaCapture.")).VideoDeviceController.TorchControl;
			Skip.IfNot(torchControl.Supported, "The camera has no torch.");
			torchControl.Enabled = false;
			scope.Access.IsInitialized = false;

			scope.Manager.UpdateIsTorchOn(true);

			Assert.False(torchControl.Enabled);
			scope.Access.IsInitialized = true;

			return Task.CompletedTask;
		});
	}

	[Fact]
	public async Task UpdateZoomIsIgnoredWhileTheCameraIsNotInitialized()
	{
		await RunWithConnectedCameraAsync((scope, _) =>
		{
			var zoomControl = (scope.Access.MediaCapture ?? throw new InvalidOperationException("No MediaCapture.")).VideoDeviceController.ZoomControl;
			Skip.IfNot(zoomControl.Supported, "The camera does not support zoom.");
			Skip.If(zoomControl.Max <= zoomControl.Min, "The camera's zoom range is empty.");
			zoomControl.Value = zoomControl.Min;
			scope.Access.IsInitialized = false;

			scope.Manager.UpdateZoom(zoomControl.Max);

			Assert.Equal(zoomControl.Min, zoomControl.Value, precision: 2);
			scope.Access.IsInitialized = true;

			return Task.CompletedTask;
		});
	}

	[Fact]
	public async Task StopVideoRecordingWhileTheCameraIsNotInitializedReturnsTheNullStream()
	{
		await RunWithRecordingAsync(async (scope, destination) =>
		{
			scope.Access.IsInitialized = false;

			var result = await scope.Manager.StopVideoRecording(CancellationToken.None);

			Assert.Same(Stream.Null, result);
			scope.Access.IsInitialized = true;
		});
	}

	[Fact]
	public async Task StopVideoRecordingWithoutAPlatformViewReturnsTheNullStream()
	{
		await RunWithRecordingAsync(async (scope, destination) =>
		{
			var mediaElement = scope.Access.MediaElement;
			scope.Access.MediaElement = null;

			var result = await scope.Manager.StopVideoRecording(CancellationToken.None);

			Assert.Same(Stream.Null, result);
			scope.Access.MediaElement = mediaElement;
		});
	}

	[Fact]
	public async Task StartVideoRecordingWithANonSeekableStreamThrowsNotSupportedException()
	{
		await RunWithConnectedCameraAsync(async (scope, _) =>
		{
			using var destination = new NonSeekableStream();

			// MediaCapture records into an IRandomAccessStream; Stream.AsRandomAccessStream refuses a stream that cannot seek.
			await Assert.ThrowsAsync<NotSupportedException>(() => scope.Manager.StartVideoRecording(destination, CancellationToken.None));
		});
	}

	// A StartVideoRecording that fails during a recording (AsRandomAccessStream refuses a stream that cannot seek) keeps the earlier
	// LowLagMediaRecording, so it must keep its stream too: the next StopVideoRecording stops the earlier recording and must return its stream,
	// rewound and with the movie, not the refused one (needs a camera).
	[Fact]
	public async Task StartVideoRecordingThatFailsDuringARecordingKeepsTheStreamOfTheRunningRecording()
	{
		await RunWithRecordingAsync(async (scope, destination) =>
		{
			using var refused = new NonSeekableStream();
			await Assert.ThrowsAsync<NotSupportedException>(() => scope.Manager.StartVideoRecording(refused, CancellationToken.None));

			Assert.Same(destination, scope.Access.VideoCaptureStream);
			var result = await scope.Manager.StopVideoRecording(CancellationToken.None);

			Assert.Same(destination, result);
			Assert.Equal(0, result.Position);
			Assert.True(destination.Length > 0);
		});
	}

	// A stop ends the recording, as on Android, iOS and Mac Catalyst: a second StopVideoRecording finds no recording and returns Stream.Null.
	// CameraManager.windows.cs used to stop the stopped LowLagMediaRecording again, which throws COMException ("The op is invalid. RecordState").
	[Fact]
	public async Task StopVideoRecordingAfterTheRecordingStoppedReturnsTheNullStream()
	{
		await RunWithRecordingAsync(async (scope, destination) =>
		{
			var result = await scope.Manager.StopVideoRecording(CancellationToken.None);
			Assert.Same(destination, result);
			Assert.Null(scope.Access.MediaRecording);
			Assert.Null(scope.Access.VideoCaptureStream);

			var second = await scope.Manager.StopVideoRecording(CancellationToken.None);

			Assert.Same(Stream.Null, second);
		});
	}

	[Fact]
	public async Task StartAndStopVideoRecordingCopiesAnMp4IntoTheStream()
	{
		await RunWithConnectedCameraAsync(async (scope, _) =>
		{
			var mediaCapture = scope.Access.MediaCapture ?? throw new InvalidOperationException("No MediaCapture.");
			Skip.IfNot(mediaCapture.FrameSources.Any(source => source.Value.Info.MediaStreamType == MediaStreamType.VideoRecord && source.Value.Info.SourceKind == MediaFrameSourceKind.Color),
				"The camera has no color frame source for MediaStreamType.VideoRecord, so recording does not start.");
			using var destination = new MemoryStream();

			await scope.Manager.StartVideoRecording(destination, CancellationToken.None);
			Assert.Same(destination, scope.Access.VideoCaptureStream);
			// Exception to the house rule against fixed-time waits: the recording needs a moment of footage, and nothing reports when it has
			// some (LowLagMediaRecording and MediaCapture raise no progress event, and the MP4 sink may only write the movie when the recording
			// stops, so the stream cannot be watched either). This test needs a camera, so it does not run in CI.
			var recordingTime = System.Diagnostics.Stopwatch.StartNew();
			await AssertHelpers.AssertEventually(() => recordingTime.ElapsedMilliseconds >= 1000, timeout: 5000, message: "The recording time did not elapse.");
			var result = await scope.Manager.StopVideoRecording(CancellationToken.None);

			Assert.Same(destination, result);
			Assert.Equal(0, result.Position);
			Assert.True(destination.Length > 100);
			byte[] fileTypeBox = "ftyp"u8.ToArray();
			Assert.Equal(fileTypeBox, destination.ToArray().AsSpan(4, 4).ToArray());
		});
	}

	[Fact]
	public async Task UpdateCurrentCameraSwitchesThePreviewToTheOtherCamera()
	{
		var cameras = await DiscoverCamerasOrSkipAsync();
		DeviceTestCapabilities.Require(DeviceTestCapabilities.SecondCamera, cameras.Count >= 2, $"Needs two cameras (found {cameras.Count}).");

		await RunWithConnectedCameraAsync(async (scope, first) =>
		{
			var other = cameras.First(camera => camera.DeviceId != first.DeviceId);
			scope.View.SelectedCamera = other;

			await scope.Manager.UpdateCurrentCamera(other, CancellationToken.None);

			Assert.Equal(other.DeviceId, scope.Access.MediaCapture?.MediaCaptureSettings.VideoDeviceId, ignoreCase: true);
			Assert.True(scope.Access.IsInitialized);
			Assert.Equal(2, scope.LoadedCount);
		});
	}

	// A switch to a camera that cannot be opened keeps the manager initialized (see
	// CameraManagerWindowsTests.UpdateCurrentCameraAfterASwitchThatFailedOpensTheNextSelectedCamera), so selecting the real camera again
	// opens it and the preview runs again.
	[Fact]
	public async Task UpdateCurrentCameraBackToTheCameraAfterASwitchThatFailedStartsItAgain()
	{
		await RunWithConnectedCameraAsync(async (scope, camera) =>
		{
			using var errors = new CameraErrorRecorder(scope.View);
			var unknownCamera = new CameraInfo("Unknown camera", "fake-unknown-device", CameraPosition.Rear, false, 1f, 1f, [], null!, []);
			scope.View.SelectedCamera = unknownCamera;

			await scope.Manager.UpdateCurrentCamera(unknownCamera, CancellationToken.None);

			Assert.Equal(CameraManagerWindowsTests.CameraThatCannotBeOpenedMessage, Assert.IsType<CameraException>(Assert.Single(errors.Errors)).Message);
			Assert.True(scope.Access.IsInitialized);
			Assert.Null(scope.Access.MediaCapture);

			scope.View.SelectedCamera = camera;
			await scope.Manager.UpdateCurrentCamera(camera, CancellationToken.None);

			Assert.Equal(camera.DeviceId, scope.Access.MediaCapture?.MediaCaptureSettings.VideoDeviceId, ignoreCase: true);
			Assert.True(scope.Access.IsInitialized);
			Assert.Equal(2, scope.LoadedCount);
		});
	}

	[Fact]
	public async Task TheHandlerConnectsTheCameraByItselfAndCaptureImageReturnsAJpeg()
	{
		var cameras = await DiscoverCamerasOrSkipAsync();
		Skip.If(cameras.Count is 0, "The camera provider found no camera.");
		SetupBuilder();
		var view = new CameraView { WidthRequest = 320, HeightRequest = 240 };

		await AttachAndRun<CameraViewHandler>(view, async handler =>
		{
			await view.OnLoadedAsync();
			var access = new WindowsCameraManagerAccessor(handler.CameraManager);
			await AssertHelpers.AssertEventually(() => access.IsInitialized, timeout: 30000, message: "The handler did not start the camera preview.");

			Assert.True(view.IsAvailable);
			Assert.Equal(1f, view.ZoomFactor);
			Assert.NotNull(view.SelectedCamera);
			var stream = await view.CaptureImage(CancellationToken.None).WaitAsync(captureTimeout);
			Assert.True(stream.Length > 1000);
			Assert.Equal(0xFF, stream.ReadByte());
			Assert.Equal(0xD8, stream.ReadByte());
		});

		// WinUI objects may only be used on their own thread: OnUnloadedAsync reads FrameworkElement.IsLoaded.
		await InvokeOnMainThreadAsync(async () =>
		{
			await view.OnUnloadedAsync();
			view.Handler?.DisconnectHandler();
			view.Handler = null;
		});
	}

	// The value of a zoom control, or the type of the exception that reading it throws (an unsupported control may not report one).
	static string ReadZoomValue(Windows.Media.Devices.ZoomControl zoomControl)
	{
		try
		{
			return zoomControl.Value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
		}
		catch (Exception exception)
		{
			return exception.GetType().Name;
		}
	}

	// The Enabled value of a flash or torch control, or the type of the exception that reading it throws (an unsupported control refuses it).
	static string ReadControlState(Func<bool> readEnabled)
	{
		try
		{
			return readEnabled().ToString();
		}
		catch (Exception exception)
		{
			return exception.GetType().Name;
		}
	}

	static async Task<IReadOnlyList<CameraInfo>> DiscoverCamerasOrSkipAsync()
	{
		DeviceTestCapabilities.Require(DeviceTestCapabilities.Camera, await WindowsCameraEnvironment.HasVideoCaptureDeviceAsync(), $"Needs a camera ({await WindowsCameraEnvironment.DescribeCameraAccessAsync()}).");

		try
		{
			var cameras = await WindowsCameraEnvironment.DiscoverCamerasAsync();
			Skip.If(cameras.Count is 0, "The camera provider found no camera.");

			return cameras;
		}
		catch (UnauthorizedAccessException exception)
		{
			throw new SkipException($"Camera access is denied for this app in the Windows privacy settings ({exception.Message}).");
		}
	}

	// Starts a recording into a MemoryStream on the connected camera, runs the test, and stops the recording if the test left it running.
	async Task RunWithRecordingAsync(Func<WindowsCameraManagerScope, MemoryStream, Task> test)
	{
		await RunWithConnectedCameraAsync(async (scope, _) =>
		{
			var mediaCapture = scope.Access.MediaCapture ?? throw new InvalidOperationException("No MediaCapture.");
			Skip.IfNot(mediaCapture.FrameSources.Any(source => source.Value.Info.MediaStreamType == MediaStreamType.VideoRecord && source.Value.Info.SourceKind == MediaFrameSourceKind.Color),
				"The camera has no color frame source for MediaStreamType.VideoRecord, so recording does not start.");
			using var destination = new MemoryStream();
			await scope.Manager.StartVideoRecording(destination, CancellationToken.None);
			Assert.NotNull(scope.Access.MediaRecording);

			try
			{
				await test(scope, destination);
			}
			finally
			{
				scope.Access.IsInitialized = true;
				await scope.Manager.StopVideoRecording(CancellationToken.None);
			}
		});
	}

	async Task RunWithConnectedCameraAsync(Func<WindowsCameraManagerScope, CameraInfo, Task> test, bool hostPlatformView = false)
	{
		var cameras = await DiscoverCamerasOrSkipAsync();
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var provider = new WindowsFakeCameraProvider { AvailableCameras = cameras };
			using var scope = new WindowsCameraManagerScope(MauiContext, provider);
			var platformView = scope.Manager.CreatePlatformView();

			if (hostPlatformView)
			{
				platformView.Width = 320;
				platformView.Height = 240;
				await platformView.AttachAndRun(ConnectAndTestAsync, MauiContext);
			}
			else
			{
				await ConnectAndTestAsync();
			}

			async Task ConnectAndTestAsync()
			{
				try
				{
					try
					{
						await scope.Manager.ConnectCamera(CancellationToken.None);
					}
					catch (UnauthorizedAccessException exception)
					{
						throw new SkipException($"Camera access is denied for this app in the Windows privacy settings ({exception.Message}).");
					}

					await test(scope, scope.View.SelectedCamera ?? throw new InvalidOperationException("ConnectCamera selected no camera."));
				}
				finally
				{
					scope.Manager.StopCameraPreview();
				}
			}
		});
	}

	// A writable stream that cannot seek, such as a network or pipe stream.
	sealed class NonSeekableStream : Stream
	{
		public override bool CanRead => false;

		public override bool CanSeek => false;

		public override bool CanWrite => true;

		public override long Length => throw new NotSupportedException();

		public override long Position
		{
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}

		public override void Flush()
		{
		}

		public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

		public override void SetLength(long value) => throw new NotSupportedException();

		public override void Write(byte[] buffer, int offset, int count)
		{
		}
	}
}