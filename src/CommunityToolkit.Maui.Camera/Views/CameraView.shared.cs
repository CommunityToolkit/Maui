using System.ComponentModel;
using System.Runtime.Versioning;
using System.Windows.Input;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// A visual element that provides the ability to show a camera preview and capture images.
/// </summary>
[SupportedOSPlatform("windows10.0.10240.0")]
[SupportedOSPlatform("android21.0")]
[SupportedOSPlatform("ios")]
[SupportedOSPlatform("maccatalyst")]
public class CameraView : View, ICameraView
{
	static readonly BindablePropertyKey isAvailablePropertyKey =
		BindableProperty.CreateReadOnly(nameof(IsAvailable), typeof(bool), typeof(CameraView), false);

	/// <summary>
	/// Backing <see cref="BindableProperty"/> for the <see cref="CameraFlashMode"/> property.
	/// </summary>
	public static readonly BindableProperty CameraFlashModeProperty =
		BindableProperty.Create(nameof(CameraFlashMode), typeof(CameraFlashMode), typeof(CameraView), CameraFlashMode.Off);

	/// <summary>
	/// Backing <see cref="BindableProperty"/> for the <see cref="IsTorchOn"/> property.
	/// </summary>
	public static readonly BindableProperty IsTorchOnProperty =
		BindableProperty.Create(nameof(IsTorchOn), typeof(bool), typeof(CameraView), false);

	/// <summary>
	/// Backing <see cref="BindableProperty"/> for the <see cref="IsAvailable"/> property.
	/// </summary>
	public static readonly BindableProperty IsAvailableProperty = isAvailablePropertyKey.BindableProperty;

	static readonly BindablePropertyKey isCameraBusyPropertyKey =
		BindableProperty.CreateReadOnly(nameof(IsCameraBusy), typeof(bool), typeof(CameraView), false);

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
		BindableProperty.Create(nameof(ZoomFactor), typeof(float), typeof(CameraView), 1.0f, coerceValue: CoerceZoom, defaultBindingMode: BindingMode.TwoWay);

	/// <summary>
	/// Backing <see cref="BindableProperty"/> for the <see cref="ImageCaptureResolution"/> property.
	/// </summary>
	public static readonly BindableProperty ImageCaptureResolutionProperty = BindableProperty.Create(nameof(ImageCaptureResolution),
		typeof(Size), typeof(CameraView), Size.Zero, defaultBindingMode: BindingMode.TwoWay);

	/// <summary>
	/// Backing BindableProperty for the <see cref="CaptureImageCommand"/> property.
	/// </summary>
	public static readonly BindableProperty CaptureImageCommandProperty =
		BindableProperty.CreateReadOnly(nameof(CaptureImageCommand), typeof(ICommand), typeof(CameraView), default, BindingMode.OneWayToSource, defaultValueCreator: CreateCaptureImageCommand).BindableProperty;

	/// <summary>
	/// Backing BindableProperty for the <see cref="StartCameraPreviewCommand"/> property.
	/// </summary>
	public static readonly BindableProperty StartCameraPreviewCommandProperty =
		BindableProperty.CreateReadOnly(nameof(StartCameraPreviewCommand), typeof(ICommand), typeof(CameraView), default, BindingMode.OneWayToSource, defaultValueCreator: CreateStartCameraPreviewCommand).BindableProperty;

	/// <summary>
	/// Backing BindableProperty for the <see cref="StopCameraPreviewCommand"/> property.
	/// </summary>
	public static readonly BindableProperty StopCameraPreviewCommandProperty =
		BindableProperty.CreateReadOnly(nameof(StopCameraPreviewCommand), typeof(ICommand), typeof(CameraView), default, BindingMode.OneWayToSource, defaultValueCreator: CreateStopCameraPreviewCommand).BindableProperty;

	readonly WeakEventManager weakEventManager = new();

	TaskCompletionSource handlerCompletedTCS = new();

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
	/// Gets or sets the <see cref="CameraFlashMode"/>.
	/// </summary>
	public CameraFlashMode CameraFlashMode
	{
		get => (CameraFlashMode)GetValue(CameraFlashModeProperty);
		set => SetValue(CameraFlashModeProperty, value);
	}

	/// <summary>
	/// Gets the Command that triggers an image capture.
	/// </summary>
	/// <remarks>
	/// <see cref="CaptureImageCommand"/> has a <see cref="Type"/> of Command&lt;CancellationToken&gt; which requires a <see cref="CancellationToken"/> as a CommandParameter. See <see cref="Command{CancellationToken}"/> and <see cref="System.Windows.Input.ICommand.Execute(object)"/> for more information on passing a <see cref="CancellationToken"/> into <see cref="Command{T}"/> as a CommandParameter"
	/// </remarks>
	public ICommand CaptureImageCommand => (ICommand)GetValue(CaptureImageCommandProperty);

