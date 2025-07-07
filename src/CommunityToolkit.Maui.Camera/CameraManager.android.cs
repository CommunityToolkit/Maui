using System.Runtime.Versioning;
using Android.Content;
using Android.Views;
using AndroidX.Camera.Core;
using AndroidX.Camera.Core.ResolutionSelector;
using AndroidX.Camera.Extensions;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.Video;
using AndroidX.Core.Content;
using AndroidX.Core.Util;
using AndroidX.Lifecycle;
using CommunityToolkit.Maui.Extensions;
using Java.Lang;
using Java.Util.Concurrent;
using Image = Android.Media.Image;
using Math = System.Math;
using Object = Java.Lang.Object;

namespace CommunityToolkit.Maui.Core;

[SupportedOSPlatform("android21.0")]
partial class CameraManager
{
	readonly Context context =
		mauiContext.Context ?? throw new CameraException($"Unable to retrieve {nameof(Context)}");

	NativePlatformCameraPreviewView? previewView;
	IExecutorService? cameraExecutor;
	ProcessCameraProvider? processCameraProvider;
	ImageCapture? imageCapture;
	ImageCallBack? imageCallback;
	VideoCapture? videoCapture;
	Recorder? videoRecorder;
	Recording? videoRecording;
	ICamera? camera;
	ICameraControl? cameraControl;
	Preview? cameraPreview;
	ResolutionSelector? resolutionSelector;
	ResolutionFilter? resolutionFilter;
	OrientationListener? orientationListener;
	Java.IO.File? videoRecordingFile;
	TaskCompletionSource? videoRecordingFinalizeTcs;
	Stream? videoRecordingStream;
	int extensionMode = ExtensionMode.Auto;

	public async Task SetExtensionMode(int mode)
	{
		extensionMode = mode;
		if (cameraView.SelectedCamera is null || processCameraProvider is null || cameraPreview is null ||
		    imageCapture is null || videoCapture is null)
		{
			return;
		}

		var cameraSelector = await EnableModes(cameraView.SelectedCamera);
		camera = RebindCamera(processCameraProvider, cameraSelector, cameraPreview, imageCapture, videoCapture);

		cameraControl = camera.CameraControl;
	}

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

		cameraExecutor = Executors.NewSingleThreadExecutor() ??
		                 throw new CameraException($"Unable to retrieve {nameof(IExecutorService)}");
		orientationListener = new OrientationListener(SetImageCaptureTargetRotation, context);
		orientationListener.Enable();

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

			videoCapture?.Dispose();
			videoCapture = null;

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

			orientationListener?.Disable();
			orientationListener?.Dispose();
			orientationListener = null;
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
			processCameraProvider = (ProcessCameraProvider)(cameraProviderFuture.Get() ??
			                                                throw new CameraException(
				                                                $"Unable to retrieve {nameof(ProcessCameraProvider)}"));

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
		if (resolutionSelector is null || cameraExecutor is null)
		{
			return;
		}

		PlatformStopCameraPreview();

		cameraPreview?.Dispose();
		imageCapture?.Dispose();

		cameraPreview = new Preview.Builder().SetResolutionSelector(resolutionSelector).Build();
		cameraPreview.SetSurfaceProvider(cameraExecutor, previewView?.SurfaceProvider);

		imageCapture = new ImageCapture.Builder()
			.SetCaptureMode(ImageCapture.CaptureModeMaximizeQuality)
			.SetResolutionSelector(resolutionSelector)
			.Build();

		videoRecorder = new Recorder.Builder()
			.SetExecutor(cameraExecutor)
			.SetQualitySelector(QualitySelector.From(Quality.Highest!))
			.Build();
		videoCapture = VideoCapture.WithOutput(videoRecorder);

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

