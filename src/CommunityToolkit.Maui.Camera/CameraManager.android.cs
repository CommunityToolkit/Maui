using System.Diagnostics;
using System.Runtime.Versioning;
using Android.Content;
using Android.Runtime;
using Android.Views;
using AndroidX.Camera.Camera2.InterOp;
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

public class CameraConsumer(TaskCompletionSource finalizeTcs) : Object, IConsumer
{
	readonly TaskCompletionSource? finalizeTcs = finalizeTcs;

	internal int Error { get; private set; } = VideoRecordEvent.Finalize.ErrorNone;

	internal string? FailureReason { get; private set; }

	public void Accept(Object? videoRecordEvent)
	{
		if (videoRecordEvent is VideoRecordEvent.Finalize finalizeEvent)
		{
			// CameraX finalizes a recording that failed with an error (the storage is full, the camera closed, ...) also when the video
			// recorded until then is in the file, so the error is kept for StopVideoRecording to decide whether that video can be played
			if (finalizeEvent.HasError)
			{
				Error = finalizeEvent.Error;
				var error = $"CameraX finalized the recording with VideoRecordEvent.Finalize error code {finalizeEvent.Error}";
				FailureReason = finalizeEvent.Cause?.Message is { Length: > 0 } causeMessage
					? $"{error} - {causeMessage}"
					: error;
			}

			finalizeTcs?.SetResult();
		}
	}
}
[SupportedOSPlatform("android21.0")]
partial class CameraManager
{
	const string cameraPermissionNotGrantedMessage = "Camera permission is not granted. Please enable it in the app settings.";

	readonly Context context = mauiContext.Context ?? throw new CameraException($"Unable to retrieve {nameof(Context)}");

	// StartVideoRecording and StopVideoRecording both wait for CameraX: without this, one could work on a recording that the other one
	// is ending or replacing
	readonly SemaphoreSlim videoRecordingSemaphoreSlim = new(1, 1);

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
	CameraConsumer? videoRecordingConsumer;
	Stream? videoRecordingStream;
	int extensionMode = ExtensionMode.Auto;
	bool isDisposed;

	bool isCameraSwitchDeferred;

	CaptureSessionMode currentSessionMode = CaptureSessionMode.Photo;
	enum CaptureSessionMode { Photo, Video }

	bool IsCapturePhotoSession => currentSessionMode is CaptureSessionMode.Photo;

	public async Task SetExtensionMode(int mode, CancellationToken token)
	{
		extensionMode = mode;
		if (cameraView.SelectedCamera is null
			|| processCameraProvider is null
			|| cameraPreview is null)
		{
			return;
		}

		// Binding the use cases again would make CameraX finalize the running recording: the new extension mode applies when the
		// session is bound next (StopVideoRecording binds the photo session)
		if (IsRecordingBound())
		{
			return;
		}

		await RebindCurrentSessionAsync(token);
	}

