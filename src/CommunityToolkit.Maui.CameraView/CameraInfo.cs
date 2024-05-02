using CommunityToolkit.Maui.Core.Primitives;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

#if IOS || MACCATALYST
using AVFoundation;
#elif ANDROID
using AndroidX.Camera.Core;
#elif WINDOWS
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
#endif

namespace CommunityToolkit.Maui.Core;

public partial class CameraInfo : INotifyPropertyChanged
{
    public string? Name { get; internal set; }
    public string? DeviceId { get; internal set; }
    public CameraPosition? Position { get; internal set; }

    bool isFlashSupported = false;

	// TODO: Consider other capabilities like auto focus, etc.
    public bool IsFlashSupported
    {
        get => isFlashSupported;
        internal set => SetProperty(ref isFlashSupported, value);
    }

    float? minZoomFactor;

    public float? MinZoomFactor
    {
        get => minZoomFactor;
        internal set => SetProperty(ref minZoomFactor, value);
    }

    float? maxZoomFactor;

	public float? MaxZoomFactor
    {
        get => maxZoomFactor;
        internal set => SetProperty(ref maxZoomFactor, value);
    }

    public ObservableCollection<Size> SupportedResolutions { get; internal set; } = new();

#if ANDROID
    internal CameraSelector? CameraSelector { get; set; }
#endif

#if IOS || MACCATALYST
	internal AVCaptureDevice? CaptureDevice {  get; set; }
	internal List<AVCaptureDeviceFormat> formats { get; set; } = new();
#endif

#if WINDOWS
    internal MediaFrameSourceGroup? FrameSourceGroup { get; set; }
	internal List<ImageEncodingProperties> ImageEncodingProperties { get; set; } = new();
#endif

    public override string ToString()
    {
        string s = string.Empty;

        if (SupportedResolutions is not null)
        {
            foreach (var r in SupportedResolutions)
            {
                string final = r == SupportedResolutions.Last() ? "." : ", ";
                s += $"{r.Width}x{r.Height}{final}";
            }
        }
        return $"Camera Info => Name:{Name}, id:{DeviceId}, position:{Position}, hasFlash:{IsFlashSupported}, minZoom:{MinZoomFactor}, maxZoom:{MaxZoomFactor}" +
            $"\nresolutions {s} ";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
}
