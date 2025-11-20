using System.Runtime.Versioning;
using Android.Content;
using Android.Runtime;
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
	readonly Context context = mauiContext.Context ?? throw new CameraException($"Unable to retrieve {nameof(Context)}");

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

	public async Task SetExtensionMode(int mode, CancellationToken token)
	{
		extensionMode = mode;
		if (cameraView.SelectedCamera is null
			|| processCameraProvider is null
			|| cameraPreview is null
			|| imageCapture is null
			|| videoCapture is null)
		{
			return;
		}

		camera = await RebindCamera(processCameraProvider, cameraView.SelectedCamera, token, cameraPreview, imageCapture, videoCapture);

		cameraControl = camera.CameraControl;
	}

	public void Dispose()
	{
		CleanupVideoRecordingResources();

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

		processCameraProvider?.UnbindAll();
		processCameraProvider?.Dispose();
		processCameraProvider = null;

		resolutionSelector?.Dispose();
		resolutionSelector = null;

		resolutionFilter?.Dispose();
		resolutionFilter = null;

		orientationListener?.Disable();
		orientationListener?.Dispose();
		orientationListener = null;

		videoRecordingStream?.Dispose();
		videoRecordingStream = null;
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
			.SetAllowedResolutionMode(ResolutionSelector.PreferHigherResolutionOverCaptureRate)?
			.SetResolutionFilter(resolutionFilter)
			?.Build() ?? throw new InvalidOperationException("Unable to Set Resolution Filter");

		// `.SetResolutionFilter()` should never return null
		// According to the Android docs, `ResolutionSelector.Builder.setResolutionFilter(ResolutionFilter)` returns a `NonNull` object
		// `ResolutionSelector.Builder.SetResolutionFilter(ResolutionFilter)` returning a nullable object in .NET for Android is likely a C# Binding mistake

		if (IsInitialized)
		{
			await StartUseCase(token);
		}
	}

	private async partial Task PlatformConnectCamera(CancellationToken token)
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

			await StartUseCase(token);

			cameraProviderTCS.SetResult();
		}), ContextCompat.GetMainExecutor(context));

		await cameraProviderTCS.Task.WaitAsync(token);
	}

	async Task StartUseCase(CancellationToken token)
	{
		if (resolutionSelector is null || cameraExecutor is null)
		{
			return;
		}

		PlatformStopCameraPreview();

		cameraPreview?.Dispose();
		imageCapture?.Dispose();

		videoCapture?.Dispose();
		videoRecorder?.Dispose();

		cameraPreview = new Preview.Builder().SetResolutionSelector(resolutionSelector)?.Build();
		cameraPreview?.SetSurfaceProvider(cameraExecutor, previewView?.SurfaceProvider);

		imageCapture = new ImageCapture.Builder()
			.SetCaptureMode(ImageCapture.CaptureModeMaximizeQuality)?
			.SetResolutionSelector(resolutionSelector)
			?.Build() ?? throw new InvalidOperationException("Unable to set resolution selector");

		// `.SetResolutionFilter()` should never return null
		// According to the Android docs, `ResolutionSelector.Builder.SetResolutionFilter(ResolutionFilter)` returns a `NonNull` object
		// `ResolutionSelector.Builder.SetResolutionFilter(ResolutionFilter)` returning a nullable object in .NET for Android is likely a C# Binding mistake
		// https://developer.android.com/reference/androidx/camera/core/resolutionselector/ResolutionSelector.Builder#setResolutionFilter(androidx.camera.core.resolutionselector.ResolutionFilter)

		var videoRecorderBuilder = new Recorder.Builder()
			.SetExecutor(cameraExecutor) ?? throw new InvalidOperationException("Unable to set video recorder executor");

		// `.SetExecutor()` should never return null
		// According to the Android docs, `ResolutionSelector.Builder.setExecutor(ResolutionFilter)` returns a `NonNull` object
		// `ResolutionSelector.Builder.SetExecutor(ResolutionFilter)` returning a nullable object in .NET for Android is likely a C# Binding mistake
		// https://developer.android.com/reference/androidx/camera/video/Recorder.Builder#setExecutor(java.util.concurrent.Executor)

		if (Quality.Highest is not null)
		{
			videoRecorderBuilder = videoRecorderBuilder.SetQualitySelector(QualitySelector.From(Quality.Highest));
		}

		videoRecorder = videoRecorderBuilder.Build();
		videoCapture = VideoCapture.WithOutput(videoRecorder);

		await StartCameraPreview(token);
	}

	private async partial Task PlatformStartCameraPreview(CancellationToken token)
	{
		if (previewView is null || processCameraProvider is null || cameraPreview is null || imageCapture is null || videoCapture is null)
		{
			return;
		}

		cameraView.SelectedCamera ??= cameraProvider.AvailableCameras?.FirstOrDefault() ?? throw new CameraException("No camera available on device");

		camera = await RebindCamera(processCameraProvider, cameraView.SelectedCamera, token, cameraPreview, imageCapture, videoCapture);
		cameraControl = camera.CameraControl;

		var point = previewView.MeteringPointFactory.CreatePoint(previewView.Width / 2.0f, previewView.Height / 2.0f, 0.1f);
		var action = new FocusMeteringAction.Builder(point).Build();
		camera.CameraControl?.StartFocusAndMetering(action);

		IsInitialized = true;
		OnLoaded.Invoke();
	}

	private partial void PlatformStopCameraPreview()
	{
		if (processCameraProvider is null)
		{
			return;
		}

		processCameraProvider.UnbindAll();
		IsInitialized = false;
	}

	private partial void PlatformDisconnect()
	{
	}

	private partial ValueTask PlatformTakePicture(CancellationToken token)
	{
		ArgumentNullException.ThrowIfNull(cameraExecutor);
		ArgumentNullException.ThrowIfNull(imageCallback);

		imageCapture?.TakePicture(cameraExecutor, imageCallback);
		return ValueTask.CompletedTask;
	}

	private async partial Task PlatformStartVideoRecording(Stream stream, CancellationToken token)
	{
		if (previewView is null
			|| processCameraProvider is null
			|| cameraPreview is null
			|| imageCapture is null
			|| videoCapture is null
			|| videoRecorder is null
			|| videoRecordingFile is not null)
		{
			return;
		}

		videoRecordingStream = stream;

		cameraView.SelectedCamera ??= cameraProvider.AvailableCameras?.FirstOrDefault() ?? throw new CameraException("No camera available on device");

		if (camera is null || !IsVideoCaptureAlreadyBound())
		{
			camera = await RebindCamera(processCameraProvider, cameraView.SelectedCamera, token, cameraPreview, imageCapture, videoCapture);
			cameraControl = camera.CameraControl;
		}

		videoRecordingFile = new Java.IO.File(context.CacheDir, $"{DateTime.UtcNow.Ticks}.mp4");
		videoRecordingFile.CreateNewFile();

		var outputOptions = new FileOutputOptions.Builder(videoRecordingFile).Build();

		videoRecordingFinalizeTcs = new TaskCompletionSource();
		var captureListener = new CameraConsumer(videoRecordingFinalizeTcs);
		var executor = ContextCompat.GetMainExecutor(context) ?? throw new CameraException($"Unable to retrieve {nameof(IExecutorService)}");
		videoRecording = videoRecorder
			.PrepareRecording(context, outputOptions)
			?.WithAudioEnabled()
			.Start(executor, captureListener) ?? throw new InvalidOperationException("Unable to prepare recording");

		// `.PrepareRecording()` should never return null
		// According to the Android docs, `Recorder.prepareRecording(Context, eMediaSoreOutputOptions)` returns a `NonNull` object
		// `Recorder.PrepareRecording(Context, eMediaSoreOutputOptions)` returning a nullable object in .NET for Android is likely a C# Binding mistake
		// https://developer.android.com/reference/androidx/camera/video/Recorder#prepareRecording(android.content.Context,androidx.camera.video.MediaStoreOutputOptions)
	}

	private async partial Task<Stream> PlatformStopVideoRecording(CancellationToken token)
	{
		ArgumentNullException.ThrowIfNull(cameraExecutor);
		if (videoRecording is null
			|| videoRecordingFile is null
			|| videoRecordingFinalizeTcs is null
			|| videoRecordingStream is null)
		{
			return Stream.Null;
		}

		videoRecording.Stop();
		await videoRecordingFinalizeTcs.Task.WaitAsync(token);

		await using var inputStream = new FileStream(videoRecordingFile.AbsolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
		await inputStream.CopyToAsync(videoRecordingStream, token);
		await videoRecordingStream.FlushAsync(token);
		CleanupVideoRecordingResources();

		return videoRecordingStream;
	}

	bool IsVideoCaptureAlreadyBound()
	{
		return processCameraProvider is not null
			   && videoCapture is not null
			   && processCameraProvider.IsBound(videoCapture);
	}

	void CleanupVideoRecordingResources()
	{
		videoRecording?.Dispose();
		videoRecording = null;

		if (videoRecordingFile is not null)
		{
			if (videoRecordingFile.Exists())
			{
				videoRecordingFile.Delete();
			}

			videoRecordingFile.Dispose();
			videoRecordingFile = null;
		}

		videoRecorder?.Dispose();
		videoRecorder = null;

		videoCapture?.Dispose();
		videoCapture = null;

		videoRecordingFinalizeTcs = null;
	}

	async Task<CameraSelector> EnableModes(CameraInfo selectedCamera, CancellationToken token)
	{
		var cameraFutureCts = new TaskCompletionSource();
		var cameraSelector = selectedCamera.CameraSelector ?? throw new CameraException($"Unable to retrieve {nameof(CameraSelector)}");
		var cameraProviderFuture = ProcessCameraProvider.GetInstance(context) ?? throw new CameraException($"Unable to retrieve {nameof(ProcessCameraProvider)}");
		cameraProviderFuture.AddListener(new Runnable(() =>
		{
			var cameraProviderInstance = cameraProviderFuture.Get().JavaCast<AndroidX.Camera.Core.ICameraProvider>();
			if (cameraProviderInstance is null)
			{
				return;
			}

			var extensionsManagerFuture = ExtensionsManager.GetInstanceAsync(context, cameraProviderInstance)
										  ?? throw new InvalidOperationException("Unable to get listenable future for camera provider"); ;

			extensionsManagerFuture.AddListener(new Runnable(() =>
			{
				var extensionsManager = (ExtensionsManager?)extensionsManagerFuture.Get();
				if (extensionsManager is not null && extensionsManager.IsExtensionAvailable(cameraSelector, extensionMode))
				{
					cameraSelector = extensionsManager.GetExtensionEnabledCameraSelector(cameraSelector, extensionMode);
				}

				cameraFutureCts.SetResult();
			}), ContextCompat.GetMainExecutor(context));
		}), ContextCompat.GetMainExecutor(context));

		await cameraFutureCts.Task.WaitAsync(token);
		return cameraSelector;
	}

	async Task<ICamera> RebindCamera(ProcessCameraProvider provider, CameraInfo cameraInfo, CancellationToken token, params UseCase[] useCases)
	{
		var cameraSelector = await EnableModes(cameraInfo, token);
		provider.UnbindAll();
		return provider.BindToLifecycle((ILifecycleOwner)context, cameraSelector, useCases);
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
			var img = image?.Image;

			if (img is null)
			{
				cameraView.OnMediaCapturedFailed("Unable to obtain Image data.");
				return;
			}

			var buffer = GetFirstPlane(img.GetPlanes())?.Buffer;

			if (buffer is null)
			{
				cameraView.OnMediaCapturedFailed("Unable to obtain a buffer for the image plane.");
				image?.Close();
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
				image?.Close();
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
			cameraView.OnMediaCapturedFailed(exception?.Message ?? "An unknown error occurred.");
		}
	}

	sealed class ResolutionFilter(Android.Util.Size size) : Object, IResolutionFilter
	{
		public Android.Util.Size TargetSize { get; set; } = size;

		public IList<Android.Util.Size> Filter(IList<Android.Util.Size>? supportedSizes, int rotationDegrees)
		{
			var filteredList = supportedSizes?
				.Where(size => size.Width <= TargetSize.Width && size.Height <= TargetSize.Height)
				.OrderByDescending(size => size.Width * size.Height).ToList();

			return filteredList is null || filteredList.Count is 0
				? supportedSizes ?? []
				: filteredList;
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
		if (videoRecordEvent is VideoRecordEvent.Finalize)
		{
			finalizeTcs?.SetResult();
		}
	}
}