			cameraView.SelectedCamera = cameraProvider.AvailableCameras?.FirstOrDefault() ??
			                            throw new CameraException("No camera available on device");
		}

		var cameraSelector = await EnableModes(cameraView.SelectedCamera);
		camera = RebindCamera(processCameraProvider, cameraSelector, cameraPreview, imageCapture);
		cameraControl = camera.CameraControl;

		var point = previewView.MeteringPointFactory.CreatePoint(previewView.Width / 2.0f, previewView.Height / 2.0f,
			0.1f);
		var action = new FocusMeteringAction.Builder(point).Build();
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

	protected virtual async partial Task PlatformStartVideoRecording(Stream stream, CancellationToken token)
	{
		if (previewView is null || processCameraProvider is null || cameraPreview is null || videoCapture is null ||
		    videoRecorder is null || videoRecordingFile is not null)
		{
			return;
		}

		videoRecordingStream = stream;

		if (cameraView.SelectedCamera is null)
		{
			if (cameraProvider.AvailableCameras is null)
			{
				await cameraProvider.RefreshAvailableCameras(token);
			}

			cameraView.SelectedCamera = cameraProvider.AvailableCameras?.FirstOrDefault() ??
			                            throw new CameraException("No camera available on device");
		}

		var cameraSelector = await EnableModes(cameraView.SelectedCamera);
		camera = RebindCamera(processCameraProvider, cameraSelector, cameraPreview, videoCapture);

		cameraControl = camera.CameraControl;

		videoRecordingFile = new Java.IO.File(context.CacheDir, $"{DateTime.UtcNow.Ticks}.mp4");
		videoRecordingFile.CreateNewFile();

		var outputOptions = new FileOutputOptions.Builder(videoRecordingFile).Build();

		videoRecordingFinalizeTcs = new TaskCompletionSource();
		var captureListener = new CameraConsumer(videoRecordingFinalizeTcs);
		videoRecording = videoRecorder
			.PrepareRecording(context, outputOptions)
			.WithAudioEnabled()
			.Start(ContextCompat.GetMainExecutor(context)!, captureListener);
	}

	protected virtual async partial Task PlatformStopVideoRecording(CancellationToken token)
	{
		ArgumentNullException.ThrowIfNull(cameraExecutor);
		if (videoRecording is null || videoRecordingFile is null || videoRecordingFinalizeTcs is null || videoRecordingStream is null)
		{
			return;
		}

		videoRecording.Stop();
		await videoRecordingFinalizeTcs.Task.WaitAsync(token);

		await using var inputStream = new FileStream(videoRecordingFile.AbsolutePath, FileMode.Open);
		await inputStream.CopyToAsync(videoRecordingStream, token);
		videoRecordingFile.Delete();
		videoRecording.Dispose();
		videoRecording = null;
		videoRecordingFinalizeTcs = null;
	}

	async Task<CameraSelector> EnableModes(CameraInfo selectedCamera)
	{
		var cameraFutureCts = new TaskCompletionSource();
		var cameraSelector = selectedCamera.CameraSelector ?? throw new CameraException($"Unable to retrieve {nameof(CameraSelector)}");
		var cameraProviderFuture = ProcessCameraProvider.GetInstance(context) ?? throw new CameraException($"Unable to retrieve {nameof(ProcessCameraProvider)}");
		cameraProviderFuture.AddListener(new Runnable(() =>
		{
			var cameraProviderInstance = cameraProviderFuture.Get();
			if (cameraProviderInstance is not AndroidX.Camera.Core.ICameraProvider androidCameraProvider)
			{
				cameraFutureCts.SetResult();
				return;
			}
			
			var extensionsManagerFuture = ExtensionsManager.GetInstanceAsync(context, androidCameraProvider);
			extensionsManagerFuture.AddListener(new Runnable(() =>
			{
				var extensionsManager = (ExtensionsManager)extensionsManagerFuture.Get()!;
				if (extensionsManager.IsExtensionAvailable(cameraSelector, extensionMode))
				{
					cameraSelector = extensionsManager.GetExtensionEnabledCameraSelector(cameraSelector, extensionMode);
				}
				
				cameraFutureCts.SetResult();
			}), ContextCompat.GetMainExecutor(context));
		}), ContextCompat.GetMainExecutor(context));
		
		await cameraFutureCts.Task;
		return cameraSelector;
	}

	ICamera RebindCamera(ProcessCameraProvider provider, CameraSelector cameraSelector, params UseCase[] useCases)
	{
		provider.UnbindAll();
		return provider.BindToLifecycle(
			(ILifecycleOwner)context,
			cameraSelector,
			useCases);
	}

	void SetImageCaptureTargetRotation(int rotation)
	{
		if (imageCapture is not null)
		{
			imageCapture.TargetRotation = rotation switch
			{
				>= 45 and < 135 => (int)SurfaceOrientation.Rotation270,
				>= 135 and < 225 => (int)SurfaceOrientation.Rotation180,
				>= 225 and < 315 => (int)SurfaceOrientation.Rotation90,
				_ => (int)SurfaceOrientation.Rotation0
			};
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
				cameraView.OnMediaCapturedFailed("Unable to obtain Image data.");
				return;
			}

			var buffer = GetFirstPlane(img.GetPlanes())?.Buffer;

			if (buffer is null)
			{
				cameraView.OnMediaCapturedFailed("Unable to obtain a buffer for the image plane.");
				image.Close();
				return;
			}

			var imgData = new byte[buffer.Remaining()];
			try
			{
				buffer.Get(imgData);
				var memStream = new MemoryStream(imgData);
				cameraView.OnMediaCaptured(memStream);
			}
			catch (System.Exception ex)
			{
				cameraView.OnMediaCapturedFailed(ex.Message);
				throw;
			}
			finally
			{
				image.Close();
			}

			static Image.Plane? GetFirstPlane(Image.Plane[]? planes)
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
			cameraView.OnMediaCapturedFailed(exception.Message ?? "An unknown error occurred.");
		}
	}

	sealed class ResolutionFilter(Android.Util.Size size) : Object, IResolutionFilter
	{
		public Android.Util.Size TargetSize { get; set; } = size;

		public IList<Android.Util.Size> Filter(IList<Android.Util.Size> supportedSizes, int rotationDegrees)
		{
			var filteredList = supportedSizes
				.Where(size => size.Width <= TargetSize.Width && size.Height <= TargetSize.Height)
				.OrderByDescending(size => size.Width * size.Height).ToList();

			return filteredList.Count is 0 ? supportedSizes : filteredList;
		}
	}

	sealed class OrientationListener(Action<int> callback, Context context) : OrientationEventListener(context)
	{
		public override void OnOrientationChanged(int orientation)
		{
			if (orientation == OrientationUnknown)
			{
				return;
			}

			callback.Invoke(orientation);
		}
	}
}

public class CameraConsumer(TaskCompletionSource finalizeTcs) : Object, IConsumer
{
	readonly TaskCompletionSource? finalizeTcs = finalizeTcs;

	public void Accept(Object? videoRecordEvent)
	{
		if (videoRecordEvent is VideoRecordEvent and VideoRecordEvent.Finalize)
		{
			finalizeTcs?.SetResult();
		}
	}
}