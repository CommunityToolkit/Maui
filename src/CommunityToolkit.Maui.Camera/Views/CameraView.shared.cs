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

	// Only the platform's answer to the picture (ICameraView.OnMediaCaptured or OnMediaCapturedFailed) completes it: a failure that a command
	// reports through MediaCaptureFailed does not
	TaskCompletionSource<PictureOutcome>? pendingPictureTCS;

	/// <summary>
	/// Event that is raised when the camera capture fails.
	/// </summary>
	/// <remarks>
	/// It also reports the failures of <see cref="CaptureImageCommand"/>, <see cref="StartVideoRecordingCommand"/> and
	/// <see cref="StopVideoRecordingCommand"/>, which let no exception escape.
	/// </remarks>
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
	/// handler connects the view (for example when no camera is available, when the app does not have the camera permission, or on Android when
	/// CameraX cannot initialize), switching to the camera that <see cref="SelectedCamera"/> selects, applying <see cref="ImageCaptureResolution"/>,
	/// on Android the start of the preview that the activity retries once the app has the camera permission (see
	/// <see cref="StartCameraPreview(CancellationToken)"/>) and, on Windows, updating <see cref="IsAvailable"/>. Failures after the handler was
	/// disconnected are not reported. It also reports a camera that <see cref="StartCameraPreview(CancellationToken)"/> cannot start, instead of an
	/// exception, on every platform, and the failures of <see cref="StartCameraPreviewCommand"/> and <see cref="StopCameraPreviewCommand"/>, which
	/// let no exception escape.
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
	/// The command lets no exception escape: <see cref="System.Windows.Input.ICommand.Execute(object)"/> cannot return the task of the capture,
	/// so an exception would reach no caller and end the app. The image reaches the app through <see cref="MediaCaptured"/>, and a capture that
	/// fails through <see cref="MediaCaptureFailed"/>, once, also when it fails before the platform could take the picture (for example when the
	/// view has no handler). A capture that is cancelled through the token ends without an event.
	/// </para>
	/// </remarks>
	public Command<CancellationToken> CaptureImageCommand => (Command<CancellationToken>)GetValue(CaptureImageCommandProperty);

	/// <summary>
	/// Gets the <see cref="Command{CancellationToken}"/> that starts the camera preview.
	/// </summary>
	/// <remarks>
	/// <see cref="StartCameraPreviewCommand"/> has a <see cref="Type"/> of Command&lt;CancellationToken&gt; which requires a <see cref="CancellationToken"/> as a CommandParameter. See <see cref="Command{CancellationToken}"/> and <see cref="System.Windows.Input.ICommand.Execute(object)"/> for more information on passing a <see cref="CancellationToken"/> into <see cref="Command{T}"/> as a CommandParameter
	/// <para>
	/// The command lets no exception escape: a preview that cannot start, or a view without a handler, is reported through
	/// <see cref="ErrorOccurred"/>, once. A start that is cancelled through the token ends without an event.
	/// </para>
	/// </remarks>
	public Command<CancellationToken> StartCameraPreviewCommand => (Command<CancellationToken>)GetValue(StartCameraPreviewCommandProperty);

	/// <summary>
	/// Gets the <see cref="ICommand"/> that stops the camera preview.
	/// </summary>
	/// <remarks>
	/// The command lets no exception escape: a failure, for example of a view without a handler, is reported through
	/// <see cref="ErrorOccurred"/>.
	/// </remarks>
	public ICommand StopCameraPreviewCommand => (ICommand)GetValue(StopCameraPreviewCommandProperty);

	/// <summary>
	/// Gets the <see cref="Command{Stream}"/> that starts video recording.
	/// </summary>
	/// <remarks>
	/// <see cref="StartVideoRecordingCommand"/> has a <see cref="Type"/> of Command&lt;Stream&gt; which requires a <see cref="Stream"/> as a CommandParameter. See <see cref="Command{Stream}"/> and <see cref="System.Windows.Input.ICommand.Execute(object)"/> for more information on passing a <see cref="Stream"/> into <see cref="Command{T}"/> as a CommandParameter
	/// <para>
	/// The command lets no exception escape: <see cref="System.Windows.Input.ICommand.Execute(object)"/> cannot return the task of the recording,
	/// so an exception would reach no caller and end the app. A recording that cannot start (an exception of
	/// <see cref="StartVideoRecording(Stream, CancellationToken)"/>, such as its <see cref="CameraException"/>) is reported through
	/// <see cref="MediaCaptureFailed"/> with the message of the exception, once.
	/// </para>
	/// </remarks>
	public Command<Stream> StartVideoRecordingCommand => (Command<Stream>)GetValue(StartVideoRecordingCommandProperty);

	/// <summary>
	/// Gets the <see cref="Command{CancellationToken}"/> that stops video recording.
	/// </summary>
	/// <remarks>
	/// <see cref="StopVideoRecordingCommand"/> has a <see cref="Type"/> of Command&lt;CancellationToken&gt; which requires a <see cref="CancellationToken"/> as a CommandParameter. See <see cref="Command{CancellationToken}"/> and <see cref="System.Windows.Input.ICommand.Execute(object)"/> for more information on passing a <see cref="CancellationToken"/> into <see cref="Command{T}"/> as a CommandParameter
	/// <para>
	/// The command lets no exception escape: <see cref="System.Windows.Input.ICommand.Execute(object)"/> cannot return the task of the stop, so an
	/// exception would reach no caller and end the app. The video reaches the stream that was passed to
	/// <see cref="StartVideoRecordingCommand"/>; a stop that fails (an exception of <see cref="StopVideoRecording(CancellationToken)"/>, such as
	/// the <see cref="CameraException"/> of a recording that failed without a usable video) is reported through <see cref="MediaCaptureFailed"/>
	/// with the message of the exception, once. A stop that is cancelled through the token ends without an event.
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
		var picture = await TakePicture(token);

		return picture.Image ?? throw new CameraException(picture.FailureReason);
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
		// TrySetResult: a second answer of the platform must not throw into the platform's callback
		Volatile.Read(ref pendingPictureTCS)?.TrySetResult(new(imageData, string.Empty));

		weakEventManager.HandleEvent(this, new MediaCapturedEventArgs(imageData), nameof(MediaCaptured));
	}

	void ICameraView.OnMediaCapturedFailed(string failureReason)
	{
		Volatile.Read(ref pendingPictureTCS)?.TrySetResult(new(null, failureReason));

		RaiseMediaCaptureFailed(failureReason);
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

		// MediaCaptured and MediaCaptureFailed report what the platform answered: the command reports only what failed before
		return new(async token => await ExecuteCommand(() => cameraView.TakePicture(token), cameraView.ReportMediaCaptureFailure, token));
	}

	static Command<CancellationToken> CreateStartCameraPreviewCommand(BindableObject bindable)
	{
		var cameraView = (CameraView)bindable;

		// StartCameraPreview reports a preview that cannot start through ErrorOccurred itself
		return new(async token => await ExecuteCommand(() => cameraView.StartCameraPreview(token), cameraView.ReportError, token));
	}

	static ICommand CreateStopCameraPreviewCommand(BindableObject bindable)
	{
		var cameraView = (CameraView)bindable;
		return new Command(() =>
		{
			// An exception would escape to the caller of ICommand.Execute, which is MAUI when the command is bound in XAML
			try
			{
				cameraView.StopCameraPreview();
			}
			catch (Exception ex)
			{
				cameraView.ReportError(ex);
			}
		});
	}

	static Command<Stream> CreateStartVideoRecordingCommand(BindableObject bindable)
	{
		var cameraView = (CameraView)bindable;
		return new(async stream => await ExecuteCommand(() => cameraView.StartVideoRecording(stream), cameraView.ReportMediaCaptureFailure));
	}

	static Command<CancellationToken> CreateStopVideoRecordingCommand(BindableObject bindable)
	{
		var cameraView = (CameraView)bindable;
		return new(async token => await ExecuteCommand(() => cameraView.StopVideoRecording(token), cameraView.ReportMediaCaptureFailure, token));
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

	// ICommand.Execute cannot return the task of the work that a command starts: an exception that escaped the command would reach no caller
	// and end the app, and an app that binds the command in XAML cannot catch it
	static async Task ExecuteCommand(Func<Task> work, Action<Exception> reportFailure, CancellationToken token = default)
	{
		try
		{
			await work();
		}
		catch (OperationCanceledException) when (token.IsCancellationRequested)
		{
		}
		catch (Exception ex)
		{
			reportFailure(ex);
		}
	}

	// What fails before the platform answered, for example a view without a handler, throws
	async Task<PictureOutcome> TakePicture(CancellationToken token)
	{
		// One capture at a time: the platform's answer completes the pending capture, so a second capture waits until the first one has ended
		await captureImageSemaphoreSlim.WaitAsync(token);

		var pictureOutcomeTCS = new TaskCompletionSource<PictureOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
		Volatile.Write(ref pendingPictureTCS, pictureOutcomeTCS);

		try
		{
			await Handler.CameraManager.TakePicture(token);

			return await pictureOutcomeTCS.Task.WaitAsync(token);
		}
		finally
		{
			// Cleared before the semaphore is released: an answer that arrives while no capture waits completes nothing
			Volatile.Write(ref pendingPictureTCS, null);

			captureImageSemaphoreSlim.Release();
		}
	}

	// A failure of a command is reported to the app only: it is not the platform's answer to a capture that is pending
	void ReportMediaCaptureFailure(Exception exception) => RaiseMediaCaptureFailed(exception.Message);

	void RaiseMediaCaptureFailed(string failureReason) =>
		weakEventManager.HandleEvent(this, new MediaCaptureFailedEventArgs(failureReason), nameof(MediaCaptureFailed));

	void ReportError(Exception exception) => ((ICameraView)this).OnErrorOccurred(exception);

	readonly record struct PictureOutcome(Stream? Image, string FailureReason);
}