using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// A visual element that provides the ability to show a camera preview and capture images.
/// </summary>
public partial class CameraView : View, ICameraView, IDisposable
{
	/// <summary>
	/// Gets the <see cref="BindableProperty"/> indicating whether the <see cref="IsAvailable"/> is available on the current device.
	/// </summary>
	[BindableProperty(DefaultValue = CameraViewDefaults.IsAvailable)]
	public partial bool IsAvailable { get; }

	/// <summary>
	/// Gets or sets the <see cref="CameraFlashMode"/> property.
	/// </summary>
	[BindableProperty(DefaultValue = nameof(CameraViewDefaults.CameraFlashMode))]
	public partial CameraFlashMode CameraFlashMode { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="BindableProperty"/> for the <see cref="IsTorchOn"/> property.
	/// </summary>
	[BindableProperty(DefaultValue = CameraViewDefaults.IsTorchOn)]
	public partial bool IsTorchOn { get; set; }

	/// <summary>
	/// Gets the <see cref="BindableProperty"/> for the <see cref="IsCameraBusy"/> property.
	/// </summary>
	[BindableProperty(DefaultValue = CameraViewDefaults.IsCameraBusy)]
	public partial bool IsCameraBusy { get; }

	/// <summary>
	/// Gets or sets the <see cref="BindableProperty"/> for the <see cref="SelectedCamera"/> property.
	/// </summary>
	[BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
	public partial CameraInfo? SelectedCamera { get; set; }
	
	/// <summary>
	/// Gets or sets the <see cref="BindableProperty"/> for the <see cref="ZoomFactor"/> property.
	/// </summary>
	[BindableProperty(DefaultValue = CameraViewDefaults.ZoomFactor, DefaultBindingMode = BindingMode.TwoWay, CoerceValueMethodName = nameof(CoerceZoom))]
	public partial float ZoomFactor { get; set; }

	/// <summary>
	/// Gets or sets the <see cref="BindableProperty"/> for the <see cref="ImageCaptureResolution"/> property.
	/// </summary>
	[BindableProperty(DefaultValue = nameof(CameraViewDefaults.ImageCaptureResolution), DefaultBindingMode = BindingMode.TwoWay)]
	public partial Size ImageCaptureResolution { get; set; }

	/// <summary>
	/// Gets the <see cref="BindableProperty"/> for the <see cref="CaptureImageCommand"/> property.
	/// </summary>
	[BindableProperty(DefaultValueCreatorMethodName = nameof(CameraViewDefaults.CreateCaptureImageCommand), DefaultBindingMode = BindingMode.OneWayToSource)]
	public partial Command<CancellationToken> CaptureImageCommand { get; }

	/// <summary>
	/// Gets the <see cref="BindableProperty"/> for the <see cref="StartCameraPreviewCommand"/> property.
	/// </summary>
	[BindableProperty(DefaultValueCreatorMethodName = nameof(CameraViewDefaults.CreateStartCameraPreviewCommand), DefaultBindingMode = BindingMode.OneWayToSource)]
	public partial Command<CancellationToken> StartCameraPreviewCommand { get; }

	/// <summary>
	/// Gets the <see cref="BindableProperty"/> for the <see cref="StopCameraPreviewCommand"/> property.
	/// </summary>
	[BindableProperty(DefaultValueCreatorMethodName = nameof(CameraViewDefaults.CreateStopCameraPreviewCommand), DefaultBindingMode = BindingMode.OneWayToSource)]
	public partial Command<CancellationToken> StopCameraPreviewCommand { get; }

	/// <summary>
	/// Gets the <see cref="BindableProperty"/> for the <see cref="StartVideoRecordingCommand"/> property.
	/// </summary>
	[BindableProperty(DefaultValue = nameof(CameraViewDefaults.CreateStartVideoRecordingCommand), DefaultBindingMode = BindingMode.OneWayToSource)]
	public partial Command<Stream> StartVideoRecordingCommand { get; }

	/// <summary>
	/// Gets the <see cref="BindableProperty"/> for the <see cref="StopVideoRecordingCommand"/> property.
	/// </summary>
	[BindableProperty(DefaultValue = nameof(CameraViewDefaults.CreateStopVideoRecordingCommand), DefaultBindingMode = BindingMode.OneWayToSource)]
	public partial Command<CancellationToken> StopVideoRecordingCommand { get; }

	readonly SemaphoreSlim captureImageSemaphoreSlim = new(1, 1);
	readonly WeakEventManager weakEventManager = new();

	bool isDisposed;

	/// <summary>
	/// Event that is raised when the camera capture fails.
	/// </summary>
	public event EventHandler<MediaCaptureFailedEventArgs> MediaCaptureFailed
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Event that is raised when the camera captures an image.
	/// </summary>
	/// <remarks>
	/// The <see cref="MediaCapturedEventArgs"/> contains the captured image data.
	/// </remarks>
	public event EventHandler<MediaCapturedEventArgs> MediaCaptured
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}

	static ICameraProvider CameraProvider => IPlatformApplication.Current?.Services.GetRequiredService<ICameraProvider>() ?? throw new CameraException("Unable to retrieve CameraProvider");

	[EditorBrowsable(EditorBrowsableState.Never)]
	bool ICameraView.IsAvailable
	{
		get => IsAvailable;
		set => SetValue(isAvailablePropertyKey, value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	bool ICameraView.IsBusy
	{
		get => IsCameraBusy;
		set => SetValue(isCameraBusyPropertyKey, value);
	}

	new CameraViewHandler Handler => (CameraViewHandler)(base.Handler ?? throw new InvalidOperationException("Unable to retrieve Handler"));

	/// <inheritdoc/>
	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	/// <inheritdoc cref="ICameraView.GetAvailableCameras"/>
	public async ValueTask<IReadOnlyList<CameraInfo>> GetAvailableCameras(CancellationToken token)
	{
		if (CameraProvider.AvailableCameras is null)
		{
			await CameraProvider.RefreshAvailableCameras(token);
		}
		return CameraProvider.AvailableCameras ?? throw new CameraException("No camera available on device");
	}

#if ANDROID
	/// <summary>
	/// Set Extension Mode
	/// </summary>
	/// <param name="mode">mode</param>
	public Task SetExtensionMode(int mode, CancellationToken token = default)
	{
		return Handler.CameraManager.SetExtensionMode(mode, token);
	}
#endif

	/// <inheritdoc cref="ICameraView.CaptureImage"/>
	public async Task<Stream> CaptureImage(CancellationToken token)
	{
		// Use SemaphoreSlim to ensure `MediaCaptured` and `MediaCaptureFailed` events are unsubscribed before calling `TakePicture` again
		// Without this SemaphoreSlim, previous calls to this method will fire `MediaCaptured` and/or `MediaCaptureFailed` events causing this method to return the wrong Stream or throw the wrong Exception
		await captureImageSemaphoreSlim.WaitAsync(token);

		var mediaStreamTCS = new TaskCompletionSource<Stream>(TaskCreationOptions.RunContinuationsAsynchronously);

		MediaCaptured += HandleMediaCaptured;
		MediaCaptureFailed += HandleMediaCapturedFailed;

		try
		{
			await Handler.CameraManager.TakePicture(token);

			var stream = await mediaStreamTCS.Task.WaitAsync(token);
			return stream;
		}
		finally
		{
			MediaCaptured -= HandleMediaCaptured;
			MediaCaptureFailed -= HandleMediaCapturedFailed;

			// Release SemaphoreSlim after `MediaCaptured` and `MediaCaptureFailed` events are unsubscribed
			captureImageSemaphoreSlim.Release();
		}

		void HandleMediaCaptured(object? sender, MediaCapturedEventArgs e) => mediaStreamTCS.SetResult(e.Media);

		void HandleMediaCapturedFailed(object? sender, MediaCaptureFailedEventArgs e) => mediaStreamTCS.SetException(new CameraException(e.FailureReason));
	}

	/// <inheritdoc cref="ICameraView.StartCameraPreview"/>
	public Task StartCameraPreview(CancellationToken token) =>
		Handler.CameraManager.StartCameraPreview(token);

	/// <inheritdoc cref="ICameraView.StopCameraPreview"/>
	public void StopCameraPreview() =>
		Handler.CameraManager.StopCameraPreview();

	/// <inheritdoc cref="ICameraView.StartVideoRecording(CancellationToken)"/>
	public Task StartVideoRecording(CancellationToken token = default) =>
		StartVideoRecording(new MemoryStream(), token);

	/// <inheritdoc cref="ICameraView.StartVideoRecording(Stream,CancellationToken)"/>
	public Task StartVideoRecording(Stream stream, CancellationToken token = default) =>
		Handler.CameraManager.StartVideoRecording(stream, token);

	/// <inheritdoc cref="ICameraView.StopVideoRecording"/>
	public async Task<TStream> StopVideoRecording<TStream>(CancellationToken token = default) where TStream : Stream
	{
		var stream = await Handler.CameraManager.StopVideoRecording(token);
		return (TStream)stream;
	}

	/// <inheritdoc cref="ICameraView.StopVideoRecording"/>
	public Task<Stream> StopVideoRecording(CancellationToken token = default) =>
		Handler.CameraManager.StopVideoRecording(token);

	/// <inheritdoc/>
	protected virtual void Dispose(bool disposing)
	{
		if (!isDisposed)
		{
			if (disposing)
			{
				captureImageSemaphoreSlim.Dispose();
			}

			isDisposed = true;
		}
	}

	void ICameraView.OnMediaCaptured(Stream imageData)
	{
		weakEventManager.HandleEvent(this, new MediaCapturedEventArgs(imageData), nameof(MediaCaptured));
	}

	void ICameraView.OnMediaCapturedFailed(string failureReason)
	{
		weakEventManager.HandleEvent(this, new MediaCaptureFailedEventArgs(failureReason), nameof(MediaCaptureFailed));
	}

	static object CoerceZoom(BindableObject bindable, object value)
	{
		var cameraView = (CameraView)bindable;
		var input = (float)value;

		if (cameraView.SelectedCamera is null)
		{
			return input;
		}

		if (input < cameraView.SelectedCamera.MinimumZoomFactor)
		{
			input = cameraView.SelectedCamera.MinimumZoomFactor;
		}
		else if (input > cameraView.SelectedCamera.MaximumZoomFactor)
		{
			input = cameraView.SelectedCamera.MaximumZoomFactor;
		}

		return input;
	}
}