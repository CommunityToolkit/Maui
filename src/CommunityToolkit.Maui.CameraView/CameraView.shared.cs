using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Views;

public partial class CameraView : View, ICameraView
{
    static readonly WeakEventManager weakEventManager = new();

    public static readonly BindableProperty CameraFlashModeProperty =
        BindableProperty.Create(nameof(CameraFlashMode), typeof(CameraFlashMode), typeof(CameraView), CameraFlashMode.Off);

    public static readonly BindableProperty IsTorchOnProperty =
        BindableProperty.Create(nameof(IsTorchOn), typeof(bool), typeof(CameraView), false);

    static readonly BindablePropertyKey isAvailablePropertyKey =
        BindableProperty.CreateReadOnly(nameof(IsAvailable), typeof(bool), typeof(CameraView), false);

    public static readonly BindableProperty IsAvailableProperty = isAvailablePropertyKey.BindableProperty;

    static readonly BindablePropertyKey isCameraBusyPropertyKey =
        BindableProperty.CreateReadOnly(nameof(IsCameraBusy), typeof(bool), typeof(CameraView), false);

    public static readonly BindableProperty IsCameraBusyProperty = isCameraBusyPropertyKey.BindableProperty;

    public static readonly BindableProperty? SelectedCameraProperty = BindableProperty.Create(nameof(SelectedCamera),
        typeof(CameraInfo), typeof(CameraView), null, defaultBindingMode: BindingMode.TwoWay);

    public static readonly BindableProperty ZoomFactorProperty =
        BindableProperty.Create(nameof(ZoomFactor), typeof(float), typeof(CameraView), 1.0f, coerceValue: CoerceZoom, defaultBindingMode: BindingMode.TwoWay);

    public static readonly BindableProperty CaptureResolutionProperty = BindableProperty.Create(nameof(CaptureResolution),
        typeof(Size), typeof(CameraView), Size.Zero, defaultBindingMode: BindingMode.TwoWay);

    public CameraFlashMode CameraFlashMode
    {
        get => (CameraFlashMode)GetValue(CameraFlashModeProperty);
        set => SetValue(CameraFlashModeProperty, value);
    }

    public CameraInfo? SelectedCamera
    {
        get { return (CameraInfo?)GetValue(SelectedCameraProperty); }
        set { SetValue(SelectedCameraProperty, value); }
    }

    public float ZoomFactor
    {
        get { return (float)GetValue(ZoomFactorProperty); }
        set { SetValue(ZoomFactorProperty, value); }
    }

    public Size CaptureResolution
    {
        get { return (Size)GetValue(CaptureResolutionProperty); }
        set { SetValue(CaptureResolutionProperty, value); }
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
            input = (float)view.SelectedCamera.MinZoomFactor;
        }
        else if (input > view.SelectedCamera.MaxZoomFactor)
        {
            input = (float)view.SelectedCamera.MaxZoomFactor;
        }
        return input;
    }

    public bool IsTorchOn
    {
        get => (bool)GetValue(IsTorchOnProperty);
        set => SetValue(IsTorchOnProperty, value);
    }

    public bool IsAvailable => (bool)GetValue(IsAvailableProperty);

    [EditorBrowsable(EditorBrowsableState.Never)]
    bool IAvailability.IsAvailable
    {
        get => IsAvailable;
        set => SetValue(isAvailablePropertyKey, value);
    }

    public bool IsCameraBusy => (bool)GetValue(IsCameraBusyProperty);

    [EditorBrowsable(EditorBrowsableState.Never)]
    bool IAvailability.IsBusy
    {
        get => IsCameraBusy;
        set => SetValue(isCameraBusyPropertyKey, value);
    }

    public event EventHandler<MediaCaptureFailedEventArgs> MediaCaptureFailed
    {
        add => weakEventManager.AddEventHandler(value);
        remove => weakEventManager.RemoveEventHandler(value);
    }

    public event EventHandler<MediaCapturedEventArgs> MediaCaptured
    {
        add => weakEventManager.AddEventHandler(value);
        remove => weakEventManager.RemoveEventHandler(value);
    }

    public void OnAvailable()
    {
    }

    public void OnMediaCaptured(Stream imageData)
    {
        weakEventManager.HandleEvent(this, new MediaCapturedEventArgs(imageData), nameof(MediaCaptured));
    }

    public void OnMediaCapturedFailed()
    {
        weakEventManager.HandleEvent(this, new MediaCaptureFailedEventArgs(), nameof(MediaCaptureFailed));
    }

    public void Shutter()
    {
        Handler?.Invoke(nameof(ICameraView.Shutter));
    }

    public void Start()
    {
        Handler?.Invoke(nameof(ICameraView.Start));
    }

    public void Stop()
    {
        Handler?.Invoke(nameof(ICameraView.Stop));
    }

}
