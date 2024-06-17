using System.Buffers;
using System.Runtime.Versioning;
using Android.Content;
using AndroidX.Camera.Core;
using AndroidX.Camera.Core.Impl.Utils.Futures;
using AndroidX.Camera.Core.ResolutionSelector;
using AndroidX.Camera.Lifecycle;
using AndroidX.Core.Content;
using AndroidX.Lifecycle;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Extensions;
using Java.Lang;
using Java.Util.Concurrent;
using static Android.Media.Image;
using Math = System.Math;

namespace CommunityToolkit.Maui.Core;

[SupportedOSPlatform("android21.0")]
partial class CameraManager
{
	readonly Context context = mauiContext.Context ?? throw new CameraException($"Unable to retrieve {nameof(Context)}");

	NativePlatformCameraPreviewView? previewView;
	IExecutorService? cameraExecutor;
	ProcessCameraProvider? processCameraProvider;
	ImageCapture? imageCapture;
	ImageCallBack? imageCallback;
	ICamera? camera;
	ICameraControl? cameraControl;
	Preview? cameraPreview;
	ResolutionSelector? resolutionSelector;
	ResolutionFilter? resolutionFilter;

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	// IN the future change the return type to be an alias
	public NativePlatformCameraPreviewView CreatePlatformView()
	{
		imageCallback = new ImageCallBack(cameraView);
		previewView = new NativePlatformCameraPreviewView(context);
		if (NativePlatformCameraPreviewView.ScaleType.FitCenter is not null)
		{
			previewView.SetScaleType(NativePlatformCameraPreviewView.ScaleType.FitCenter);
		}
		cameraExecutor = Executors.NewSingleThreadExecutor() ?? throw new CameraException($"Unable to retrieve {nameof(IExecutorService)}");

		return previewView;
	}

	public partial void UpdateFlashMode(CameraFlashMode flashMode)
	{
		if (imageCapture is null)
		{
			return;
		}

		imageCapture.FlashMode = flashMode.ToPlatform();
	}

	public partial void UpdateZoom(float zoomLevel)
	{
		cameraControl?.SetZoomRatio(zoomLevel);
	}

	public async partial ValueTask UpdateCaptureResolution(Size resolution, CancellationToken token)
	{
		if (resolutionFilter is not null)
		{
			if (Math.Abs(resolutionFilter.TargetSize.Width - resolution.Width) < double.Epsilon &&
				Math.Abs(resolutionFilter.TargetSize.Height - resolution.Height) < double.Epsilon)
			{
				return;
			}
		}

		var targetSize = new Android.Util.Size((int)resolution.Width, (int)resolution.Height);

		if (resolutionFilter is null)
		{
			resolutionFilter = new ResolutionFilter(targetSize);
		}
		else
		{
			resolutionFilter.TargetSize = targetSize;
		}

		resolutionSelector?.Dispose();

		resolutionSelector = new ResolutionSelector.Builder()
		.SetAllowedResolutionMode(ResolutionSelector.PreferHigherResolutionOverCaptureRate)
		.SetResolutionFilter(resolutionFilter)
		.Build();

		if (IsInitialized)
		{
			await StartUseCase(token);
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			camera?.Dispose();
			camera = null;

			cameraControl?.Dispose();
			cameraControl = null;

			cameraPreview?.Dispose();
			cameraPreview = null;

			cameraExecutor?.Dispose();
			cameraExecutor = null;

			imageCapture?.Dispose();
			imageCapture = null;

			imageCallback?.Dispose();
			imageCallback = null;

			previewView?.Dispose();
			previewView = null;

			processCameraProvider?.Dispose();
			processCameraProvider = null;

			resolutionSelector?.Dispose();
			resolutionSelector = null;

			resolutionFilter?.Dispose();
			resolutionFilter = null;
		}
	}

	protected virtual async partial Task PlatformConnectCamera(CancellationToken token)
	{
		var cameraProviderFuture = ProcessCameraProvider.GetInstance(context);
		if (previewView is null)
		{
			return;
		}

		var cameraProviderTCS = new TaskCompletionSource();

		cameraProviderFuture.AddListener(new Runnable(async () =>
		{
			processCameraProvider = (ProcessCameraProvider)(cameraProviderFuture.Get() ?? throw new CameraException($"Unable to retrieve {nameof(ProcessCameraProvider)}"));

			if (cameraProvider.AvailableCameras is null)
			{
				await cameraProvider.RefreshAvailableCameras(token);

				if (cameraProvider.AvailableCameras is null)
				{
					throw new CameraException("Unable to refresh available cameras");
				}
			}

			await StartUseCase(token);

			cameraProviderTCS.SetResult();

		}), ContextCompat.GetMainExecutor(context));

		await cameraProviderTCS.Task.WaitAsync(token);
	}

