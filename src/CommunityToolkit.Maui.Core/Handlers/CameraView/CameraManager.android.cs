using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Content;
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.View;
using AndroidX.Core.Content;
using AndroidX.Lifecycle;
using Java.Lang;
using Java.Util.Concurrent;

namespace CommunityToolkit.Maui.Core.Handlers.CameraView;

class Bla
{
	public Bla()
	{
		
	}
}

public partial class CameraManager
{
	PreviewView? previewView;
	IExecutorService? cameraExecutor;
	Context Context => mauiContext.Context ?? throw new NullReferenceException();
	ProcessCameraProvider? cameraProvider;
	ImageCapture? imageCapture;
	ImageCallBack imageCallback = new();
	// IN the future change the return type to be an alias
	public PreviewView CreatePlatformView()
	{
		
		previewView = new PreviewView(Context);
		cameraExecutor = Executors.NewSingleThreadExecutor() ?? throw new NullReferenceException();
		return previewView;
	}

	protected virtual partial void PlatformSetup()
	{

	}

	protected virtual partial void PlatformConnect()
	{
		var cameraProviderFuture = ProcessCameraProvider.GetInstance(Context);
		if (previewView is null)
		{
			return;
		}
		cameraProviderFuture.AddListener(new Runnable(() =>
		{
			cameraProvider = (ProcessCameraProvider)(cameraProviderFuture.Get() ?? throw new NullReferenceException());

			cameraProvider.UnbindAll();

			var cameraPreview = new Preview.Builder()
			.SetCameraSelector(CameraSelector.DefaultBackCamera)
			.Build();
			cameraPreview.SetSurfaceProvider(previewView.SurfaceProvider);


			imageCapture = new ImageCapture.Builder()
			.SetCaptureMode(ImageCapture.CaptureModeMaximizeQuality)
			.Build();


			var owner = (ILifecycleOwner)Context;
			var camera = cameraProvider.BindToLifecycle(owner, CameraSelector.DefaultBackCamera, cameraPreview, imageCapture);

			//start the camera with AutoFocus
			MeteringPoint point = previewView.MeteringPointFactory.CreatePoint(previewView.Width / 2, previewView.Height / 2, 0.1F);
			FocusMeteringAction action = new FocusMeteringAction.Builder(point)
																.DisableAutoCancel()
																.Build();
			camera.CameraControl.StartFocusAndMetering(action);

		}), ContextCompat.GetMainExecutor(Context));
	}

	protected virtual partial void PlatformDisconnect()
	{

	}

	protected virtual partial void PlatformTakePicture()
	{
		imageCapture?.TakePicture(cameraExecutor, imageCallback);
	}

	class ImageCallBack : ImageCapture.OnImageCapturedCallback
	{
		public override void OnCaptureSuccess(IImageProxy image)
		{
			base.OnCaptureSuccess(image);

			var buffer = image.GetPlanes()[0].Buffer;

			if (buffer is null)
			{
				return;
			}

			var imgData = new byte[buffer.Capacity()];
			buffer.Get(imgData);
		}

		public override void OnError(ImageCaptureException exception)
		{
			base.OnError(exception);
		}
	}
}
