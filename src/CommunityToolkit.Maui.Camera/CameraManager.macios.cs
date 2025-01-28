using System.Diagnostics;
using AVFoundation;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Extensions;
using CoreMedia;
using Foundation;
using UIKit;

namespace CommunityToolkit.Maui.Core;

partial class CameraManager
{
	// TODO: Check if we really need this
	readonly NSDictionary<NSString, NSObject> codecSettings = new([AVVideo.CodecKey], [new NSString("jpeg")]);

	AVCaptureSession? captureSession;
	AVCapturePhotoOutput? photoOutput;
	AVCaptureInput? captureInput;
	AVCaptureDevice? captureDevice;

	AVCaptureFlashMode flashMode;

	IDisposable? orientationDidChangeObserver;
	PreviewView? previewView;
	AVCaptureVideoOrientation videoOrientation;

	// IN the future change the return type to be an alias
	public UIView CreatePlatformView()
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

	/// <inheritdoc/>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
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
			captureSession?.StopRunning();
			captureSession?.Dispose();
			captureSession = null;

			captureInput?.Dispose();
			captureInput = null;

			orientationDidChangeObserver?.Dispose();
			orientationDidChangeObserver = null;

			photoOutput?.Dispose();
			photoOutput = null;
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

	sealed class PreviewView : UIView
	{
		public PreviewView()
		{
			PreviewLayer.VideoGravity = AVLayerVideoGravity.ResizeAspectFill;
		}

		[Export("layerClass")]
		public static ObjCRuntime.Class GetLayerClass()
		{
			return new ObjCRuntime.Class(typeof(AVCaptureVideoPreviewLayer));
		}

		public AVCaptureSession? Session
		{
			get => PreviewLayer.Session;
			set => PreviewLayer.Session = value;
		}

		AVCaptureVideoPreviewLayer PreviewLayer => (AVCaptureVideoPreviewLayer)Layer;

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