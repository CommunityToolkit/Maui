using System.Runtime.InteropServices;
using AVFoundation;
using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Core.Handlers;

public partial class CameraManager
{
	AVCaptureSession? captureSession;
	AVCapturePhotoOutput? photoOutput;

	internal Action? Loaded { get; set; }

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
		var device = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video) ?? throw new InvalidOperationException();

		ArgumentNullException.ThrowIfNull(captureSession);
		ArgumentNullException.ThrowIfNull(photoOutput);

		captureSession.AddInput(new AVCaptureDeviceInput(device, out var err));
		captureSession.AddOutput(photoOutput);
		captureSession.StartRunning();
	}

	protected virtual partial void PlatformDisconnect()
	{
	}

	protected virtual partial void PlatformTakePicture()
	{
		ArgumentNullException.ThrowIfNull(photoOutput);

		var wrapper = new AVCapturePhotoCaptureDelegateWrapper();
		var avCapturePhotoSettings = AVCapturePhotoSettings.Create();

		AVCapturePhotoSettings.FromFormat(new NSDictionary<NSString, NSObject>(new[] {AVVideo.CodecKey},
			new[] {(NSObject) new NSString("jpeg")}));

		photoOutput.CapturePhoto(avCapturePhotoSettings, wrapper);

		Task.Run(async () =>
		{
			var (output, photo, error) = await wrapper.Task;

			var nsData = photo.FileDataRepresentation;

			if (nsData == null)
			{
				// TODO: Pass NSError information
				cameraView.OnMediaCapturedFailed();
				return;
			}

			var dataBytes = new byte[nsData.Length];
			Marshal.Copy(nsData.Bytes, dataBytes, 0, (int) nsData.Length);

			cameraView.OnMediaCaptured(new MemoryStream(dataBytes));
		});
	}

	public partial void UpdateFlashMode(CameraFlashMode flashMode)
	{
		// TODO: Implement :)
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			captureSession?.Dispose();
			photoOutput?.Dispose();
		}
	}

	public class AVCapturePhotoCaptureDelegateWrapper : AVCapturePhotoCaptureDelegate
	{
		readonly TaskCompletionSource<(AVCapturePhotoOutput Output, AVCapturePhoto Photo, NSError? Error)>
			taskCompletionSource = new();

		public Task<(AVCapturePhotoOutput Output, AVCapturePhoto Photo, NSError? Error)> Task =>
			taskCompletionSource.Task;

		public override void DidFinishProcessingPhoto(AVCapturePhotoOutput output, AVCapturePhoto photo, NSError? error)
		{
			taskCompletionSource.TrySetResult((output, photo, error));
		}
	}
}

public class PreviewView : UIView
{
	public PreviewView()
	{
		PreviewLayer.VideoGravity = AVLayerVideoGravity.ResizeAspectFill;
	}

	public AVCaptureVideoPreviewLayer PreviewLayer => (AVCaptureVideoPreviewLayer) Layer;

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