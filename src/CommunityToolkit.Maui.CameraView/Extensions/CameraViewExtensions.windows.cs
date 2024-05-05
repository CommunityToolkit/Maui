using CommunityToolkit.Maui.Core;
using Windows.Devices.Enumeration;

namespace CommunityToolkit.Maui.Extensions;

public static class CameraViewExtensions
{

	public static void UpdateAvailability(this IAvailability cameraView)
	{
		cameraView.IsAvailable = DeviceInformation.FindAllAsync(DeviceClass.VideoCapture).GetAwaiter().GetResult().Count > 0;
	}
}