	public void Dispose()
	{
		// Work that still waits for CameraX (connecting, binding the camera, finalizing a recording) ends with ObjectDisposedException
		// instead of using what is released here
		isDisposed = true;

		CleanupVideoRecordingResources();

		// The ProcessCameraProvider is process-wide, and CameraX hands the same camera and CameraControl to every manager bound to the same
		// LifecycleOwner, so their managed peers are shared: release them, never dispose them.
		UnbindUseCases();
		processCameraProvider = null;

		camera = null;
		cameraControl = null;

		cameraPreview?.Dispose();
		cameraPreview = null;

		// Unbinding the ImageCapture aborted the captures that were still pending, and CameraX queued their onError on the
		// camera executor: the callback is disposed after them, on the same executor
		if (imageCallback is not null)
		{
			if (cameraExecutor is null)
			{
				imageCallback.Dispose();
			}
			else
			{
				cameraExecutor.Execute(new Runnable(imageCallback.Dispose));
			}

			imageCallback = null;
		}

		// The executor runs what was queued before (the callbacks above), then its thread ends
		cameraExecutor?.Shutdown();
		cameraExecutor?.Dispose();
		cameraExecutor = null;

		imageCapture?.Dispose();
		imageCapture = null;

		videoCapture?.Dispose();
		videoCapture = null;

		videoRecorder?.Dispose();
		videoRecorder = null;

		previewView?.Dispose();
		previewView = null;

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
		orientationListener = new OrientationListener(OnOrientationChanged, context);
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

	public partial void UpdateIsTorchOn(bool isTorchOn)
	{
		if (cameraControl is null)
		{
			return;
		}

		cameraControl.EnableTorch(isTorchOn);
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

		if (isInitialized && IsCapturePhotoSession)
		{
			RebuildCameraPreview();
			RebuildImageCapture();
			await PlatformStartCameraPreview(token);
		}
	}

	static int GetSurfaceRotation(int orientationDegrees) => orientationDegrees switch
	{
		>= 45 and < 135 => (int)SurfaceOrientation.Rotation270,
		>= 135 and < 225 => (int)SurfaceOrientation.Rotation180,
		>= 225 and < 315 => (int)SurfaceOrientation.Rotation90,
		_ => (int)SurfaceOrientation.Rotation0
	};

	private async partial Task PlatformConnectCamera(CancellationToken token)
	{
		var cameraProviderFuture = ProcessCameraProvider.GetInstance(context);
		if (previewView is null)
		{
			return;
		}

		var cameraProviderTCS = new TaskCompletionSource();

		// The listener runs later on the UI thread, outside of this method: its outcome completes ConnectCamera, nothing escapes from it
		cameraProviderFuture.AddListener(new Runnable(async () =>
		{
			try
			{
				// Dispose released what the use cases need before CameraX answered: connect nothing. The released camera ends the connection like
				// a cancellation, which ConnectCamera ends quietly, instead of an error that the view would report although it is gone
				if (isDisposed)
				{
					cameraProviderTCS.TrySetCanceled();
					return;
				}

				processCameraProvider = (ProcessCameraProvider)(cameraProviderFuture.Get() ?? throw new CameraException($"Unable to retrieve {nameof(ProcessCameraProvider)}"));

				InitializeUseCases();
				await PlatformStartCameraPreview(token);

				cameraProviderTCS.SetResult();
			}
			catch (OperationCanceledException ex)
			{
				cameraProviderTCS.TrySetCanceled(ex.CancellationToken);
			}
			catch (System.Exception ex)
			{
				cameraProviderTCS.TrySetException(ex);
			}
		}), ContextCompat.GetMainExecutor(context));

		await cameraProviderTCS.Task.WaitAsync(token);
	}

	void InitializeUseCases()
	{
		RebuildCameraPreview();
		RebuildImageCapture();
		RebuildVideoCapture();
	}

	void RebuildCameraPreview()
	{
		ArgumentNullException.ThrowIfNull(cameraExecutor);
		ArgumentNullException.ThrowIfNull(resolutionSelector);

		if (cameraPreview is not null)
		{
			processCameraProvider?.Unbind(cameraPreview);
			cameraPreview.Dispose();
		}
		cameraPreview = new Preview.Builder().SetResolutionSelector(resolutionSelector).Build();
		cameraPreview.SetSurfaceProvider(cameraExecutor, previewView?.SurfaceProvider);
	}

	void RebuildImageCapture() => ReplaceImageCapture(CreateImageCapture(currentSessionMode));

	ImageCapture CreateImageCapture(CaptureSessionMode sessionMode)
	{
		ArgumentNullException.ThrowIfNull(resolutionSelector);

		var imageCaptureMode = sessionMode switch
		{
			CaptureSessionMode.Video => ImageCapture.CaptureModeMinimizeLatency,
			_ => ImageCapture.CaptureModeMaximizeQuality
		};

		// A new ImageCapture starts with CameraX's default flash mode, not the one of the view
		return new ImageCapture.Builder()
			.SetCaptureMode(imageCaptureMode)
			.SetFlashMode(cameraView.CameraFlashMode.ToPlatform())
			.SetResolutionSelector(resolutionSelector)
			.Build();
	}

	// The current ImageCapture is released once its replacement exists, so that a failure while creating the replacement (an undefined
	// CameraFlashMode) never leaves a disposed ImageCapture behind
	void ReplaceImageCapture(ImageCapture replacement)
	{
		if (imageCapture is not null)
		{
			processCameraProvider?.Unbind(imageCapture);
			imageCapture.Dispose();
		}

		imageCapture = replacement;
	}

	void RebuildVideoCapture()
	{
		if (videoCapture is not null)
		{
			processCameraProvider?.Unbind(videoCapture);
			videoCapture.Dispose();
		}
		videoRecorder?.Dispose();

		// The Recorder runs on the I/O executor of CameraX (its default), not on the camera executor: it still finalizes recordings
		// and releases its encoders after its VideoCapture is unbound, which the camera executor that Dispose shuts down would reject
		var videoRecorderBuilder = new Recorder.Builder();

		if (Quality.Highest is not null)
		{
			videoRecorderBuilder = videoRecorderBuilder.SetQualitySelector(QualitySelector.From(Quality.Highest));
		}
		videoRecorder = videoRecorderBuilder.Build();
		videoCapture = VideoCapture.WithOutput(videoRecorder);
	}

	Task BindPhotoSessionAsync(CancellationToken token) => BindSessionAsync(CaptureSessionMode.Photo, token);

	Task BindVideoSessionAsync(CancellationToken token) => BindSessionAsync(CaptureSessionMode.Video, token);

	// CameraX resolves the camera selector first, and the session and its use cases are read once it answered: a session switch or a new
	// capture resolution can have replaced (and disposed) the use cases meanwhile, and a recording can have started.
	async Task BindSessionAsync(CaptureSessionMode? sessionMode, CancellationToken token)
	{
		if (processCameraProvider is null || cameraView.SelectedCamera is null)
		{
			return;
		}

		var cameraSelector = await EnableModes(cameraView.SelectedCamera, token);

		ObjectDisposedException.ThrowIf(isDisposed, this);

		// Binding the use cases again would make CameraX finalize the recording that started meanwhile, whose session is bound already
		if (IsRecordingBound())
		{
			return;
		}

		BindSession(sessionMode ?? currentSessionMode, cameraSelector);
	}

	void BindSession(CaptureSessionMode sessionMode, CameraSelector cameraSelector)
	{
		if (processCameraProvider is null || cameraPreview is null || imageCapture is null)
		{
			return;
		}

		if (sessionMode is CaptureSessionMode.Photo)
		{
			OnCameraBound(BindCamera(processCameraProvider, cameraSelector, cameraPreview, imageCapture));
		}
		else if (videoCapture is not null)
		{
			OnCameraBound(BindCamera(processCameraProvider, cameraSelector, cameraPreview, imageCapture, videoCapture));
		}
	}

	// CameraX resolves the camera selector and the new ImageCapture is created before anything changes, so that a cancelled token (or an
	// ImageCapture that can not be created) leaves the current session as it was.
	async Task SwitchSessionAsync(CaptureSessionMode sessionMode, bool bindSession, CancellationToken token)
	{
		var cameraSelector = bindSession && cameraView.SelectedCamera is not null
			? await EnableModes(cameraView.SelectedCamera, token)
			: null;

		ObjectDisposedException.ThrowIf(isDisposed, this);

		var sessionImageCapture = CreateImageCapture(sessionMode);
		currentSessionMode = sessionMode;
		ReplaceImageCapture(sessionImageCapture);

		if (!bindSession)
		{
			UnbindUseCases();
		}
		else if (cameraSelector is not null)
		{
			BindSession(sessionMode, cameraSelector);
		}
	}

	// Called once no recording runs any more, also when the token of the call was cancelled meanwhile. A preview that the app stopped stays
	// stopped. A camera switch that waited for the recording to end completes now, the way UpdateCurrentCamera completes a switch.
	async Task ReturnToPhotoSessionAsync()
	{
		if (isDisposed)
		{
			return;
		}

		// A selection that came back to the camera of the recording (for example through null) needs no other camera: the photo session is
		// bound to that camera again, as after a recording without a switch, instead of closing the camera and opening it again
		var completeCameraSwitch = isCameraSwitchDeferred && isInitialized && !IsBoundTo(cameraView.SelectedCamera);
		isCameraSwitchDeferred = false;

		await SwitchSessionAsync(CaptureSessionMode.Photo, bindSession: isInitialized && !completeCameraSwitch, CancellationToken.None);

		if (!completeCameraSwitch)
		{
			return;
		}

		try
		{
			await PlatformStartCameraPreview(CancellationToken.None);
		}
		catch (System.Exception ex) when (!isDisposed)
		{
			// Like a switch to a camera that can not be bound (CameraViewHandler.MapSelectedCamera reports it), which keeps the preview
			// started for the next selection: the recording that ended is not affected
			Trace.TraceError($"{nameof(CameraManager)}: unable to switch to the selected camera: {ex}");
		}
	}

	bool IsBoundTo(CameraInfo? cameraInfo) => camera is not null
		&& cameraInfo is not null
		&& string.Equals(Camera2CameraInfo.From(camera.CameraInfo)?.CameraId, cameraInfo.DeviceId, StringComparison.Ordinal);

	// CameraX finalizes a recording as soon as its VideoCapture is unbound, which binding the use cases again does
	bool IsRecordingBound() => videoRecording is not null
		&& videoCapture is not null
		&& processCameraProvider?.IsBound(videoCapture) is true;

	// The ProcessCameraProvider is process-wide, so only the use cases of this manager are unbound
	void UnbindUseCases()
	{
		UseCase[] useCases = [.. new UseCase?[] { cameraPreview, imageCapture, videoCapture }.OfType<UseCase>()];
		processCameraProvider?.Unbind(useCases);
	}

	void OnCameraBound(ICamera boundCamera)
	{
		camera = boundCamera;
		cameraControl = boundCamera.CameraControl;

		// CameraX starts a camera that is bound again with its defaults (zoom ratio 1, torch off). Its camera thread resets the published
		// zoom state when it detaches the use cases, which can come after this zoom request was published (the request itself still reaches
		// the camera), so the zoom of the view is requested again once CameraX answered.
		cameraControl?.SetZoomRatio(cameraView.ZoomFactor)?.AddListener(new Runnable(() => UpdateZoom(cameraView.ZoomFactor)), ContextCompat.GetMainExecutor(context));
		UpdateIsTorchOn(cameraView.IsTorchOn);
	}

	Task RebindCurrentSessionAsync(CancellationToken token) => BindSessionAsync(null, token);

	bool IsPhotoSessionBound()
	{
		return IsSessionBound(CaptureSessionMode.Photo);
	}

	bool IsVideoSessionBound()
	{
		return IsSessionBound(CaptureSessionMode.Video);
	}

	bool IsSessionBound(CaptureSessionMode mode)
	{
		if (processCameraProvider is null || cameraPreview is null || imageCapture is null)
		{
			return false;
		}

		var arePreviewAndImageCaptureBound = processCameraProvider.IsBound(cameraPreview)
			&& processCameraProvider.IsBound(imageCapture);

		return mode switch
		{
			CaptureSessionMode.Video => arePreviewAndImageCaptureBound && videoCapture is not null && processCameraProvider.IsBound(videoCapture),
			_ => arePreviewAndImageCaptureBound
		};
	}

	private async partial Task PlatformStartCameraPreview(CancellationToken token)
	{
		if (previewView is null || processCameraProvider is null || cameraPreview is null || imageCapture is null)
		{
			return;
		}

		cameraView.SelectedCamera ??= cameraProvider.AvailableCameras?.FirstOrDefault() ?? throw new CameraException("No camera available on device");

		// The recording keeps its camera until it ends (see PlatformStopCameraPreviewForCameraSwitch)
		if (isCameraSwitchDeferred && IsRecordingBound())
		{
			return;
		}

		// CameraX binds the use cases also without the camera permission, and then keeps trying to open the camera without reporting anything.
		// Like a denied permission on iOS, Mac Catalyst and Windows, a start without the permission fails and leaves the preview stopped until
		// StartCameraPreview is called again once the app has the permission
		if (ContextCompat.CheckSelfPermission(context, Android.Manifest.Permission.Camera) is not Android.Content.PM.Permission.Granted)
		{
			throw new CameraException(cameraPermissionNotGrantedMessage);
		}

		if (camera is null || !IsPhotoSessionBound())
		{
			try
			{
				await BindPhotoSessionAsync(token);
			}
			catch (System.Exception ex) when (ex is not OperationCanceledException)
			{
				// The selected camera can not be bound (for example an unplugged USB camera): the preview stays started, so that
				// selecting another camera (UpdateCurrentCamera) opens it
				isInitialized = true;
				throw;
			}
		}

		var point = previewView.MeteringPointFactory.CreatePoint(previewView.Width / 2.0f, previewView.Height / 2.0f, 0.1f);
		var action = new FocusMeteringAction.Builder(point).Build();
		camera?.CameraControl?.StartFocusAndMetering(action);

		isInitialized = true;
		onLoaded.Invoke();
	}

	// Unbinding the use cases of a recording would make CameraX finalize it, so while a recording runs the switch is deferred until the
	// recording ends (StopVideoRecording, or a StartVideoRecording that replaces it or fails)
	private partial void PlatformStopCameraPreviewForCameraSwitch()
	{
		if (processCameraProvider is not null && isInitialized && IsRecordingBound())
		{
			isCameraSwitchDeferred = true;
			return;
		}

		PlatformStopCameraPreview();
	}

	// StopCameraPreview always stops the preview, also a running recording (CameraX finalizes it) and a camera switch that waits for it
	private partial void PlatformStopCameraPreview()
	{
		if (processCameraProvider is null)
		{
			return;
		}

		isCameraSwitchDeferred = false;
		processCameraProvider.UnbindAll();
		isInitialized = false;
	}

	private partial void PlatformDisconnect()
	{
	}

	private partial ValueTask PlatformTakePicture(CancellationToken token)
	{
		ArgumentNullException.ThrowIfNull(cameraExecutor);
		ArgumentNullException.ThrowIfNull(imageCallback);

		if (imageCapture is null)
		{
			// CameraView.CaptureImage waits for MediaCaptured or MediaCaptureFailed, so a capture that can not start (the camera never started,
			// for example without a camera) is reported: CaptureImage throws CameraException instead of waiting for its token, as on Windows
			cameraView.OnMediaCapturedFailed("Unable to capture an image because the camera is not initialized.");
			return ValueTask.CompletedTask;
		}

		imageCapture.TakePicture(cameraExecutor, imageCallback);
		return ValueTask.CompletedTask;
	}

	private async partial Task PlatformStartVideoRecording(Stream stream, CancellationToken token)
	{
		await videoRecordingSemaphoreSlim.WaitAsync(token);

		try
		{
			await StartRecordingAsync(stream, token);
		}
		finally
		{
			videoRecordingSemaphoreSlim.Release();
		}
	}

	async Task StartRecordingAsync(Stream stream, CancellationToken token)
	{
		if (previewView is null
			|| processCameraProvider is null
			|| cameraPreview is null
			|| videoCapture is null
			|| videoRecorder is null)
		{
			return;
		}

		cameraView.SelectedCamera ??= cameraProvider.AvailableCameras?.FirstOrDefault() ?? throw new CameraException("No camera available on device");

		if (videoRecording is not null && videoRecordingFinalizeTcs is not null)
		{
			// Like on iOS and Windows, the stream of the latest StartVideoRecording receives the video: the running recording ends and its
			// video is discarded
			token.ThrowIfCancellationRequested();
			videoRecording.Stop();
			await videoRecordingFinalizeTcs.Task.WaitAsync(token);

			ObjectDisposedException.ThrowIf(isDisposed, this);

			CleanupVideoRecordingResources();

			// A camera switch that waited for the ended recording completes first: the new recording records with the selected camera
			if (isCameraSwitchDeferred)
			{
				await ReturnToPhotoSessionAsync();
			}
		}

		// The recording file and the recording (which checks RECORD_AUDIO) are prepared before the session changes, and the manager
		// keeps the recording state only once the recording started: a StartVideoRecording that fails leaves nothing behind
		var recordingFile = new Java.IO.File(context.CacheDir, $"{DateTime.UtcNow.Ticks}.mp4");
		try
		{
			recordingFile.CreateNewFile();

			var outputOptions = new FileOutputOptions.Builder(recordingFile).Build();

			var pendingRecording = videoRecorder
				.PrepareRecording(context, outputOptions)
				?.WithAudioEnabled() ?? throw new InvalidOperationException("Unable to prepare recording");

			// `.PrepareRecording()` should never return null
			// According to the Android docs, `Recorder.prepareRecording(Context, eMediaSoreOutputOptions)` returns a `NonNull` object
			// `Recorder.PrepareRecording(Context, eMediaSoreOutputOptions)` returning a nullable object in .NET for Android is likely a C# Binding mistake
			// https://developer.android.com/reference/androidx/camera/video/Recorder#prepareRecording(android.content.Context,androidx.camera.video.MediaStoreOutputOptions)

			if (IsCapturePhotoSession)
			{
				// Rebuild the ImageCapture use case with the CaptureModeMinimizeLatency capture mode
				// to optimize for latency during video recording sessions
				await SwitchSessionAsync(CaptureSessionMode.Video, bindSession: true, token);
			}
			else if (camera is null || !IsVideoSessionBound())
			{
				await BindVideoSessionAsync(token);
			}

			var finalizeTcs = new TaskCompletionSource();
			var executor = ContextCompat.GetMainExecutor(context) ?? throw new CameraException($"Unable to retrieve {nameof(IExecutorService)}");
			var consumer = new CameraConsumer(finalizeTcs);

			videoRecording = pendingRecording.Start(executor, consumer);
			videoRecordingConsumer = consumer;
			videoRecordingFile = recordingFile;
			videoRecordingFinalizeTcs = finalizeTcs;
			videoRecordingStream = stream;
		}
		catch
		{
			recordingFile.Delete();
			recordingFile.Dispose();

			// No recording runs any more: the running recording ended above, or the session switched to video before the failure
			if (!IsCapturePhotoSession)
			{
				await ReturnToPhotoSessionAsync();
			}

			throw;
		}
	}

	private async partial Task<Stream> PlatformStopVideoRecording(CancellationToken token)
	{
		ArgumentNullException.ThrowIfNull(cameraExecutor);

		// Like on iOS, the recording stops also when the token is cancelled already: the token cancels waiting for CameraX to finalize
		// the recording and copying it into the stream, and a stop whose token was cancelled while it waited can be called again
		videoRecording?.Stop();

		await videoRecordingSemaphoreSlim.WaitAsync(token);

		try
		{
			return await StopRecordingAsync(token);
		}
		catch (ObjectDisposedException) when (isDisposed)
		{
			// Dispose released the recording and the stream while the stop waited (the page closed right after the user tapped stop):
			// there is no video to return, and nothing may escape from the async void StopVideoRecordingCommand
			return Stream.Null;
		}
		finally
		{
			videoRecordingSemaphoreSlim.Release();
		}
	}

	async Task<Stream> StopRecordingAsync(CancellationToken token)
	{
		if (videoRecording is null
			|| videoRecordingFile is null
			|| videoRecordingFinalizeTcs is null
			|| videoRecordingStream is null)
		{
			return Stream.Null;
		}

		var stream = videoRecordingStream;

		videoRecording.Stop();
		await videoRecordingFinalizeTcs.Task.WaitAsync(token);

		ObjectDisposedException.ThrowIf(isDisposed, this);

		try
		{
			// Without a video that can be played the caller gets the failure instead of its stream, which would look like a successful
			// recording; a video recorded until the failure (for example until StopCameraPreview closed the camera) is returned like the
			// video of a recording that was stopped
			if (videoRecordingConsumer?.FailureReason is string failureReason)
			{
				if (!HasPlayableVideo(videoRecordingConsumer.Error, videoRecordingFile))
				{
					throw new CameraException($"Video recording failed: {failureReason}");
				}

				Trace.TraceWarning($"{nameof(CameraManager)}: StopVideoRecording returns the video recorded until the recording failed: {failureReason}");
			}

			await using var inputStream = new FileStream(videoRecordingFile.AbsolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);
			await inputStream.CopyToAsync(stream, token);
			await stream.FlushAsync(token);
		}
		finally
		{
			CleanupVideoRecordingResources();
			await ReturnToPhotoSessionAsync();
		}

		if (stream.CanSeek)
		{
			stream.Position = 0;
		}

		return stream;
	}

	// The errors are classified according to the documentation of VideoRecordEvent.Finalize:
	// - ERROR_INVALID_OUTPUT_OPTIONS generates no output file, the output file of ERROR_ENCODING_FAILED and ERROR_RECORDER_ERROR "is not
	//   properly constructed", and ERROR_NO_VALID_DATA misses "the essential data for a recording to be played correctly": no video.
	// - ERROR_SOURCE_INACTIVE (the camera closed: StopCameraPreview, the app went to the background), ERROR_INSUFFICIENT_STORAGE (the storage
	//   became full while recording), ERROR_FILE_SIZE_LIMIT_REACHED, ERROR_DURATION_LIMIT_REACHED and ERROR_RECORDING_GARBAGE_COLLECTED keep
	//   the video recorded until then in the file, unless the recording ended before CameraX wrote anything, and ERROR_UNKNOWN "may or may
	//   not" generate the output file: for these errors (and any other one) the file decides, it must hold a video that Android can read.
	static bool HasPlayableVideo(int error, Java.IO.File file)
	{
		if (error is VideoRecordEvent.Finalize.ErrorInvalidOutputOptions
			or VideoRecordEvent.Finalize.ErrorEncodingFailed
			or VideoRecordEvent.Finalize.ErrorRecorderError
			or VideoRecordEvent.Finalize.ErrorNoValidData)
		{
			return false;
		}

		// The manager creates the file before the recording starts: it stays empty when CameraX wrote nothing
		if (!file.Exists() || file.Length() is 0)
		{
			return false;
		}

		using var retriever = new Android.Media.MediaMetadataRetriever();
		try
		{
			retriever.SetDataSource(file.AbsolutePath);
			return retriever.ExtractMetadata(Android.Media.MetadataKey.HasVideo) is "yes";
		}
		catch (Java.Lang.RuntimeException)
		{
			// MediaMetadataRetriever can not read the file, for example an MP4 file without the index that the recording writes last
			return false;
		}
		finally
		{
			retriever.Release();
		}
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

		videoRecordingFinalizeTcs = null;
		videoRecordingConsumer = null;
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
									  ?? throw new InvalidOperationException("Unable to get listenable future for camera provider");

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

	ICamera BindCamera(ProcessCameraProvider provider, CameraSelector cameraSelector, params UseCase[] useCases)
	{
		provider.UnbindAll();
		return provider.BindToLifecycle((ILifecycleOwner)context, cameraSelector, useCases);
	}

	void OnOrientationChanged(int orientationDegrees)
	{
		var targetRotation = GetSurfaceRotation(orientationDegrees);

		imageCapture?.TargetRotation = targetRotation;
		videoCapture?.TargetRotation = targetRotation;
		cameraPreview?.TargetRotation = targetRotation;
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
				image?.Close();
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