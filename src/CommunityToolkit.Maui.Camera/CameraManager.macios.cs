using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using AVFoundation;
using CommunityToolkit.Maui.Extensions;
using CoreMedia;
using CoreMotion;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace CommunityToolkit.Maui.Core;

partial class CameraManager
{
	// TODO: Check if we really need this
	readonly NSDictionary<NSString, NSObject> codecSettings = new([AVVideo.CodecKey], [new NSString("jpeg")]);
	AVCaptureDeviceInput? audioInput;
	AVCaptureDevice? captureDevice;
	AVCaptureDeviceInput? captureInput;

	AVCaptureSession? captureSession;

	AVCaptureFlashMode flashMode;

	IDisposable? orientationDidChangeObserver;
	AVCapturePhotoOutput? photoOutput;
	PreviewView? previewView;

	AVCaptureVideoOrientation videoOrientation;
	AVCaptureMovieFileOutput? videoOutput;
	AVCaptureDeviceRotationCoordinator? rotationCoordinator;
	string? videoRecordingFileName;
	TaskCompletionSource? videoRecordingFinalizeTcs;
	Stream? videoRecordingStream;
	CMMotionManager? motionManager;

	/// <inheritdoc />
	public void Dispose()
	{
		CleanupVideoRecordingResources();

		captureSession?.StopRunning();
		captureSession?.Dispose();
		captureSession = null;

		captureInput?.Dispose();
		captureInput = null;

		captureDevice = null;

		orientationDidChangeObserver?.Dispose();
		orientationDidChangeObserver = null;

		photoOutput?.Dispose();
		photoOutput = null;

		previewView?.Dispose();
		previewView = null;

		videoRecordingStream?.Dispose();
		videoRecordingStream = null;

		rotationCoordinator?.Dispose();
		rotationCoordinator = null;

		motionManager?.StopAccelerometerUpdates();
		motionManager?.Dispose();
		motionManager = null;
	}

	public NativePlatformCameraPreviewView CreatePlatformView()
	{
		captureSession = new AVCaptureSession
		{
			SessionPreset = AVCaptureSession.PresetPhoto
		};

		previewView = new PreviewView
		{
			Session = captureSession
		};

		// use CMMotionManager to get device orientation on iOS 16 or lower, since AVCaptureDeviceRotationCoordinator is unavailable
		if (!UIDevice.CurrentDevice.CheckSystemVersion(17, 0))
		{
			motionManager ??= new();
			motionManager.StartAccelerometerUpdates();
		}
		orientationDidChangeObserver = UIDevice.Notifications.ObserveOrientationDidChange((_, _) => UpdateVideoOrientation());
		UpdateVideoOrientation();

		return previewView;
	}

	public partial void UpdateFlashMode(CameraFlashMode flashMode)
	{
		this.flashMode = flashMode.ToPlatform();
	}

	public partial void UpdateZoom(float zoomLevel)
	{
		if (!isInitialized || captureDevice is null)
		{
			return;
		}

		if (zoomLevel < (float)captureDevice.MinAvailableVideoZoomFactor || zoomLevel > (float)captureDevice.MaxAvailableVideoZoomFactor)
		{
			return;
		}

		captureDevice.LockForConfiguration(out NSError? error);
		if (error is not null)
		{
			Trace.WriteLine(error);
			return;
		}

		captureDevice.VideoZoomFactor = zoomLevel;
		captureDevice.UnlockForConfiguration();
	}

	public partial ValueTask UpdateCaptureResolution(Size resolution, CancellationToken token)
	{
		if (cameraView.SelectedCamera is null || captureDevice is null)
		{
			return ValueTask.CompletedTask;
		}

		captureDevice.LockForConfiguration(out NSError? error);
		if (error is not null)
		{
			Trace.WriteLine(error);
			return ValueTask.CompletedTask;
		}

		var formatsMatchingResolution = cameraView.SelectedCamera.SupportedFormats
			.Where(format => MatchesResolution(format, resolution))
			.ToList();

		var availableFormats = formatsMatchingResolution.Count is not 0
			? formatsMatchingResolution
			: GetPhotoCompatibleFormats(cameraView.SelectedCamera.SupportedFormats);

		var selectedFormat = availableFormats
			.OrderByDescending(f => f.ResolutionArea)
			.FirstOrDefault();

		if (selectedFormat is not null)
		{
			captureDevice.ActiveFormat = selectedFormat;
		}

		captureDevice.UnlockForConfiguration();
		return ValueTask.CompletedTask;
	}

	private async partial Task PlatformConnectCamera(CancellationToken token)
	{
		await PlatformStartCameraPreview(token);
	}

