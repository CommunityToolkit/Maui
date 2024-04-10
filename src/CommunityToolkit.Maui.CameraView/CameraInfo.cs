using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

#if IOS || MACCATALYST
using AVFoundation;
#elif ANDROID
using AndroidX.Camera.Core;
#elif WINDOWS
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
#endif

namespace CommunityToolkit.Maui.Core;

public partial class CameraInfo : ObservableObject
{
	public string Name { get; internal set; } = string.Empty;
    public string DeviceId { get; internal set; } = string.Empty;
    public CameraPosition Position { get; internal set; } = CameraPosition.Unknown;

    internal bool Updated { get; set; } = false;

    bool isFlashSupported = false;

	// TODO: Consider other capabilities like auto focus, etc.
    public bool IsFlashSupported
    {
        get => isFlashSupported;
        internal set => SetProperty(ref isFlashSupported, value);
    }

    float minZoomFactor = 1.0f;

    public float MinZoomFactor
    {
        get => minZoomFactor;
        internal set => SetProperty(ref minZoomFactor, value);
    }

    float maxZoomFactor = 1.0f;

    public float MaxZoomFactor
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
}
