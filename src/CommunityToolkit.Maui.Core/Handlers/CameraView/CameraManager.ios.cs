using System.Runtime.InteropServices;
using AVFoundation;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Core.Views.CameraView;

namespace CommunityToolkit.Maui.Core.Handlers;

public partial class CameraManager
{
	AVCaptureSession? captureSession;
	AVCapturePhotoOutput? photoOutput;

	// TODO: Check if we really need this
	NSDictionary<NSString, NSObject> codecSettings = new NSDictionary<NSString, NSObject>(
		new[] { AVVideo.CodecKey }, new[] { (NSObject)new NSString("jpeg") });
	AVCaptureFlashMode flashMode;

	// IN the future change the return type to be an alias
	public UIView CreatePlatformView()
	{
		captureSession = new AVCaptureSession
		{
			SessionPreset = AVCaptureSession.PresetPhoto
		};

		photoOutput = new AVCapturePhotoOutput();

		var previewView = new PreviewView();
		previewView.Session = captureSession;

		return previewView;
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual partial void PlatformConnect()
	{
		var device = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video) ?? throw new InvalidOperationException("There's no camera available on your device.");

		ArgumentNullException.ThrowIfNull(captureSession);
		ArgumentNullException.ThrowIfNull(photoOutput);

		captureSession.AddInput(new AVCaptureDeviceInput(device, out var err));
		captureSession.AddOutput(photoOutput);
		captureSession.StartRunning();

		Loaded?.Invoke();
	}

	protected virtual partial void PlatformDisconnect()
	{
	}

	protected virtual async partial void PlatformTakePicture()
	{
		ArgumentNullException.ThrowIfNull(photoOutput);

		var capturePhotoSettings = AVCapturePhotoSettings.FromFormat(codecSettings);
		capturePhotoSettings.FlashMode = photoOutput.SupportedFlashModes.Contains(flashMode) ? flashMode : photoOutput.SupportedFlashModes.First();

		var wrapper = new AVCapturePhotoCaptureDelegateWrapper();

		photoOutput.CapturePhoto(capturePhotoSettings, wrapper);

		var result = await wrapper.Task;
		var data = result.Photo.FileDataRepresentation;

		if (data is null)
		{
			// TODO: Pass NSError information
			cameraView.OnMediaCapturedFailed();
			return;
		}

		var dataBytes = new byte[data.Length];
		Marshal.Copy(data.Bytes, dataBytes, 0, (int) data.Length);

		cameraView.OnMediaCaptured(new MemoryStream(dataBytes));
	}

	public partial void UpdateFlashMode(CameraFlashMode flashMode)
	{
		this.flashMode = flashMode.ToPlatform();
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			captureSession?.Dispose();
			photoOutput?.Dispose();
		}
	}

	class AVCapturePhotoCaptureDelegateWrapper : AVCapturePhotoCaptureDelegate
	{
		readonly TaskCompletionSource<CapturePhotoResult> taskCompletionSource = new();

		public Task<CapturePhotoResult> Task =>
			taskCompletionSource.Task;

		public override void DidFinishProcessingPhoto(AVCapturePhotoOutput output, AVCapturePhoto photo, NSError? error)
		{
			taskCompletionSource.TrySetResult(new() { Output = output, Photo = photo, Error = error });
		}
	}

	record CapturePhotoResult
	{
		public required AVCapturePhotoOutput Output { get; init; }

		public required AVCapturePhoto Photo { get; init; }

		public NSError? Error { get; init; }
	}

	class PreviewView : UIView
	{
		public PreviewView()
		{
			PreviewLayer.VideoGravity = AVLayerVideoGravity.ResizeAspectFill;
		}

		public AVCaptureVideoPreviewLayer PreviewLayer => (AVCaptureVideoPreviewLayer)Layer;

		public AVCaptureSession? Session
		{
			get => PreviewLayer.Session;
			set => PreviewLayer.Session = value;
		}

		[Export("layerClass")]
		public static ObjCRuntime.Class GetLayerClass()
		{
			return new ObjCRuntime.Class(typeof(AVCaptureVideoPreviewLayer));
		}

		public override void LayoutSubviews()
		{
			base.LayoutSubviews();

			if (PreviewLayer?.Connection == null)
			{
				return;
			}

			PreviewLayer.Connection.VideoOrientation = UIDevice.CurrentDevice.Orientation switch
			{
				UIDeviceOrientation.Portrait => AVCaptureVideoOrientation.Portrait,
				UIDeviceOrientation.PortraitUpsideDown => AVCaptureVideoOrientation.PortraitUpsideDown,
				UIDeviceOrientation.LandscapeLeft => AVCaptureVideoOrientation.LandscapeRight,
				UIDeviceOrientation.LandscapeRight => AVCaptureVideoOrientation.LandscapeLeft,
				_ => PreviewLayer.Connection.VideoOrientation
			};
		}
	}
}