	private async partial Task PlatformStartCameraPreview(CancellationToken token)
	{
		if (captureSession is null)
		{
			return;
		}

		captureSession.BeginConfiguration();

		foreach (var input in captureSession.Inputs)
		{
			captureSession.RemoveInput(input);
			input.Dispose();
		}

		cameraView.SelectedCamera ??= cameraProvider.AvailableCameras?.FirstOrDefault() ?? throw new CameraException("No camera available on device");

		captureDevice = cameraView.SelectedCamera.CaptureDevice ?? throw new CameraException($"No Camera found");
		captureInput = new AVCaptureDeviceInput(captureDevice, out NSError? error);

		if (error is null && captureSession.CanAddInput(captureInput))
		{
			captureSession.AddInput(captureInput);
		}
		else
		{
			var errorMessage = error is not null
				? $"Error creating capture device input: {error.LocalizedDescription}"
				: "Unable to add capture device input to capture session.";

			captureInput.Dispose();
			captureInput = null;
			captureSession.CommitConfiguration();
			throw new CameraException(errorMessage);
		}

		// On iOS 17+, create a new instance of AVCaptureDeviceRotationCoordinator when switching to a new camera
		if (UIDevice.CurrentDevice.CheckSystemVersion(17, 0))
		{
			rotationCoordinator?.Dispose();
			rotationCoordinator = new(captureDevice, previewView?.Layer);
		}

		if (photoOutput is null)
		{
			photoOutput = new AVCapturePhotoOutput();
			captureSession.AddOutput(photoOutput);
		}

		await UpdateCaptureResolution(cameraView.ImageCaptureResolution, token);

		captureSession.CommitConfiguration();
		captureSession.StartRunning();
		isInitialized = true;
		onLoaded.Invoke();
	}

	private partial void PlatformStopCameraPreview()
	{
		if (captureSession is null)
		{
			return;
		}

		if (captureSession.Running)
		{
			captureSession.StopRunning();
		}

		isInitialized = false;
	}

	private partial void PlatformDisconnect()
	{
	}

	private async partial Task PlatformStartVideoRecording(Stream stream, CancellationToken token)
	{
		var isPermissionGranted = await AVCaptureDevice.RequestAccessForMediaTypeAsync(AVAuthorizationMediaType.Video).WaitAsync(token);
		if (!isPermissionGranted)
		{
			throw new CameraException("Camera permission is not granted. Please enable it in the app settings.");
		}

		if (captureSession is null)
		{
			throw new CameraException("Capture session is not initialized. Call ConnectCamera first.");
		}

		CleanupVideoRecordingResources();

		captureSession.BeginConfiguration();

		try
		{
			var audioDevice = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Audio);
			if (audioDevice is not null)
			{
				audioInput = new AVCaptureDeviceInput(audioDevice, out NSError? audioError);
				if (audioError is null && captureSession.CanAddInput(audioInput))
				{
					captureSession.AddInput(audioInput);
				}
				else
				{
					audioInput?.Dispose();
					audioInput = null;
				}
			}
		}
		catch
		{
			// Ignore audio configuration issues; proceed with video-only recording
		}

		videoOutput = new AVCaptureMovieFileOutput();

		if (!captureSession.CanAddOutput(videoOutput))
		{
			if (audioInput is not null)
			{
				captureSession.RemoveInput(audioInput);
				audioInput.Dispose();
				audioInput = null;
			}

			videoOutput?.Dispose();
			captureSession.CommitConfiguration();
			throw new CameraException("Unable to add video output to capture session.");
		}

		captureSession.AddOutput(videoOutput);
		captureSession.CommitConfiguration();

		if (!TryConfigureAVCaptureConnection(videoOutput, out var error))
		{
			Trace.TraceWarning(error);
		}

		videoRecordingStream = stream;
		videoRecordingFinalizeTcs = new TaskCompletionSource();
		videoRecordingFileName = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.mov");

