using System.Diagnostics;
using AVFoundation;
using CommunityToolkit.Maui.Extensions;
using CoreMedia;
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
	AVCaptureInput? captureInput;

	AVCaptureSession? captureSession;

	AVCaptureFlashMode flashMode;

	IDisposable? orientationDidChangeObserver;
	AVCapturePhotoOutput? photoOutput;
	PreviewView? previewView;

	AVCaptureDeviceInput? videoInput;
	AVCaptureVideoOrientation videoOrientation;
	AVCaptureMovieFileOutput? videoOutput;
	string? videoRecordingFileName;
	TaskCompletionSource? videoRecordingFinalizeTcs;
	Stream? videoRecordingStream;

	/// <inheritdoc />
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
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
		if (!IsInitialized || captureDevice is null)
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

	public async partial ValueTask UpdateCaptureResolution(Size resolution, CancellationToken token)
	{
		if (captureDevice is null)
		{
			return;
		}

		captureDevice.LockForConfiguration(out NSError? error);
		if (error is not null)
		{
			Trace.WriteLine(error);
			return;
		}

		if (cameraView.SelectedCamera is null)
		{
			await cameraProvider.RefreshAvailableCameras(token);
			cameraView.SelectedCamera = cameraProvider.AvailableCameras?.FirstOrDefault() ?? throw new CameraException("No camera available on device");
		}

		var filteredFormatList = cameraView.SelectedCamera.SupportedFormats.Where(f =>
		{
			var d = ((CMVideoFormatDescription)f.FormatDescription).Dimensions;
			return d.Width <= resolution.Width && d.Height <= resolution.Height;
		}).ToList();

		filteredFormatList = [.. (filteredFormatList.Count is not 0 ? filteredFormatList : cameraView.SelectedCamera.SupportedFormats)
			.OrderByDescending(f =>
			{
				var d = ((CMVideoFormatDescription)f.FormatDescription).Dimensions;
				return d.Width * d.Height;
			})];

		if (filteredFormatList.Count is not 0)
		{
			captureDevice.ActiveFormat = filteredFormatList.First();
		}

		captureDevice.UnlockForConfiguration();
	}

	protected virtual async partial Task PlatformConnectCamera(CancellationToken token)
	{
		if (cameraProvider.AvailableCameras is null)
		{
			await cameraProvider.RefreshAvailableCameras(token);

			if (cameraProvider.AvailableCameras is null)
			{
				throw new CameraException("Unable to refresh cameras");
			}
		}

		await PlatformStartCameraPreview(token);
	}

	protected virtual async partial Task PlatformStartCameraPreview(CancellationToken token)
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

		if (cameraView.SelectedCamera is null)
		{
			await cameraProvider.RefreshAvailableCameras(token);
			cameraView.SelectedCamera = cameraProvider.AvailableCameras?.FirstOrDefault() ?? throw new CameraException("No camera available on device");
		}

		captureDevice = cameraView.SelectedCamera.CaptureDevice ?? throw new CameraException($"No Camera found");
		captureInput = new AVCaptureDeviceInput(captureDevice, out _);
		captureSession.AddInput(captureInput);

		if (photoOutput is null)
		{
			photoOutput = new AVCapturePhotoOutput();
			captureSession.AddOutput(photoOutput);
		}

		await UpdateCaptureResolution(cameraView.ImageCaptureResolution, token);

		captureSession.CommitConfiguration();
		captureSession.StartRunning();
		IsInitialized = true;
		OnLoaded.Invoke();
	}

	protected virtual partial void PlatformStopCameraPreview()
	{
		if (captureSession is null)
		{
			return;
		}

		if (captureSession.Running)
		{
			captureSession.StopRunning();
		}

		IsInitialized = false;
	}

	protected virtual partial void PlatformDisconnect()
	{
	}

	protected virtual async partial Task PlatformStartVideoRecording(Stream stream, CancellationToken token)
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

		var videoDevice = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video) ?? throw new CameraException("Unable to get video device");

		videoInput = new AVCaptureDeviceInput(videoDevice, out NSError? error);
		if (error is not null)
		{
			throw new CameraException($"Error creating video input: {error.LocalizedDescription}");
		}

		if (!captureSession.CanAddInput(videoInput))
		{
			videoInput?.Dispose();
			throw new CameraException("Unable to add video input to capture session.");
		}

		captureSession.BeginConfiguration();
		captureSession.AddInput(videoInput);

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
			captureSession.RemoveInput(videoInput);
			if (audioInput is not null)
			{
				captureSession.RemoveInput(audioInput);
				audioInput.Dispose();
				audioInput = null;
			}

			videoInput?.Dispose();
			videoOutput?.Dispose();
			captureSession.CommitConfiguration();
			throw new CameraException("Unable to add video output to capture session.");
		}

		captureSession.AddOutput(videoOutput);
		captureSession.CommitConfiguration();

		videoRecordingStream = stream;
		videoRecordingFinalizeTcs = new TaskCompletionSource();
		videoRecordingFileName = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.mov");

		var outputUrl = NSUrl.FromFilename(videoRecordingFileName);
		videoOutput.StartRecordingToOutputFile(outputUrl, new AVCaptureMovieFileOutputRecordingDelegate(videoRecordingFinalizeTcs));
	}

	protected virtual async partial Task<Stream> PlatformStopVideoRecording(CancellationToken token)
	{
		if (captureSession is null 
		    || videoRecordingFileName is null 
		    || videoInput is null 
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

			foreach (var input in captureSession.Inputs)
			{
				captureSession.RemoveInput(input);
				input.Dispose();
			}

			foreach (var output in captureSession.Outputs)
			{
				captureSession.RemoveOutput(output);
				output.Dispose();
			}

			// Restore to photo preset for preview after video recording
			captureSession.SessionPreset = AVCaptureSession.PresetPhoto;
			captureSession.CommitConfiguration();
		}

		videoOutput = null;
		videoInput = null;
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

	protected virtual async partial ValueTask PlatformTakePicture(CancellationToken token)
	{
		ArgumentNullException.ThrowIfNull(photoOutput);

		var capturePhotoSettings = AVCapturePhotoSettings.FromFormat(codecSettings);
		capturePhotoSettings.FlashMode = photoOutput.SupportedFlashModes.Contains(flashMode) ? flashMode : photoOutput.SupportedFlashModes.First();

		if (AVMediaTypes.Video.GetConstant() is NSString avMediaTypeVideo)
		{
			var photoOutputConnection = photoOutput.ConnectionFromMediaType(avMediaTypeVideo);
			if (photoOutputConnection is not null)
			{
				photoOutputConnection.VideoOrientation = videoOrientation;
			}
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

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
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
		}
	}

	static AVCaptureVideoOrientation GetVideoOrientation()
	{
		IEnumerable<UIScene> scenes = UIApplication.SharedApplication.ConnectedScenes;
		var interfaceOrientation = scenes.FirstOrDefault() is UIWindowScene windowScene
			? windowScene.InterfaceOrientation
			: UIApplication.SharedApplication.StatusBarOrientation;

		return interfaceOrientation switch
		{
			UIInterfaceOrientation.Portrait => AVCaptureVideoOrientation.Portrait,
			UIInterfaceOrientation.PortraitUpsideDown => AVCaptureVideoOrientation.PortraitUpsideDown,
			UIInterfaceOrientation.LandscapeRight => AVCaptureVideoOrientation.LandscapeRight,
			UIInterfaceOrientation.LandscapeLeft => AVCaptureVideoOrientation.LandscapeLeft,
			_ => AVCaptureVideoOrientation.Portrait
		};
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
	public override void FinishedRecording(AVCaptureFileOutput captureOutput, NSUrl outputFileUrl, NSObject[] connections, NSError? error)
	{
		taskCompletionSource.SetResult();
	}
}