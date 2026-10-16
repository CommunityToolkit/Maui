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
	static readonly BindablePropertyKey isAvailablePropertyKey =
		BindableProperty.CreateReadOnly(nameof(IsAvailable), typeof(bool), typeof(CameraView), CameraViewDefaults.IsAvailable);

	/// <summary>
	/// Backing <see cref="BindableProperty"/> for the <see cref="CameraFlashMode"/> property.
	/// </summary>
	public static readonly BindableProperty CameraFlashModeProperty =
		BindableProperty.Create(nameof(CameraFlashMode), typeof(CameraFlashMode), typeof(CameraView), CameraViewDefaults.CameraFlashMode);

	/// <summary>
	/// Backing <see cref="BindableProperty"/> for the <see cref="IsTorchOn"/> property.
	/// </summary>
	public static readonly BindableProperty IsTorchOnProperty =
		BindableProperty.Create(nameof(IsTorchOn), typeof(bool), typeof(CameraView), CameraViewDefaults.IsTorchOn);

	/// <summary>
	/// Backing <see cref="BindableProperty"/> for the <see cref="IsAvailable"/> property.
	/// </summary>
	public static readonly BindableProperty IsAvailableProperty = isAvailablePropertyKey.BindableProperty;

	static readonly BindablePropertyKey isBusyPropertyKey =
		BindableProperty.CreateReadOnly(nameof(IsBusy), typeof(bool), typeof(CameraView), CameraViewDefaults.IsCameraBusy);

	/// <summary>
	/// Backing <see cref="BindableProperty"/> for the <see cref="IsBusy"/> property.
	/// </summary>
	public static readonly BindableProperty IsBusyProperty = isBusyPropertyKey.BindableProperty;

	/// <summary>
	/// Backing <see cref="BindableProperty"/> for the <see cref="SelectedCamera"/> property.
	/// </summary>
	public static readonly BindableProperty SelectedCameraProperty = BindableProperty.Create(nameof(SelectedCamera),
		typeof(CameraInfo), typeof(CameraView), null, defaultBindingMode: BindingMode.TwoWay);

	/// <summary>
	/// Backing <see cref="BindableProperty"/> for the <see cref="ZoomFactor"/> property.
	/// </summary>
	public static readonly BindableProperty ZoomFactorProperty =
		BindableProperty.Create(nameof(ZoomFactor), typeof(float), typeof(CameraView), CameraViewDefaults.ZoomFactor, coerceValue: CoerceZoom, defaultBindingMode: BindingMode.TwoWay);

	/// <summary>
	/// Bindable property for the <see cref="ImageCaptureResolution"/> property.
	/// </summary>
	public static readonly BindableProperty ImageCaptureResolutionProperty = BindableProperty.Create(nameof(ImageCaptureResolution),
		typeof(Size), typeof(CameraView), CameraViewDefaults.ImageCaptureResolution, defaultBindingMode: BindingMode.TwoWay);

	/// <summary>
	/// Bindable property for the <see cref="CaptureImageCommand"/> property.
	/// </summary>
	public static readonly BindableProperty CaptureImageCommandProperty =
		BindableProperty.CreateReadOnly(nameof(CaptureImageCommand), typeof(Command<CancellationToken>), typeof(CameraView), null, BindingMode.OneWayToSource, defaultValueCreator: CreateCaptureImageCommand).BindableProperty;

	/// <summary>
	/// Bindable property for the <see cref="StartCameraPreviewCommand"/> property.
	/// </summary>
	public static readonly BindableProperty StartCameraPreviewCommandProperty =
		BindableProperty.CreateReadOnly(nameof(StartCameraPreviewCommand), typeof(Command<CancellationToken>), typeof(CameraView), null, BindingMode.OneWayToSource, defaultValueCreator: CreateStartCameraPreviewCommand).BindableProperty;

	/// <summary>
	/// Bindable property for the <see cref="StopCameraPreviewCommand"/> property.
	/// </summary>
	public static readonly BindableProperty StopCameraPreviewCommandProperty =
		BindableProperty.CreateReadOnly(nameof(StopCameraPreviewCommand), typeof(ICommand), typeof(CameraView), null, BindingMode.OneWayToSource, defaultValueCreator: CreateStopCameraPreviewCommand).BindableProperty;

	/// <summary>
	/// Bindable property for the <see cref="StartVideoRecordingCommand"/> property.
	/// </summary>
	public static readonly BindableProperty StartVideoRecordingCommandProperty =
		BindableProperty.CreateReadOnly(nameof(StartVideoRecordingCommand), typeof(Command<Stream>), typeof(CameraView), null, BindingMode.OneWayToSource, defaultValueCreator: CreateStartVideoRecordingCommand).BindableProperty;

	/// <summary>
	/// Bindable property for the <see cref="StopVideoRecordingCommand"/> property.
	/// </summary>
	public static readonly BindableProperty StopVideoRecordingCommandProperty =
		BindableProperty.CreateReadOnly(nameof(StopVideoRecordingCommand), typeof(Command<CancellationToken>), typeof(CameraView), null, BindingMode.OneWayToSource, defaultValueCreator: CreateStopVideoRecordingCommand).BindableProperty;

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

	/// <summary>
	/// Event that is raised when an error occurs.
	/// </summary>
	/// <remarks>
	/// It reports the failures of the work that the handler starts on its own, which no caller could catch: connecting the camera when the
	/// handler connects the view (for example when no camera is available, when the camera permission was removed, or on Android when CameraX
	/// cannot initialize), switching to the camera that <see cref="SelectedCamera"/> selects, applying <see cref="ImageCaptureResolution"/>
	/// and, on Windows, updating <see cref="IsAvailable"/>. Failures after the handler was disconnected are not reported. On Windows it also
	/// reports a camera that <see cref="StartCameraPreview(CancellationToken)"/> cannot start, instead of an exception.
	/// </remarks>
	public event EventHandler<ErrorOccurredEventArgs> ErrorOccurred
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}

	/// <inheritdoc cref="ICameraView.IsAvailable"/>
	public bool IsAvailable => (bool)GetValue(IsAvailableProperty);

	/// <inheritdoc cref="ICameraView.IsBusy"/>
	public bool IsBusy => (bool)GetValue(IsBusyProperty);

	/// <summary>
	/// Gets the <see cref="Command{CancellationToken}"/> that triggers an image capture.
	/// </summary>
	/// <remarks>
	/// <see cref="CaptureImageCommand"/> has a <see cref="Type"/> of Command&lt;CancellationToken&gt; which requires a <see cref="CancellationToken"/> as a CommandParameter. See <see cref="Command{CancellationToken}"/> and <see cref="System.Windows.Input.ICommand.Execute(object)"/> for more information on passing a <see cref="CancellationToken"/> into <see cref="Command{T}"/> as a CommandParameter
	/// <para>
	/// The command does not catch the exceptions of <see cref="CaptureImage(CancellationToken)"/>, such as the <see cref="CameraException"/> of a
	/// capture that fails. <see cref="System.Windows.Input.ICommand.Execute(object)"/> cannot return the task of the capture, so such an exception
	/// reaches no caller: it is raised as an unhandled exception, which usually ends the app. To handle failures, call
	/// <see cref="CaptureImage(CancellationToken)"/> and catch its exceptions.
	/// </para>
	/// </remarks>
	public Command<CancellationToken> CaptureImageCommand => (Command<CancellationToken>)GetValue(CaptureImageCommandProperty);

	/// <summary>
	/// Gets the <see cref="Command{CancellationToken}"/> that starts the camera preview.
	/// </summary>
	/// <remarks>
	/// <see cref="StartCameraPreviewCommand"/> has a <see cref="Type"/> of Command&lt;CancellationToken&gt; which requires a <see cref="CancellationToken"/> as a CommandParameter. See <see cref="Command{CancellationToken}"/> and <see cref="System.Windows.Input.ICommand.Execute(object)"/> for more information on passing a <see cref="CancellationToken"/> into <see cref="Command{T}"/> as a CommandParameter
	/// </remarks>
	public Command<CancellationToken> StartCameraPreviewCommand => (Command<CancellationToken>)GetValue(StartCameraPreviewCommandProperty);

	/// <summary>
	/// Gets the <see cref="ICommand"/> that stops the camera preview.
	/// </summary>
	public ICommand StopCameraPreviewCommand => (ICommand)GetValue(StopCameraPreviewCommandProperty);

	/// <summary>
	/// Gets the <see cref="Command{Stream}"/> that starts video recording.
	/// </summary>
	/// <remarks>
	/// <see cref="StartVideoRecordingCommand"/> has a <see cref="Type"/> of Command&lt;Stream&gt; which requires a <see cref="Stream"/> as a CommandParameter. See <see cref="Command{Stream}"/> and <see cref="System.Windows.Input.ICommand.Execute(object)"/> for more information on passing a <see cref="Stream"/> into <see cref="Command{T}"/> as a CommandParameter
	/// <para>
	/// The command does not catch the exceptions of <see cref="StartVideoRecording(Stream, CancellationToken)"/>, such as the
	/// <see cref="CameraException"/> of a recording that cannot start. <see cref="System.Windows.Input.ICommand.Execute(object)"/> cannot return the
	/// task of the recording, so such an exception reaches no caller: it is raised as an unhandled exception, which usually ends the app. To handle
	/// failures, call <see cref="StartVideoRecording(Stream, CancellationToken)"/> and catch its exceptions.
	/// </para>
	/// </remarks>
	public Command<Stream> StartVideoRecordingCommand => (Command<Stream>)GetValue(StartVideoRecordingCommandProperty);

	/// <summary>
	/// Gets the <see cref="Command{CancellationToken}"/> that stops video recording.
	/// </summary>
	/// <remarks>
	/// <see cref="StopVideoRecordingCommand"/> has a <see cref="Type"/> of Command&lt;CancellationToken&gt; which requires a <see cref="CancellationToken"/> as a CommandParameter. See <see cref="Command{CancellationToken}"/> and <see cref="System.Windows.Input.ICommand.Execute(object)"/> for more information on passing a <see cref="CancellationToken"/> into <see cref="Command{T}"/> as a CommandParameter
	/// <para>
	/// The command does not catch the exceptions of <see cref="StopVideoRecording(CancellationToken)"/>, such as the <see cref="CameraException"/>
	/// of a recording that failed without a usable video. <see cref="System.Windows.Input.ICommand.Execute(object)"/> cannot return the task of
	/// the stop, so such an exception reaches no caller: it is raised as an unhandled exception, which usually ends the app. To handle failures,
	/// call <see cref="StopVideoRecording(CancellationToken)"/> and catch its exceptions.
	/// </para>
	/// </remarks>
	public Command<CancellationToken> StopVideoRecordingCommand => (Command<CancellationToken>)GetValue(StopVideoRecordingCommandProperty);

	/// <inheritdoc cref="ICameraView.CameraFlashMode"/>
	public CameraFlashMode CameraFlashMode
	{
		get => (CameraFlashMode)GetValue(CameraFlashModeProperty);
		set => SetValue(CameraFlashModeProperty, value);
	}

	/// <inheritdoc cref="ICameraView.SelectedCamera"/>
	public CameraInfo? SelectedCamera
	{
		get => (CameraInfo?)GetValue(SelectedCameraProperty);
		set => SetValue(SelectedCameraProperty, value);
	}

	/// <inheritdoc cref="ICameraView.ZoomFactor"/>
	public float ZoomFactor
	{
		get => (float)GetValue(ZoomFactorProperty);
		set => SetValue(ZoomFactorProperty, value);
	}

	/// <inheritdoc cref="ICameraView.ImageCaptureResolution"/>
	public Size ImageCaptureResolution
	{
		get => (Size)GetValue(ImageCaptureResolutionProperty);
		set => SetValue(ImageCaptureResolutionProperty, value);
	}

	/// <inheritdoc cref="ICameraView.IsTorchOn"/>
	public bool IsTorchOn
	{
		get => (bool)GetValue(IsTorchOnProperty);
		set => SetValue(IsTorchOnProperty, value);
	}

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

	static ICameraProvider CameraProvider => IPlatformApplication.Current?.Services.GetRequiredService<ICameraProvider>() ?? throw new CameraException("Unable to retrieve CameraProvider");

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
	/// <param name="token"></param>
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

	/// <summary>
	/// Stops the ongoing video recording operation and returns the stream that received the video as a <typeparamref name="TStream"/>.
	/// </summary>
	/// <remarks>
	/// Works like <see cref="StopVideoRecording(CancellationToken)"/> and casts its result to <typeparamref name="TStream"/>.
	/// </remarks>
	/// <typeparam name="TStream">The type of the stream that the recording was started with.</typeparam>
	/// <param name="token">A cancellation token that can be used to cancel the stop operation.</param>
	/// <returns>A task that represents the asynchronous stop operation. Its result is the stream that received the video.</returns>
	/// <exception cref="InvalidCastException">Thrown when the result of <see cref="StopVideoRecording(CancellationToken)"/> is not a
	/// <typeparamref name="TStream"/>: the recording was started with a stream of another type, or the result is <see cref="Stream.Null"/>
	/// because no recording runs (for example on a second call after the recording was stopped), or, on Android, iOS and Mac Catalyst, because
	/// the camera was released while the stop waited for the platform to finish the recording.</exception>
	/// <exception cref="CameraException">Thrown in the cases that <see cref="StopVideoRecording(CancellationToken)"/> describes.</exception>
	public async Task<TStream> StopVideoRecording<TStream>(CancellationToken token = default) where TStream : Stream
	{
		var stream = await Handler.CameraManager.StopVideoRecording(token);
		return (TStream)stream;
	}

	/// <inheritdoc cref="ICameraView.StopVideoRecording"/>
	public Task<Stream> StopVideoRecording(CancellationToken token = default) =>
		Handler.CameraManager.StopVideoRecording(token);

	void ICameraView.OnMediaCaptured(Stream imageData)
	{
		weakEventManager.HandleEvent(this, new MediaCapturedEventArgs(imageData), nameof(MediaCaptured));
	}

	void ICameraView.OnMediaCapturedFailed(string failureReason)
	{
		weakEventManager.HandleEvent(this, new MediaCaptureFailedEventArgs(failureReason), nameof(MediaCaptureFailed));
	}

	void ICameraView.OnErrorOccurred(Exception ex)
	{
		weakEventManager.HandleEvent(this, new ErrorOccurredEventArgs(ex), nameof(ErrorOccurred));
	}

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

	static ICommand CreateStopCameraPreviewCommand(BindableObject bindable)
	{
		var cameraView = (CameraView)bindable;
		return new Command(cameraView.StopCameraPreview);
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
}