		var outputUrl = NSUrl.FromFilename(videoRecordingFileName);
		videoOutput.StartRecordingToOutputFile(outputUrl, new AVCaptureMovieFileOutputRecordingDelegate(videoRecordingFinalizeTcs));
	}

	private async partial Task<Stream> PlatformStopVideoRecording(CancellationToken token)
	{
		if (captureSession is null
			|| videoRecordingFileName is null
			|| videoOutput is null
			|| videoRecordingStream is null
			|| videoRecordingFinalizeTcs is null)
		{
			return Stream.Null;
		}

		videoOutput.StopRecording();
		await videoRecordingFinalizeTcs.Task.WaitAsync(token);

		if (File.Exists(videoRecordingFileName))
		{
			await using var inputStream = new FileStream(videoRecordingFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
			await inputStream.CopyToAsync(videoRecordingStream, token);
			await videoRecordingStream.FlushAsync(token);
			if (videoRecordingStream.CanSeek)
			{
				videoRecordingStream.Position = 0;
			}
		}

		CleanupVideoRecordingResources();

		return videoRecordingStream;
	}

	void CleanupVideoRecordingResources()
	{
		if (captureSession is not null)
		{
			captureSession.BeginConfiguration();

			if (audioInput is not null)
			{
				captureSession.RemoveInput(audioInput);
				audioInput.Dispose();
			}

			if (videoOutput is not null)
			{
				captureSession.RemoveOutput(videoOutput);
				videoOutput.Dispose();
			}

			captureSession.CommitConfiguration();
		}

		videoOutput = null;
		audioInput = null;

		// Clean up temporary file
		if (videoRecordingFileName is not null)
		{
			if (File.Exists(videoRecordingFileName))
			{
				File.Delete(videoRecordingFileName);
			}

			videoRecordingFileName = null;
		}

		videoRecordingFinalizeTcs = null;
	}

	private async partial ValueTask PlatformTakePicture(CancellationToken token)
	{
		ArgumentNullException.ThrowIfNull(photoOutput);

		var capturePhotoSettings = AVCapturePhotoSettings.FromFormat(codecSettings);
		capturePhotoSettings.FlashMode = photoOutput.SupportedFlashModes.Contains(flashMode) ? flashMode : photoOutput.SupportedFlashModes.First();

		if (!TryConfigureAVCaptureConnection(photoOutput, out var errorMessage))
		{
			Trace.TraceWarning(errorMessage);
		}

		var wrapper = new AVCapturePhotoCaptureDelegateWrapper();

		photoOutput.CapturePhoto(capturePhotoSettings, wrapper);

		var result = await wrapper.Task.WaitAsync(token);
		if (result.Error is not null)
		{
			var failureReason = result.Error.LocalizedDescription;
			if (!string.IsNullOrEmpty(result.Error.LocalizedFailureReason))
			{
				failureReason = $"{failureReason} - {result.Error.LocalizedFailureReason}";
			}

			cameraView.OnMediaCapturedFailed(failureReason);
			return;
		}

		Stream? imageData;
		try
		{
			imageData = result.Photo.FileDataRepresentation?.AsStream();
		}
		catch (Exception e)
		{
			// possible exception: ObjCException NSInvalidArgumentException NSAllocateMemoryPages(...) failed in AVCapturePhoto.get_FileDataRepresentation()
			cameraView.OnMediaCapturedFailed($"Unable to retrieve the file data representation from the captured result: {e.Message}");
			return;
		}

		if (imageData is null)
		{
			cameraView.OnMediaCapturedFailed("Unable to retrieve the file data representation from the captured result.");
		}
		else
		{
			cameraView.OnMediaCaptured(imageData);
		}
	}

	static AVCaptureVideoOrientation GetVideoOrientationFromAccelerometer(double x, double y)
	{
		// Absolute values help determine which axis is dominant
		if (Math.Abs(y) >= Math.Abs(x))
		{
			return y > 0 ? AVCaptureVideoOrientation.PortraitUpsideDown : AVCaptureVideoOrientation.Portrait;
		}
		else
		{
			// x > 0 is LandscapeRight for device, which is LandscapeLeft for Video
			return x > 0 ? AVCaptureVideoOrientation.LandscapeLeft : AVCaptureVideoOrientation.LandscapeRight;
		}
	}

	static AVCaptureVideoOrientation GetVideoOrientation()
	{
		IEnumerable<UIScene> scenes = UIApplication.SharedApplication.ConnectedScenes;

		UIInterfaceOrientation interfaceOrientation;
		if (!(OperatingSystem.IsMacCatalystVersionAtLeast(26) || OperatingSystem.IsIOSVersionAtLeast(26)))
		{
			interfaceOrientation = scenes.FirstOrDefault() is UIWindowScene windowScene
				? windowScene.InterfaceOrientation
				: UIApplication.SharedApplication.StatusBarOrientation;
		}
		else
		{
			interfaceOrientation = scenes.FirstOrDefault() is UIWindowScene windowScene
				? windowScene.EffectiveGeometry.InterfaceOrientation
				: UIApplication.SharedApplication.StatusBarOrientation;
		}

		return interfaceOrientation switch
		{
			UIInterfaceOrientation.Portrait => AVCaptureVideoOrientation.Portrait,
			UIInterfaceOrientation.PortraitUpsideDown => AVCaptureVideoOrientation.PortraitUpsideDown,
			UIInterfaceOrientation.LandscapeRight => AVCaptureVideoOrientation.LandscapeRight,
			UIInterfaceOrientation.LandscapeLeft => AVCaptureVideoOrientation.LandscapeLeft,
			_ => AVCaptureVideoOrientation.Portrait
		};
	}

	bool TryConfigureAVCaptureConnection(in AVCaptureOutput captureOutput, [NotNullWhen(false)] out string? errorMessage)
	{
		errorMessage = null;

		if (AVMediaTypes.Video.GetConstant() is not NSString avMediaTypeVideo)
		{
			errorMessage = "Unable to determine video format.";
			return false;
		}

		if (captureOutput.ConnectionFromMediaType(avMediaTypeVideo) is not AVCaptureConnection captureConnection)
		{
			errorMessage = "Unable to determine video connection from media type.";
			return false;
		}

		// use AVCaptureDeviceRotationCoordinator to set captured photo and video orientation on iOS 17+
		if (UIDevice.CurrentDevice.CheckSystemVersion(17, 0))
		{
			if (rotationCoordinator is not null)
			{
				captureConnection.VideoRotationAngle = rotationCoordinator.VideoRotationAngleForHorizonLevelCapture;
			}
		}
		// use CMMotionManager to set captured photo and video orientation on iOS 16 and lower
		else
		{
			var data = motionManager?.AccelerometerData;
			if (data is not null)
			{
				var orientation = GetVideoOrientationFromAccelerometer(data.Acceleration.X, data.Acceleration.Y);
				captureConnection.VideoOrientation = orientation;
			}
		}

		if (captureConnection.SupportsVideoMirroring)
		{
			captureConnection.AutomaticallyAdjustsVideoMirroring = false;
			captureConnection.VideoMirrored = cameraView.SelectedCamera?.Position is CameraPosition.Front;
		}

		return true;
	}

	void UpdateVideoOrientation()
	{
		videoOrientation = GetVideoOrientation();
		previewView?.UpdatePreviewVideoOrientation(videoOrientation);
	}

	IEnumerable<AVCaptureDeviceFormat> GetPhotoCompatibleFormats(IEnumerable<AVCaptureDeviceFormat> formats)
	{
		if (photoOutput is not null)
		{
			var photoPixelFormats = photoOutput.GetSupportedPhotoPixelFormatTypesForFileType(nameof(AVFileTypes.Jpeg));
			return formats.Where(format => photoPixelFormats.Contains((NSNumber)format.FormatDescription.MediaSubType));
		}

		return formats;
	}

	static bool MatchesResolution(AVCaptureDeviceFormat format, Size resolution)
	{
		var dimensions = ((CMVideoFormatDescription)format.FormatDescription).Dimensions;
		return dimensions.Width <= resolution.Width
			   && dimensions.Height <= resolution.Height;
	}

	sealed class AVCapturePhotoCaptureDelegateWrapper : AVCapturePhotoCaptureDelegate
	{
		readonly TaskCompletionSource<CapturePhotoResult> taskCompletionSource = new();

		public Task<CapturePhotoResult> Task =>
			taskCompletionSource.Task;

		public override void DidFinishProcessingPhoto(AVCapturePhotoOutput output, AVCapturePhoto photo, NSError? error)
		{
			taskCompletionSource.TrySetResult(new()
			{
				Output = output,
				Photo = photo,
				Error = error
			});
		}
	}

	sealed record CapturePhotoResult
	{
		public required AVCapturePhotoOutput Output { get; init; }

		public required AVCapturePhoto Photo { get; init; }

		public NSError? Error { get; init; }
	}

	sealed class PreviewView : NativePlatformCameraPreviewView
	{
		public PreviewView()
		{
			PreviewLayer.VideoGravity = AVLayerVideoGravity.ResizeAspectFill;
		}

		public AVCaptureSession? Session
		{
			get => PreviewLayer.Session;
			set => PreviewLayer.Session = value;
		}

		AVCaptureVideoPreviewLayer PreviewLayer => (AVCaptureVideoPreviewLayer)Layer;

		[Export("layerClass")]
		public static Class GetLayerClass()
		{
			return new Class(typeof(AVCaptureVideoPreviewLayer));
		}

		public override void LayoutSubviews()
		{
			base.LayoutSubviews();
			UpdatePreviewVideoOrientation(GetVideoOrientation());
		}

		public void UpdatePreviewVideoOrientation(AVCaptureVideoOrientation videoOrientation)
		{
			if (PreviewLayer.Connection is not null)
			{
				PreviewLayer.Connection.VideoOrientation = videoOrientation;
			}
		}
	}
}

class AVCaptureMovieFileOutputRecordingDelegate(TaskCompletionSource taskCompletionSource) : AVCaptureFileOutputRecordingDelegate
{
	public override void FinishedRecording(AVCaptureFileOutput captureOutput, NSUrl outputFileUrl, NSObject[] connections, NSError? error)
	{
		taskCompletionSource.SetResult();
	}
}