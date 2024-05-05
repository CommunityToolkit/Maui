using System.Diagnostics;
using System.Runtime.InteropServices;
using AVFoundation;
using Foundation;
using UIKit;
using CoreMedia;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Extensions;
using static UIKit.UIGestureRecognizer;

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

	// IN the future change the return type to be an alias
	public UIView CreatePlatformView()
	{
		captureSession = new AVCaptureSession
		{
			SessionPreset = AVCaptureSession.PresetPhoto
		};

		var previewView = new PreviewView();
		previewView.Session = captureSession;

		return previewView;
	}

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

		captureDevice.LockForConfiguration(out NSError error);
		if (error is not null)
		{
			Console.WriteLine(error);
			Debug.WriteLine(error);
			return;
		}

		captureDevice.VideoZoomFactor = zoomLevel;
		captureDevice.UnlockForConfiguration();
	}

	public partial ValueTask UpdateCaptureResolution(Size resolution, CancellationToken token)
	{
		if (captureDevice is null || currentCamera is null)
		{
			return ValueTask.CompletedTask;
		}

		captureDevice.LockForConfiguration(out NSError error);
		if (error is not null)
		{
			Trace.WriteLine(error);
			return ValueTask.CompletedTask;
		}

		var filteredFormatList = currentCamera.SupportedFormats.Where(f =>
		{
			var d = ((CMVideoFormatDescription)f.FormatDescription).Dimensions;
			return d.Width <= resolution.Width && d.Height <= resolution.Height;
		});

		filteredFormatList = (filteredFormatList.Any() ? filteredFormatList : currentCamera.SupportedFormats)
			.OrderByDescending(f =>
			{
				var d = ((CMVideoFormatDescription)f.FormatDescription).Dimensions;
				return d.Width * d.Height;
			});

		if (filteredFormatList.Any())
		{
			captureDevice.ActiveFormat = filteredFormatList.First();
		}

		captureDevice.UnlockForConfiguration();

		return ValueTask.CompletedTask;
	}

	protected virtual partial ValueTask PlatformConnectCamera(CancellationToken token)
	{
		if (cameraProvider.AvailableCameras.Count < 1)
		{
			throw new CameraViewException("No camera available on device");
		}

		return PlatformStartCameraPreview(token);
	}

	protected virtual async partial ValueTask PlatformStartCameraPreview(CancellationToken token)
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

		captureDevice = currentCamera.CaptureDevice ?? throw new CameraViewException($"No Camera found");
		captureInput = new AVCaptureDeviceInput(captureDevice, out var err);
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

		var wrapper = new AVCapturePhotoCaptureDelegateWrapper();

		photoOutput.CapturePhoto(capturePhotoSettings, wrapper);

		var result = await wrapper.Task.WaitAsync(token);
		var data = result.Photo.FileDataRepresentation;

		if (data is null)
		{
			// TODO: Pass NSError information
			cameraView.OnMediaCapturedFailed();
			return;
		}

		var dataBytes = new byte[data.Length];
		Marshal.Copy(data.Bytes, dataBytes, 0, (int)data.Length);

		cameraView.OnMediaCaptured(new MemoryStream(dataBytes));
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
			
			photoOutput?.Dispose();
			photoOutput = null;
		}
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

			if (PreviewLayer.Connection is null)
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