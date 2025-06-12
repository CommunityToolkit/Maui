using System.IO;
using System.Runtime.Versioning;
using Android.Content;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.SE.Omapi;
using Android.Views;
using AndroidX.Camera.Core;
using AndroidX.Camera.Core.Impl.Utils.Futures;
using AndroidX.Camera.Core.ResolutionSelector;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.Video;
using AndroidX.Core.Content;
using AndroidX.Lifecycle;
using CommunityToolkit.Maui.Extensions;
using Java.IO;
using Java.Lang;
using Java.Util.Concurrent;
using static Android.Media.Image;
using static Android.Print.PrintAttributes;
using static AndroidX.Camera.Core.Internal.CameraUseCaseAdapter;
using Exception = System.Exception;
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
	OrientationListener? orientationListener;

	MediaRecorder? mediaRecorder;
	CameraDevice? cameraDevice;
	CameraCaptureSession? captureSession;
	ParcelFileDescriptor? writeFd;
	ParcelFileDescriptor? readFd;
	Task? pipeTask;
	HandlerThread? cameraThread;
	Handler? cameraHandler;

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
		FocusMeteringAction action = new FocusMeteringAction.Builder(point).Build();
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

	protected virtual async partial Task PlatformStartVideoRecording(System.IO.Stream stream, CancellationToken token)
	{
		var cameraManager = (Android.Hardware.Camera2.CameraManager)context.GetSystemService(Context.CameraService)!;
		var audioManager = (AudioManager)context.GetSystemService(Context.AudioService)!;

		cameraThread = new HandlerThread("CameraThread");
		cameraThread.Start();
		cameraHandler = new Handler(cameraThread.Looper!);

		mediaRecorder = OperatingSystem.IsAndroidVersionAtLeast(31)
			? new MediaRecorder(context)
			: new MediaRecorder();

		audioManager.Mode = Mode.Normal;

		mediaRecorder.SetAudioSource(AudioSource.Mic);
		mediaRecorder.SetVideoSource(VideoSource.Surface);
		mediaRecorder.SetOutputFormat(OutputFormat.Mpeg4);

		var pipe = ParcelFileDescriptor.CreatePipe()!;
		writeFd = pipe[1];
		readFd = pipe[0];

		mediaRecorder.SetOutputFile(writeFd.FileDescriptor);
		mediaRecorder.SetVideoEncodingBitRate(10_000_000);
		mediaRecorder.SetVideoFrameRate(30);
		mediaRecorder.SetVideoEncoder(VideoEncoder.H264);
		mediaRecorder.SetAudioEncoder(AudioEncoder.Aac);
		mediaRecorder.Prepare();

		// Start background streaming to .NET stream
		pipeTask = Task.Run(async () =>
		{
			try
			{
				using var inputStream = new ParcelFileDescriptor.AutoCloseInputStream(readFd);
				byte[] buffer = new byte[81920];
				int bytesRead;
				while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0 && !token.IsCancellationRequested)
				{
					await stream.WriteAsync(buffer.AsMemory(0, bytesRead), token);
					await stream.FlushAsync(token);
				}
			}
			catch (System.OperationCanceledException) { }
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"[VideoRecorder] CopyToAsync failed: {ex}");
			}
		}, token);

		// Open camera and start recording session
		var openTcs = new TaskCompletionSource();

		var cameraCallback = new CameraStateListener(device =>
		{
			cameraDevice = device;

			var surfaces = new List<Surface> { mediaRecorder.Surface! };
			var sessionCallback = new CaptureStateListener(session =>
			{
				captureSession = session;
				var builder = cameraDevice.CreateCaptureRequest(CameraTemplate.Record);
				builder.AddTarget(mediaRecorder.Surface!);
				session.SetRepeatingRequest(builder.Build(), null, cameraHandler);

				mediaRecorder.Start();
				openTcs.TrySetResult();
			});

			device.CreateCaptureSession(surfaces, sessionCallback, cameraHandler);
		});

		cameraManager.OpenCamera(cameraView.SelectedCamera!.DeviceId, cameraCallback, cameraHandler);

		await openTcs.Task;
	}

	protected virtual async partial Task PlatformStopVideoRecording(CancellationToken token)
	{
		ArgumentNullException.ThrowIfNull(cameraExecutor);
		try
		{
			mediaRecorder?.Stop();
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[VideoRecorder] Stop failed: {ex}");
		}

		mediaRecorder?.Release();
		mediaRecorder = null;

		captureSession?.Close();
		cameraDevice?.Close();
		captureSession = null;
		cameraDevice = null;

		writeFd?.Close();
		readFd?.Close();
		writeFd = null;
		readFd = null;

		if (pipeTask is not null)
		{
			await pipeTask;
			pipeTask = null;
		}

		if (cameraThread != null)
		{
			cameraThread.QuitSafely();
			cameraThread.Join();
			cameraThread = null;
			cameraHandler = null;
		}
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
			cameraView.OnMediaCapturedFailed(exception.Message ?? "An unknown error occurred.");
		}
	}

	sealed class ResolutionFilter(Android.Util.Size size) : Java.Lang.Object, IResolutionFilter
	{
		public Android.Util.Size TargetSize { get; set; } = size;

		public IList<Android.Util.Size> Filter(IList<Android.Util.Size> supportedSizes, int rotationDegrees)
		{
			var filteredList = supportedSizes.Where(size => size.Width <= TargetSize.Width && size.Height <= TargetSize.Height)
				.OrderByDescending(size => size.Width * size.Height).ToList();

			return filteredList.Count is 0 ? supportedSizes : filteredList;
		}
	}

	sealed class Observer(Action<Java.Lang.Object?> action) : Java.Lang.Object, IObserver
	{
		readonly Action<Java.Lang.Object?> observerAction = action;

		public void OnChanged(Java.Lang.Object? value)
		{
			observerAction.Invoke(value);
		}
	}

	sealed class OrientationListener(Action<int> callback, Context context) : OrientationEventListener(context)
	{
		readonly Action<int> callback = callback;

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

public class CameraStateListener : CameraDevice.StateCallback
{
	readonly Action<CameraDevice> onOpened;
	public CameraStateListener(Action<CameraDevice> onOpened) => this.onOpened = onOpened;

	public override void OnOpened(CameraDevice camera) => onOpened(camera);
	public override void OnDisconnected(CameraDevice camera) => camera.Close();
	public override void OnError(CameraDevice camera, [GeneratedEnum] CameraError error) => camera.Close();
}

public class CaptureStateListener : CameraCaptureSession.StateCallback
{
	readonly Action<CameraCaptureSession> onConfigured;
	public CaptureStateListener(Action<CameraCaptureSession> onConfigured) => this.onConfigured = onConfigured;

	public override void OnConfigured(CameraCaptureSession session) => onConfigured(session);
	public override void OnConfigureFailed(CameraCaptureSession session) => session.Close();
}