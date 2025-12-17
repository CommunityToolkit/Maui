using System.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// A <see cref="View"/> that provides the ability to show a camera preview and capture images and record video.
/// </summary>
public partial class CameraView : View, ICameraView, IDisposable
{
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

	/// <inheritdoc cref="ICameraView.IsAvailable"/>
	[BindableProperty]
	public partial bool IsAvailable { get; } = CameraViewDefaults.IsAvailable;

	/// <inheritdoc cref="ICameraView.IsBusy"/>
	[BindableProperty]
	public partial bool IsBusy { get; } = CameraViewDefaults.IsCameraBusy;

	/// <summary>
	/// Gets the <see cref="Command{CancellationToken}"/> that triggers an image capture.
	/// </summary>
	/// <remarks>
	/// <see cref="CaptureImageCommand"/> has a <see cref="Type"/> of Command&lt;CancellationToken&gt; which requires a <see cref="CancellationToken"/> as a CommandParameter. See <see cref="Command{CancellationToken}"/> and <see cref="System.Windows.Input.ICommand.Execute(object)"/> for more information on passing a <see cref="CancellationToken"/> into <see cref="Command{T}"/> as a CommandParameter
	/// </remarks>
	[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateCaptureImageCommand), DefaultBindingMode = BindingMode.OneWayToSource)]
	public partial Command<CancellationToken> CaptureImageCommand { get; }

	/// <summary>
	/// Gets the <see cref="Command{CancellationToken}"/> that starts the camera preview.
	/// </summary>
	/// <remarks>
	/// <see cref="StartCameraPreviewCommand"/> has a <see cref="Type"/> of Command&lt;CancellationToken&gt; which requires a <see cref="CancellationToken"/> as a CommandParameter. See <see cref="Command{CancellationToken}"/> and <see cref="System.Windows.Input.ICommand.Execute(object)"/> for more information on passing a <see cref="CancellationToken"/> into <see cref="Command{T}"/> as a CommandParameter
	/// </remarks>
	[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateStartCameraPreviewCommand), DefaultBindingMode = BindingMode.OneWayToSource)]
	public partial Command<CancellationToken> StartCameraPreviewCommand { get; }

	/// <summary>
	/// Gets the <see cref="Command{CancellationToken}"/> that stops the camera preview.
	/// </summary>
	/// <remarks>
	/// <see cref="StopCameraPreviewCommand"/> has a <see cref="Type"/> of Command&lt;CancellationToken&gt; which requires a <see cref="CancellationToken"/> as a CommandParameter. See <see cref="Command{CancellationToken}"/> and <see cref="System.Windows.Input.ICommand.Execute(object)"/> for more information on passing a <see cref="CancellationToken"/> into <see cref="Command{T}"/> as a CommandParameter
	/// </remarks>
	[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateStopCameraPreviewCommand), DefaultBindingMode = BindingMode.OneWayToSource)]
	public partial Command<CancellationToken> StopCameraPreviewCommand { get; }

	/// <summary>
	/// Gets the <see cref="Command{Stream}"/> that starts video recording.
	/// </summary>
	/// <remarks>
	/// <see cref="StartVideoRecordingCommand"/> has a <see cref="Type"/> of Command&lt;Stream&gt; which requires a <see cref="Stream"/> as a CommandParameter. See <see cref="Command{Stream}"/> and <see cref="System.Windows.Input.ICommand.Execute(object)"/> for more information on passing a <see cref="Stream"/> into <see cref="Command{T}"/> as a CommandParameter
	/// </remarks>
	[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateStartVideoRecordingCommand), DefaultBindingMode = BindingMode.OneWayToSource)]
	public partial Command<Stream> StartVideoRecordingCommand { get; }

	/// <summary>
	/// Gets the <see cref="Command{CancellationToken}"/> that stops video recording.
	/// </summary>
	/// <remarks>
	/// <see cref="StopVideoRecordingCommand"/> has a <see cref="Type"/> of Command&lt;CancellationToken&gt; which requires a <see cref="CancellationToken"/> as a CommandParameter. See <see cref="Command{CancellationToken}"/> and <see cref="System.Windows.Input.ICommand.Execute(object)"/> for more information on passing a <see cref="CancellationToken"/> into <see cref="Command{T}"/> as a CommandParameter
	/// </remarks>
	[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateStopVideoRecordingCommand), DefaultBindingMode = BindingMode.OneWayToSource)]
	public partial Command<CancellationToken> StopVideoRecordingCommand { get; }

	/// <inheritdoc cref="ICameraView.CameraFlashMode"/>
	[BindableProperty]
	public partial CameraFlashMode CameraFlashMode { get; set; } = CameraViewDefaults.CameraFlashMode;

	/// <inheritdoc cref="ICameraView.SelectedCamera"/>
	[BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
	public partial CameraInfo? SelectedCamera { get; set; }

	/// <inheritdoc cref="ICameraView.ZoomFactor"/>
	[BindableProperty(DefaultBindingMode = BindingMode.TwoWay, CoerceValueMethodName = nameof(CoerceZoom))]
	public partial float ZoomFactor { get; set; } = CameraViewDefaults.ZoomFactor;

	/// <inheritdoc cref="ICameraView.ImageCaptureResolution"/>
	[BindableProperty(DefaultBindingMode = BindingMode.TwoWay)]
	public partial Size ImageCaptureResolution { get; set; } = CameraViewDefaults.ImageCaptureResolution;

	/// <inheritdoc cref="ICameraView.IsTorchOn"/>
	[BindableProperty]
	public partial bool IsTorchOn { get; set; } = CameraViewDefaults.IsTorchOn;

	new CameraViewHandler Handler => (CameraViewHandler)(base.Handler ?? throw new InvalidOperationException("Unable to retrieve Handler"));

	[EditorBrowsable(EditorBrowsableState.Never)]
	bool ICameraView.IsAvailable
	{
		get => IsAvailable;
		set => SetValue(isAvailablePropertyKey, value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	bool ICameraView.IsBusy
	{
		get => IsBusy;
		set => SetValue(isBusyPropertyKey, value);
	}

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

	static Command<CancellationToken> CreateCaptureImageCommand(BindableObject bindable)
	{
		var cameraView = (CameraView)bindable;
		return new(async token => await cameraView.CaptureImage(token).ConfigureAwait(false));
	}

	static Command<CancellationToken> CreateStartCameraPreviewCommand(BindableObject bindable)
	{
		var cameraView = (CameraView)bindable;
		return new(async token => await cameraView.StartCameraPreview(token).ConfigureAwait(false));
	}

	static Command CreateStopCameraPreviewCommand(BindableObject bindable)
	{
		var cameraView = (CameraView)bindable;
		return new(_ => cameraView.StopCameraPreview());
	}

	static Command<Stream> CreateStartVideoRecordingCommand(BindableObject bindable)
	{
		var cameraView = (CameraView)bindable;
		return new(async stream => await cameraView.StartVideoRecording(stream).ConfigureAwait(false));
	}

	static Command<CancellationToken> CreateStopVideoRecordingCommand(BindableObject bindable)
	{
		var cameraView = (CameraView)bindable;
		return new(async token => await cameraView.StopVideoRecording(token).ConfigureAwait(false));
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

	void ICameraView.OnMediaCaptured(Stream imageData)
	{
		weakEventManager.HandleEvent(this, new MediaCapturedEventArgs(imageData), nameof(MediaCaptured));
	}

	void ICameraView.OnMediaCapturedFailed(string failureReason)
	{
		weakEventManager.HandleEvent(this, new MediaCaptureFailedEventArgs(failureReason), nameof(MediaCaptureFailed));
	}
}