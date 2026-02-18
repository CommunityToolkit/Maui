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
		BindableProperty.CreateReadOnly(nameof(StopCameraPreviewCommand), typeof(Command<CancellationToken>), typeof(CameraView), null, BindingMode.OneWayToSource, defaultValueCreator: CreateStopCameraPreviewCommand).BindableProperty;

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

	static ICameraProvider CameraProvider => IPlatformApplication.Current?.Services.GetRequiredService<ICameraProvider>() ?? throw new CameraException("Unable to retrieve CameraProvider");

	/// <inheritdoc cref="ICameraView.IsAvailable"/>
	public bool IsAvailable => (bool)GetValue(IsAvailableProperty);

	/// <inheritdoc cref="ICameraView.IsBusy"/>
	public bool IsBusy => (bool)GetValue(IsBusyProperty);

	/// <summary>
	/// Gets the <see cref="Command{CancellationToken}"/> that triggers an image capture.
	/// </summary>
	/// <remarks>
	/// <see cref="CaptureImageCommand"/> has a <see cref="Type"/> of Command&lt;CancellationToken&gt; which requires a <see cref="CancellationToken"/> as a CommandParameter. See <see cref="Command{CancellationToken}"/> and <see cref="System.Windows.Input.ICommand.Execute(object)"/> for more information on passing a <see cref="CancellationToken"/> into <see cref="Command{T}"/> as a CommandParameter
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
	/// Gets the <see cref="Command{CancellationToken}"/> that stops the camera preview.
	/// </summary>
	/// <remarks>
	/// <see cref="StopCameraPreviewCommand"/> has a <see cref="Type"/> of Command&lt;CancellationToken&gt; which requires a <see cref="CancellationToken"/> as a CommandParameter. See <see cref="Command{CancellationToken}"/> and <see cref="System.Windows.Input.ICommand.Execute(object)"/> for more information on passing a <see cref="CancellationToken"/> into <see cref="Command{T}"/> as a CommandParameter
	/// </remarks>
	public Command<CancellationToken> StopCameraPreviewCommand => (Command<CancellationToken>)GetValue(StopCameraPreviewCommandProperty);

	/// <summary>
	/// Gets the <see cref="Command{Stream}"/> that starts video recording.
	/// </summary>
	/// <remarks>
	/// <see cref="StartVideoRecordingCommand"/> has a <see cref="Type"/> of Command&lt;Stream&gt; which requires a <see cref="Stream"/> as a CommandParameter. See <see cref="Command{Stream}"/> and <see cref="System.Windows.Input.ICommand.Execute(object)"/> for more information on passing a <see cref="Stream"/> into <see cref="Command{T}"/> as a CommandParameter
	/// </remarks>
	public Command<Stream> StartVideoRecordingCommand => (Command<Stream>)GetValue(StartVideoRecordingCommandProperty);

	/// <summary>
	/// Gets the <see cref="Command{CancellationToken}"/> that stops video recording.
	/// </summary>
	/// <remarks>
	/// <see cref="StopVideoRecordingCommand"/> has a <see cref="Type"/> of Command&lt;CancellationToken&gt; which requires a <see cref="CancellationToken"/> as a CommandParameter. See <see cref="Command{CancellationToken}"/> and <see cref="System.Windows.Input.ICommand.Execute(object)"/> for more information on passing a <see cref="CancellationToken"/> into <see cref="Command{T}"/> as a CommandParameter
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