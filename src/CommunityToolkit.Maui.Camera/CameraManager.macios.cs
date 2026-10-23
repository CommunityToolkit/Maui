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
	const string cameraPermissionNotGrantedMessage = "Camera permission is not granted. Please enable it in the app settings.";

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
	AVCaptureMovieFileOutputRecordingDelegate? videoRecordingDelegate;
	string? videoRecordingFileName;
	TaskCompletionSource? videoRecordingFinalizeTcs;
	Stream? videoRecordingStream;

	// A start must not release the recording that a stop still waits for AVFoundation to finish (and delete its movie), like on Android
	readonly SemaphoreSlim videoRecordingSemaphoreSlim = new(1, 1);
	CMMotionManager? motionManager;
	bool isDisposed;

	/// <inheritdoc />
	public void Dispose()
	{
		// A StopVideoRecording that still waits for AVFoundation to finish the recording completes with Stream.Null instead of using what is
		// released here (like on Android)
		isDisposed = true;
		var pendingVideoRecordingFinalizeTcs = videoRecordingFinalizeTcs;

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

		// The waiting stop does not depend on AVFoundation reporting the end of a recording whose session is gone (it completes asynchronously)
		pendingVideoRecordingFinalizeTcs?.TrySetResult();
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

	public partial void UpdateIsTorchOn(bool isTorchOn)
	{
		if (!isInitialized ||
			captureDevice is null ||
			!captureDevice.TorchAvailable)
		{
			return;
		}

		bool isCurrentlyOn = captureDevice.TorchActive;

		if (isCurrentlyOn != isTorchOn)
		{
			captureDevice.LockForConfiguration(out NSError? error);
			if (error is not null)
			{
				Trace.WriteLine(error);
				return;
			}

			captureDevice.TorchMode = isTorchOn ? AVCaptureTorchMode.On : AVCaptureTorchMode.Off;
			captureDevice.UnlockForConfiguration();
		}
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

		// The formats of the SelectedCamera belong to its own capture device. While the preview is stopped, a new SelectedCamera does not replace
		// the capture device (UpdateCurrentCamera only restarts a running preview), and AVFoundation raises NSInvalidArgumentException for the
		// format of another device; the next StartCameraPreview applies the resolution to the new camera
		if (cameraView.SelectedCamera.CaptureDevice != captureDevice)
		{
			return ValueTask.CompletedTask;
		}

		captureDevice.LockForConfiguration(out NSError? error);
		if (error is not null)
		{
			Trace.WriteLine(error);
			return ValueTask.CompletedTask;
		}

		// Setting the active format can raise (NSInvalidArgumentException for an unsupported format): the device is unlocked in any case
		try
		{
			// When no format fits the requested resolution (as with the default, Size.Zero), the device keeps its current format: the one that the
			// session preset (AVCaptureSessionPresetPhoto) chose, until setting an active format switched the session to
			// AVCaptureSessionPresetInputPriority, which keeps the format that was set last. A format picked among all formats, such as the
			// largest one, can be one that the photo output cannot capture from: on the iPhone 17 Pro capturePhotoWithSettings:delegate: then
			// raised "No active and enabled video connection" (#2920) and the preview stayed grey (#3037)
			var selectedFormat = cameraView.SelectedCamera.SupportedFormats
				.Where(format => MatchesResolution(format, resolution))
				.OrderByDescending(format => format.ResolutionArea)
				.FirstOrDefault();

			if (selectedFormat is not null)
			{
				captureDevice.ActiveFormat = selectedFormat;
			}
		}
		finally
		{
			captureDevice.UnlockForConfiguration();
		}

		return ValueTask.CompletedTask;
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

	static bool MatchesResolution(AVCaptureDeviceFormat format, Size resolution)
	{
		var dimensions = ((CMVideoFormatDescription)format.FormatDescription).Dimensions;
		return dimensions.Width <= resolution.Width
			   && dimensions.Height <= resolution.Height;
	}

	private async partial Task PlatformConnectCamera(CancellationToken token)
	{
		// CameraViewHandler connects the camera in ConnectHandler, right before MAUI maps the properties of the view. Started at once, the camera
		// would be started a second time by the mapping of SelectedCamera (UpdateCurrentCamera), which also reported a camera that cannot start
		// a second time. Like on Android and Windows, whose connections complete asynchronously, the camera starts once the mapping is done
		await Task.Yield();

		await PlatformStartCameraPreview(token);
	}

	private async partial Task PlatformStartCameraPreview(CancellationToken token)
	{
		if (captureSession is null)
		{
			return;
		}

		captureSession.BeginConfiguration();

		var isCameraAccessDenied = false;

		// The configuration is committed also when the start fails: AVCaptureSession raises NSGenericException when stopRunning
		// (which Dispose calls) or startRunning is called between beginConfiguration and commitConfiguration
		try
		{
			foreach (var input in captureSession.Inputs)
			{
				captureSession.RemoveInput(input);

				// A camera switch during a recording also removes the microphone input of the recording: it is forgotten before it is released,
				// so that stopping the recording or disposing the manager does not remove the released input again (ObjectDisposedException)
				if (ReferenceEquals(input, audioInput))
				{
					audioInput = null;
				}

				input.Dispose();
			}

			cameraView.SelectedCamera ??= cameraProvider.AvailableCameras?.FirstOrDefault() ?? throw new CameraException("No camera available on device");

			captureDevice = cameraView.SelectedCamera.CaptureDevice ?? throw new CameraException($"No Camera found");

			// FromDevice returns null and reports the reason through error when the device cannot be opened (for example while another app
			// uses it); the AVCaptureDeviceInput constructor throws a generic Exception instead (ObjCRuntime.Class.ThrowOnInitFailure)
			captureInput = AVCaptureDeviceInput.FromDevice(captureDevice, out NSError? error);

			if (captureInput is not null && error is null && captureSession.CanAddInput(captureInput))
			{
				captureSession.AddInput(captureInput);
			}
			else
			{
				isCameraAccessDenied = IsCameraAccessDenied(error);
				var errorMessage = isCameraAccessDenied
					? cameraPermissionNotGrantedMessage
					: error is not null
						? $"Error creating capture device input: {error.LocalizedDescription}"
						: "Unable to add capture device input to capture session.";

				captureInput?.Dispose();
				captureInput = null;
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
		}
		catch (Exception ex) when (ex is not OperationCanceledException && cameraView.SelectedCamera is not null && !isCameraAccessDenied)
		{
			// The selected camera cannot be started (it has no capture device, it cannot be opened, for example while another app uses it, or the
			// session rejects it): the preview stays started, so that selecting another camera starts the preview with that camera
			// (UpdateCurrentCamera). Without a camera, without the camera permission or when the start was cancelled, it stays stopped until
			// StartCameraPreview is called again
			isInitialized = true;
			throw;
		}
		finally
		{
			captureSession.CommitConfiguration();
		}

		captureSession.StartRunning();
		isInitialized = true;
		onLoaded.Invoke();
	}

	// AVFoundation refuses the device input of a camera that the app may not use (the camera permission was denied, or is restricted) with
	// AVErrorApplicationIsNotAuthorizedToUseDevice
	static bool IsCameraAccessDenied(NSError? error) =>
		error is not null
		&& error.Code == (nint)(long)AVError.ApplicationIsNotAuthorizedToUseDevice
		&& error.Domain == AVErrorKeys.ErrorDomain.ToString();

	private partial void PlatformStopCameraPreviewForCameraSwitch() => PlatformStopCameraPreview();

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
		await videoRecordingSemaphoreSlim.WaitAsync(token);

		try
		{
			await StartRecordingAsync(stream, token);
		}
		finally
		{
			videoRecordingSemaphoreSlim.Release();
		}
	}

	async Task StartRecordingAsync(Stream stream, CancellationToken token)
	{
		var isPermissionGranted = await AVCaptureDevice.RequestAccessForMediaTypeAsync(AVAuthorizationMediaType.Video).WaitAsync(token);
		if (!isPermissionGranted)
		{
			throw new CameraException(cameraPermissionNotGrantedMessage);
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

		try
		{
			videoOutput = new AVCaptureMovieFileOutput();

			if (!captureSession.CanAddOutput(videoOutput))
			{
				if (audioInput is not null)
				{
					captureSession.RemoveInput(audioInput);
					audioInput.Dispose();
					audioInput = null;
				}

				videoOutput.Dispose();
				videoOutput = null;
				throw new CameraException("Unable to add video output to capture session.");
			}

			captureSession.AddOutput(videoOutput);
		}
		finally
		{
			captureSession.CommitConfiguration();
		}

		if (!TryConfigureAVCaptureConnection(videoOutput, out var error))
		{
			// Without a usable video connection (for example without a camera input) the movie output can only record the microphone
			Trace.TraceWarning(error);
		}

		// startRecordingToOutputFileURL:recordingDelegate: raises NSInvalidArgumentException ("No active/enabled connections") when the movie
		// output has no connection that is active and enabled, for example without a camera input and without a microphone input
		if (!videoOutput.Connections.Any(static connection => connection.Active && connection.Enabled))
		{
			CleanupVideoRecordingResources();
			throw new CameraException("Unable to start video recording: no camera or microphone connection is active and enabled.");
		}

		videoRecordingStream = stream;

		// Completed by the recording delegate on a queue of AVFoundation, or by Dispose: the stop that waits for it never continues on that queue
		// or inside Dispose
		videoRecordingFinalizeTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		videoRecordingFileName = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.mov");

		var outputUrl = NSUrl.FromFilename(videoRecordingFileName);
		videoRecordingDelegate = new AVCaptureMovieFileOutputRecordingDelegate(videoRecordingFinalizeTcs);
		videoOutput.StartRecordingToOutputFile(outputUrl, videoRecordingDelegate);
	}

	private async partial Task<Stream> PlatformStopVideoRecording(CancellationToken token)
	{
		await videoRecordingSemaphoreSlim.WaitAsync(token);

		try
		{
			return await StopRecordingAsync(token);
		}
		finally
		{
			videoRecordingSemaphoreSlim.Release();
		}
	}

	async Task<Stream> StopRecordingAsync(CancellationToken token)
	{
		if (captureSession is null
			|| videoRecordingFileName is null
			|| videoOutput is null
			|| videoRecordingStream is null
			|| videoRecordingFinalizeTcs is null)
		{
			return Stream.Null;
		}

		// The recording that this stop ends: Dispose can release it while AVFoundation finishes it
		var finalizeTcs = videoRecordingFinalizeTcs;
		var recordingDelegate = videoRecordingDelegate;
		var fileName = videoRecordingFileName;
		var stream = videoRecordingStream;

		videoOutput.StopRecording();
		await finalizeTcs.Task.WaitAsync(token);

		// Dispose released the recording (deleted its movie, disposed the stream) while AVFoundation finished it: there is no video to return,
		// and nothing may escape from the async void StopVideoRecordingCommand (like on Android)
		if (isDisposed)
		{
			return Stream.Null;
		}

		try
		{
			var hasMovie = File.Exists(fileName);

			// When AVFoundation still reports the movie as successfully recorded (it stopped early, for example after an interruption or when
			// the disk filled up), the movie recorded until then is returned. Otherwise there is no usable movie: the caller gets the failure
			// instead of its stream, which would look like a successful recording
			if (recordingDelegate?.FailureReason is string failureReason)
			{
				if (!recordingDelegate.IsRecordingSuccessfullyFinished || !hasMovie)
				{
					throw new CameraException($"Video recording failed: {failureReason}");
				}

				Trace.TraceWarning($"The video recording stopped early, the movie recorded until then is returned: {failureReason}");
			}

			if (hasMovie)
			{
				await using var inputStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
				await inputStream.CopyToAsync(stream, token);
				await stream.FlushAsync(token);
				if (stream.CanSeek)
				{
					stream.Position = 0;
				}
			}
		}
		catch (ObjectDisposedException) when (isDisposed)
		{
			// Dispose ran while the movie was copied: it disposed the stream
			return Stream.Null;
		}
		finally
		{
			// Unless Dispose already released it
			if (ReferenceEquals(videoRecordingFinalizeTcs, finalizeTcs))
			{
				CleanupVideoRecordingResources();
			}
		}

		return stream;
	}

	void CleanupVideoRecordingResources()
	{
		if (captureSession is not null)
		{
			captureSession.BeginConfiguration();

			try
			{
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
			}
			finally
			{
				captureSession.CommitConfiguration();
			}
		}

		videoOutput = null;
		audioInput = null;

		// AVFoundation reports the end of every recording to its delegate, also after the movie output was removed (Dispose, a new
		// StartVideoRecording), and releases the delegate after that. A delegate whose recording has not finished is not disposed: AVFoundation
		// would call a managed object that no longer exists (the delegate type cannot be created again from its native object)
		if (videoRecordingDelegate?.HasFinished is true)
		{
			videoRecordingDelegate.Dispose();
		}

		videoRecordingDelegate = null;

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
		if (photoOutput is null)
		{
			// The camera never started (StartCameraPreview creates the photo output): CameraView.CaptureImage waits for MediaCaptured or
			// MediaCaptureFailed, so the capture that cannot start is reported, as on Windows
			cameraView.OnMediaCapturedFailed("Unable to capture an image because the camera is not initialized. Start the camera preview first.");
			return;
		}

		var capturePhotoSettings = AVCapturePhotoSettings.FromFormat(codecSettings);
		capturePhotoSettings.FlashMode = photoOutput.SupportedFlashModes.Contains(flashMode) ? flashMode : photoOutput.SupportedFlashModes.First();

		if (!TryConfigureAVCaptureConnection(photoOutput, out var errorMessage))
		{
			Trace.TraceWarning(errorMessage);
			cameraView.OnMediaCapturedFailed(errorMessage);
			return;
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

		// AVFoundation captures only through an active and enabled video connection (capturePhotoWithSettings:delegate: raises
		// NSInvalidArgumentException "No active and enabled video connection" otherwise); the session's configuration can make a connection inactive
		if (!captureConnection.Active || !captureConnection.Enabled)
		{
			errorMessage = "The video connection is not active and enabled.";
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
	// Written on the queue on which AVFoundation calls the delegate, read on the thread that releases the recording
	volatile bool hasFinished;

	public bool HasFinished => hasFinished;

	public string? FailureReason { get; private set; }

	public bool IsRecordingSuccessfullyFinished { get; private set; }

	public override void FinishedRecording(AVCaptureFileOutput captureOutput, NSUrl outputFileUrl, NSObject[] connections, NSError? error)
	{
		// AVFoundation finishes a recording that failed with an error (the disk is full, the session was interrupted, ...), and says in the error
		// whether the movie was written all the same
		if (error is null)
		{
			IsRecordingSuccessfullyFinished = true;
		}
		else
		{
			FailureReason = string.IsNullOrEmpty(error.LocalizedFailureReason)
				? error.LocalizedDescription
				: $"{error.LocalizedDescription} - {error.LocalizedFailureReason}";
			IsRecordingSuccessfullyFinished = error.UserInfo?[AVErrorKeys.RecordingSuccessfullyFinished] is NSNumber { BoolValue: true };
		}

		hasFinished = true;

		// Dispose may have completed the task already (an exception here would be raised on AVFoundation's queue)
		taskCompletionSource.TrySetResult();
	}
}