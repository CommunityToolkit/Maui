using CommunityToolkit.Maui.Core;
using Windows.Devices.Enumeration;

namespace CommunityToolkit.Maui.Extensions;

static class CameraViewExtensions
{
	public static async Task UpdateAvailability(this IAvailability cameraView, CancellationToken token)
	{
		var videoCaptureDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

		token.ThrowIfCancellationRequested();

		cameraView.IsAvailable = videoCaptureDevices.Count > 0;
	}
}