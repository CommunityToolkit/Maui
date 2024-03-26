using Windows.Devices.Enumeration;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Extensions;

public static class CameraViewExtensions
{

    public static void UpdateAvailability(this IAvailability cameraView)
    {
        cameraView.IsAvailable = DeviceInformation.FindAllAsync(DeviceClass.VideoCapture).GetAwaiter().GetResult().Count > 0;
    }
}
