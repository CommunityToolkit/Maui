using CommunityToolkit.Maui.Core;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;

namespace CommunityToolkit.Maui.Extensions;

static class CameraViewExtensions
{
	public static async Task UpdateAvailability(this IAvailability cameraView, CancellationToken token)
	{
		var videoCaptureDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

		token.ThrowIfCancellationRequested();

		cameraView.IsAvailable = videoCaptureDevices.Count > 0;
	}

	public static async Task InitializeCameraForCameraView(this MediaCapture mediaCapture, string deviceId, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		await mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
		{
			VideoDeviceId = deviceId,
			PhotoCaptureSource = PhotoCaptureSource.Auto
		});
	}
}