	/// <inheritdoc cref="ICameraView.SelectedCamera"/>
	public CameraInfo? SelectedCamera
	{
		get => (CameraInfo?)GetValue(SelectedCameraProperty);
		set => SetValue(SelectedCameraProperty, value);
	}

	/// <summary>
	/// Gets the Command that starts the camera preview.
	/// </summary>
	public ICommand StartCameraPreviewCommand => (ICommand)GetValue(StartCameraPreviewCommandProperty);

	/// <summary>
	/// Gets the Command that stops the camera preview.
	/// </summary>
	public ICommand StopCameraPreviewCommand => (ICommand)GetValue(StopCameraPreviewCommandProperty);

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
	/// Gets or sets a value indicating whether the torch is on.
	/// </summary>
	public bool IsTorchOn
	{
		get => (bool)GetValue(IsTorchOnProperty);
		set => SetValue(IsTorchOnProperty, value);
	}

	static CameraProvider cameraProvider => IPlatformApplication.Current?.Services.GetRequiredService<CameraProvider>() ?? throw new CameraViewException("Unable to retrieve CameraProvider");

	[EditorBrowsable(EditorBrowsableState.Never)]
	bool IAvailability.IsAvailable
	{
		get => IsAvailable;
		set => SetValue(isAvailablePropertyKey, value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	bool IAvailability.IsBusy
	{
		get => IsCameraBusy;
		set => SetValue(isCameraBusyPropertyKey, value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	TaskCompletionSource IAsynchronousHandler.HandlerCompleteTCS => handlerCompletedTCS;

	/// <inheritdoc cref="ICameraView.OnMediaCaptured"/>
	public void OnMediaCaptured(Stream imageData)
	{
		weakEventManager.HandleEvent(this, new MediaCapturedEventArgs(imageData), nameof(MediaCaptured));
	}

	/// <inheritdoc cref="ICameraView.OnMediaCapturedFailed"/>
	public void OnMediaCapturedFailed()
	{
		weakEventManager.HandleEvent(this, new MediaCaptureFailedEventArgs(), nameof(MediaCaptureFailed));
	}

	/// <inheritdoc cref="ICameraView.GetAvailableCameras"/>
	public async ValueTask<IReadOnlyList<CameraInfo>> GetAvailableCameras(CancellationToken token)
	{
		if (cameraProvider.AvailableCameras is null)
		{
			await cameraProvider.RefreshAvailableCameras(token);

			if(cameraProvider.AvailableCameras is null)
			{
				throw new CameraViewException("No cameras found on device");
			}
		}

		return cameraProvider.AvailableCameras;
	}

	/// <inheritdoc cref="ICameraView.CaptureImage"/>
	public async ValueTask CaptureImage(CancellationToken token)
	{
		handlerCompletedTCS.TrySetCanceled(token);

		handlerCompletedTCS = new();
		Handler?.Invoke(nameof(ICameraView.CaptureImage));

		await handlerCompletedTCS.Task.WaitAsync(token);
	}

	/// <inheritdoc cref="ICameraView.StartCameraPreview"/>
	public async ValueTask StartCameraPreview(CancellationToken token)
	{
		handlerCompletedTCS.TrySetCanceled(token);

		handlerCompletedTCS = new();
		Handler?.Invoke(nameof(ICameraView.StartCameraPreview));

		await handlerCompletedTCS.Task.WaitAsync(token);
	}

	/// <inheritdoc cref="ICameraView.StopCameraPreview"/>
	public void StopCameraPreview()
	{
		Handler?.Invoke(nameof(ICameraView.StopCameraPreview));
	}

	static object CoerceZoom(BindableObject bindable, object value)
	{
		CameraView view = (CameraView)bindable;
		float input = (float)value;

		if (view.SelectedCamera is null)
		{
			return input;
		}

		if (input < view.SelectedCamera.MinimumZoomFactor)
		{
			input = view.SelectedCamera.MinimumZoomFactor;
		}
		else if (input > view.SelectedCamera.MaximumZoomFactor)
		{
			input = view.SelectedCamera.MaximumZoomFactor;
		}

		return input;
	}

	static Command CreateCaptureImageCommand(BindableObject bindable)
	{
		var cameraView = (CameraView)bindable;
		return new Command(async token => await cameraView.CaptureImage(CancellationToken.None).ConfigureAwait(false));
	}

	static Command CreateStartCameraPreviewCommand(BindableObject bindable)
	{
		var cameraView = (CameraView)bindable;
		return new Command(async token => await cameraView.StartCameraPreview(CancellationToken.None).ConfigureAwait(false));
	}

	static Command CreateStopCameraPreviewCommand(BindableObject bindable)
	{
		var cameraView = (CameraView)bindable;
		return new Command(token => cameraView.StopCameraPreview());
	}
}