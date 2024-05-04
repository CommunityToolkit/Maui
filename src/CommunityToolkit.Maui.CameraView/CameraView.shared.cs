using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// A visual element that provides the ability to show a camera preview and capture images.
/// </summary>
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
	/// Backing <see cref="BindableProperty"/> for the <see cref="CaptureResolution"/> property.
	/// </summary>
    public static readonly BindableProperty CaptureResolutionProperty = BindableProperty.Create(nameof(CaptureResolution),
        typeof(Size), typeof(CameraView), Size.Zero, defaultBindingMode: BindingMode.TwoWay);
	
	readonly WeakEventManager weakEventManager = new();
	
	
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

    /// <inheritdoc cref="ICameraView.CaptureResolution"/>
    public Size CaptureResolution
    {
        get => (Size)GetValue(CaptureResolutionProperty);
        set => SetValue(CaptureResolutionProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the torch is on.
    /// </summary>
    public bool IsTorchOn
    {
        get => (bool)GetValue(IsTorchOnProperty);
        set => SetValue(IsTorchOnProperty, value);
    }
	
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

    /// <inheritdoc cref="ICameraView.Shutter"/>
    public void Shutter()
    {
        Handler?.Invoke(nameof(ICameraView.Shutter));
    }

    /// <inheritdoc cref="ICameraView.Start"/>
    public void Start()
    {
        Handler?.Invoke(nameof(ICameraView.Start));
    }

    /// <inheritdoc cref="ICameraView.Stop"/>
    public void Stop()
    {
        Handler?.Invoke(nameof(ICameraView.Stop));
    }
	
	static object CoerceZoom(BindableObject bindable, object value)
	{
		CameraView view = (CameraView)bindable;
		float input = (float)value;

		if (view.SelectedCamera is null)
		{
			return input;
		}

		if (input < view.SelectedCamera.MinZoomFactor)
		{
			input = view.SelectedCamera.MinZoomFactor;
		}
		else if (input > view.SelectedCamera.MaxZoomFactor)
		{
			input = view.SelectedCamera.MaxZoomFactor;
		}
		
		return input;
	}
}