	protected async Task StartUseCase(CancellationToken token)
	{
		if (resolutionSelector is null)
		{
			return;
		}

		PlatformStopCameraPreview();

		cameraPreview?.Dispose();
		imageCapture?.Dispose();

		cameraPreview = new Preview.Builder().SetResolutionSelector(resolutionSelector).Build();
		cameraPreview.SetSurfaceProvider(previewView?.SurfaceProvider);

		imageCapture = new ImageCapture.Builder()
		.SetCaptureMode(ImageCapture.CaptureModeMaximizeQuality)
		.SetResolutionSelector(resolutionSelector)
		.Build();

		await StartCameraPreview(token);
	}

	protected virtual async partial Task PlatformStartCameraPreview(CancellationToken token)
	{
		if (previewView is null || processCameraProvider is null || cameraPreview is null || imageCapture is null)
		{
			return;
		}

		if (cameraView.SelectedCamera is null)
		{
			if (cameraProvider.AvailableCameras is null)
			{
				await cameraProvider.RefreshAvailableCameras(token);
			}

			cameraView.SelectedCamera = cameraProvider.AvailableCameras?.FirstOrDefault() ?? throw new CameraException("No camera available on device");
		}

		var cameraSelector = cameraView.SelectedCamera.CameraSelector ?? throw new CameraException($"Unable to retrieve {nameof(CameraSelector)}");

		var owner = (ILifecycleOwner)context;
		camera = processCameraProvider.BindToLifecycle(owner, cameraSelector, cameraPreview, imageCapture);

		cameraControl = camera.CameraControl;

		//start the camera with AutoFocus
		MeteringPoint point = previewView.MeteringPointFactory.CreatePoint(previewView.Width / 2.0f, previewView.Height / 2.0f, 0.1f);
		FocusMeteringAction action = new FocusMeteringAction.Builder(point)
															.DisableAutoCancel()
															.Build();
		camera.CameraControl.StartFocusAndMetering(action);

		IsInitialized = true;
		OnLoaded.Invoke();
	}

	protected virtual partial void PlatformStopCameraPreview()
	{
		if (processCameraProvider is null)
		{
			return;
		}

		processCameraProvider.UnbindAll();
		IsInitialized = false;
	}

	protected virtual partial void PlatformDisconnect()
	{

	}

	protected virtual partial ValueTask PlatformTakePicture(CancellationToken token)
	{
		ArgumentNullException.ThrowIfNull(cameraExecutor);
		ArgumentNullException.ThrowIfNull(imageCallback);

		imageCapture?.TakePicture(cameraExecutor, imageCallback);
		return ValueTask.CompletedTask;
	}

	sealed class FutureCallback(Action<Java.Lang.Object?> action, Action<Throwable?> failure) : Java.Lang.Object, IFutureCallback
	{
		public void OnSuccess(Java.Lang.Object? value)
		{
			action.Invoke(value);
		}

		public void OnFailure(Throwable? throwable)
		{
			failure.Invoke(throwable);
		}
	}

	sealed class ImageCallBack(ICameraView cameraView) : ImageCapture.OnImageCapturedCallback
	{
		public override void OnCaptureSuccess(IImageProxy image)
		{
			base.OnCaptureSuccess(image);
			var img = image.Image;

			if (img is null)
			{
				return;
			}

			var buffer = GetFirstPlane(img.GetPlanes())?.Buffer;

			if (buffer is null)
			{
				image.Close();
				return;
			}

			var imgData = ArrayPool<byte>.Shared.Rent(buffer.Capacity());
			try
			{
				buffer.Get(imgData);
				var memStream = new MemoryStream(imgData);
				cameraView.OnMediaCaptured(memStream);
			}
			finally
			{
				image.Close();
				ArrayPool<byte>.Shared.Return(imgData);
			}

			static Plane? GetFirstPlane(Plane[]? planes)
			{
				if (planes is null || planes.Length is 0)
				{
					return null;
				}

				return planes[0];
			}
		}

		public override void OnError(ImageCaptureException exception)
		{
			base.OnError(exception);
			cameraView.OnMediaCapturedFailed();
		}
	}

	sealed class ResolutionFilter : Java.Lang.Object, IResolutionFilter
	{
		public Android.Util.Size TargetSize { get; set; }

		public ResolutionFilter(Android.Util.Size size)
		{
			TargetSize = size;
		}

		public IList<Android.Util.Size> Filter(IList<Android.Util.Size> supportedSizes, int rotationDegrees)
		{
			var filteredList = supportedSizes.Where(size => size.Width <= TargetSize.Width && size.Height <= TargetSize.Height)
				.OrderByDescending(size => size.Width * size.Height).ToList();

			if (!filteredList.Any())
			{
				return supportedSizes;
			}

			return filteredList;
		}
	}

	sealed class Observer : Java.Lang.Object, IObserver
	{
		Action<Java.Lang.Object?> observerAction = (Java.Lang.Object? o) => { };

		public Observer(Action<Java.Lang.Object?> action)
		{
			observerAction = action;
		}

		public void OnChanged(Java.Lang.Object? value)
		{
			observerAction.Invoke(value);
		}
	}
}