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

/// <summary>
/// Represents information about a camera device.
/// </summary>
public class CameraInfo : INotifyPropertyChanged
{
    /// <summary>
    /// Gets the name of the camera device.
    /// </summary>
	public string Name { get; internal set; } = string.Empty;

    /// <summary>
    /// Gets the unique identifier of the camera device.
    /// </summary>
    public string DeviceId { get; internal set; } = string.Empty;

    /// <summary>
    /// Gets the <see cref="CameraPosition"/> of the camera device.
    /// </summary>
    public CameraPosition Position { get; internal set; } = CameraPosition.Unknown;

    bool isFlashSupported = false;

	/// <summary>
    /// Gets a value indicating whether the camera device supports flash.
    /// </summary>
    public bool IsFlashSupported
    {
        get => isFlashSupported;
        internal set => SetProperty(ref isFlashSupported, value);
    }

    float minZoomFactor = 1.0f;

    /// <summary>
    /// Gets the minimum zoom factor supported by the camera device.
    /// </summary>
    public float MinZoomFactor
    {
        get => minZoomFactor;
        internal set => SetProperty(ref minZoomFactor, value);
    }

    float maxZoomFactor = 1.0f;

    /// <summary>
    /// Gets the maximum zoom factor supported by the camera device.
    /// </summary>
    public float MaxZoomFactor
    {
        get => maxZoomFactor;
        internal set => SetProperty(ref maxZoomFactor, value);
    }

    /// <summary>
    /// Gets the supported resolutions of the camera device.
    /// </summary>
    public ObservableCollection<Size> SupportedResolutions { get; internal set; } = [];

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

    /// <inheritdoc cref="object.ToString" />
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

    /// <inheritdoc cref="INotifyPropertyChanged.PropertyChanged" />
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
