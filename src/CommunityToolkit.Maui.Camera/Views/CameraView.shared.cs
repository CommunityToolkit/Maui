using System.ComponentModel;
using System.Runtime.Versioning;
using System.Windows.Input;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// A visual element that provides the ability to show a camera preview and capture images.
/// </summary>
[SupportedOSPlatform("windows10.0.10240.0")]
[SupportedOSPlatform("android21.0")]
[SupportedOSPlatform("ios")]
[SupportedOSPlatform("maccatalyst")]
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

	static readonly BindablePropertyKey isCameraBusyPropertyKey =
		BindableProperty.CreateReadOnly(nameof(IsCameraBusy), typeof(bool), typeof(CameraView), CameraViewDefaults.IsCameraBusy);

	/// <summary>
	/// Backing <see cref="BindableProperty"/> for the <see cref="IsCameraBusy"/> property.
	/// </summary>
	public static readonly BindableProperty IsCameraBusyProperty = isCameraBusyPropertyKey.BindableProperty;

	/// <summary>
	/// Backing <see cref="BindableProperty"/> for the <see cref="SelectedCamera"/> property.
	/// </summary>
	public static readonly BindableProperty? SelectedCameraProperty = BindableProperty.Create(nameof(SelectedCamera),
		typeof(CameraInfo), typeof(CameraView), null, defaultBindingMode: BindingMode.TwoWay);

	/// <summary>
	/// Backing <see cref="BindableProperty"/> for the <see cref="ZoomFactor"/> property.
	/// </summary>
	public static readonly BindableProperty ZoomFactorProperty =
		BindableProperty.Create(nameof(ZoomFactor), typeof(float), typeof(CameraView), CameraViewDefaults.ZoomFactor, coerceValue: CoerceZoom, defaultBindingMode: BindingMode.TwoWay);

	/// <summary>
	/// Backing <see cref="BindableProperty"/> for the <see cref="ImageCaptureResolution"/> property.
	/// </summary>
	public static readonly BindableProperty ImageCaptureResolutionProperty = BindableProperty.Create(nameof(ImageCaptureResolution),
		typeof(Size), typeof(CameraView), CameraViewDefaults.ImageCaptureResolution, defaultBindingMode: BindingMode.TwoWay);

	/// <summary>
	/// Backing BindableProperty for the <see cref="CaptureImageCommand"/> property.
	/// </summary>
	public static readonly BindableProperty CaptureImageCommandProperty =
		BindableProperty.CreateReadOnly(nameof(CaptureImageCommand), typeof(Command<CancellationToken>), typeof(CameraView), null, BindingMode.OneWayToSource, defaultValueCreator: CameraViewDefaults.CreateCaptureImageCommand).BindableProperty;

	/// <summary>
	/// Backing BindableProperty for the <see cref="StartCameraPreviewCommand"/> property.
	/// </summary>
	public static readonly BindableProperty StartCameraPreviewCommandProperty =
		BindableProperty.CreateReadOnly(nameof(StartCameraPreviewCommand), typeof(Command<CancellationToken>), typeof(CameraView), null, BindingMode.OneWayToSource, defaultValueCreator: CameraViewDefaults.CreateStartCameraPreviewCommand).BindableProperty;

	/// <summary>
	/// Backing BindableProperty for the <see cref="StopCameraPreviewCommand"/> property.
	/// </summary>
	public static readonly BindableProperty StopCameraPreviewCommandProperty =
		BindableProperty.CreateReadOnly(nameof(StopCameraPreviewCommand), typeof(ICommand), typeof(CameraView), null, BindingMode.OneWayToSource, defaultValueCreator: CameraViewDefaults.CreateStopCameraPreviewCommand).BindableProperty;

	/// <summary>
	/// Backing BindableProperty for the <see cref="StartVideoRecordingCommand"/> property.
	/// </summary>
	public static readonly BindableProperty StartVideoRecordingCommandProperty =
		BindableProperty.CreateReadOnly(nameof(StartVideoRecordingCommand), typeof(Command<Stream>), typeof(CameraView), null, BindingMode.OneWayToSource, defaultValueCreator: CameraViewDefaults.CreateStartVideoRecordingCommand).BindableProperty;

	/// <summary>
	/// Backing BindableProperty for the <see cref="StopVideoRecordingCommand"/> property.
	/// </summary>
	public static readonly BindableProperty StopVideoRecordingCommandProperty =
		BindableProperty.CreateReadOnly(nameof(StopVideoRecordingCommand), typeof(Command<CancellationToken>), typeof(CameraView), null, BindingMode.OneWayToSource, defaultValueCreator: CameraViewDefaults.CreateStopVideoRecordingCommand).BindableProperty;


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
	/// Gets a value indicating whether the camera feature is available on the current device.
	/// </summary>
	public bool IsAvailable => (bool)GetValue(IsAvailableProperty);

	/// <summary>
	/// Gets a value indicating whether the camera is currently busy.
	/// </summary>
	public bool IsCameraBusy => (bool)GetValue(IsCameraBusyProperty);

	/// <summary>
	/// Gets the Command that triggers an image capture.
	/// </summary>
	/// <remarks>
	/// <see cref="CaptureImageCommand"/> has a <see cref="Type"/> of Command&lt;CancellationToken&gt; which requires a <see cref="CancellationToken"/> as a CommandParameter. See <see cref="Command{CancellationToken}"/> and <see cref="System.Windows.Input.ICommand.Execute(object)"/> for more information on passing a <see cref="CancellationToken"/> into <see cref="Command{T}"/> as a CommandParameter
	/// </remarks>
	public Command<CancellationToken> CaptureImageCommand => (Command<CancellationToken>)GetValue(CaptureImageCommandProperty);

	/// <summary>
	/// Gets the Command that starts the camera preview.
	/// </summary>
	/// /// <remarks>
	/// <see cref="StartCameraPreviewCommand"/> has a <see cref="Type"/> of Command&lt;CancellationToken&gt; which requires a <see cref="CancellationToken"/> as a CommandParameter. See <see cref="Command{CancellationToken}"/> and <see cref="System.Windows.Input.ICommand.Execute(object)"/> for more information on passing a <see cref="CancellationToken"/> into <see cref="Command{T}"/> as a CommandParameter
	/// </remarks>
	public Command<CancellationToken> StartCameraPreviewCommand => (Command<CancellationToken>)GetValue(StartCameraPreviewCommandProperty);

	/// <summary>
	/// Gets the Command that stops the camera preview.
	/// </summary>
	public ICommand StopCameraPreviewCommand => (ICommand)GetValue(StopCameraPreviewCommandProperty);

	/// <summary>
	/// Gets the Command that starts video recording.
	/// </summary>
	/// <remarks>
	/// <see cref="StartVideoRecordingCommand"/> has a <see cref="Type"/> of Command&lt;Stream&gt; which requires a <see cref="Stream"/> as a CommandParameter.
	/// The <see cref="Stream"/> parameter represents the destination where the recorded video will be saved.
	/// See <see cref="Command{Stream}"/> and <see cref="System.Windows.Input.ICommand.Execute(object)"/> for more information on passing a <see cref="Stream"/> into <see cref="Command{T}"/> as a CommandParameter.
	/// </remarks>
	public Command<Stream> StartVideoRecordingCommand => (Command<Stream>)GetValue(StartVideoRecordingCommandProperty);

	/// <summary>
	/// Gets the Command that stops video recording.
	/// </summary>
	/// <remarks>
	/// <see cref="StopVideoRecordingCommand"/> has a <see cref="Type"/> of Command&lt;CancellationToken&gt;, which requires a <see cref="CancellationToken"/> as a CommandParameter. 
	/// See <see cref="Command{CancellationToken}"/> and <see cref="System.Windows.Input.ICommand.Execute(object)"/> for more information on passing a <see cref="CancellationToken"/> into <see cref="Command{T}"/> as a CommandParameter.
	/// </remarks>
	public Command<CancellationToken> StopVideoRecordingCommand => (Command<CancellationToken>)GetValue(StopVideoRecordingCommandProperty);

	/// <summary>
	/// Gets or sets the <see cref="CameraFlashMode"/>.
	/// </summary>
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

	/// <summary>
	/// Gets or sets a value indicating whether the torch (flash) is on.
	/// </summary>
	public bool IsTorchOn
	{
		get => (bool)GetValue(IsTorchOnProperty);
		set => SetValue(IsTorchOnProperty, value);
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

			if (CameraProvider.AvailableCameras is null)
			{
				throw new CameraException("Unable to refresh available cameras");
			}
		}

		return CameraProvider.AvailableCameras;
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