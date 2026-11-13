using CommunityToolkit.Maui.Core;
using Windows.Devices.Enumeration;
using Windows.Media;
using Windows.Media.Capture;

namespace CommunityToolkit.Maui.Extensions;

static class CameraViewExtensions
{
	public static async Task UpdateAvailability(this ICameraView cameraView, CancellationToken token)
	{
		// AsTask only asks the WinRT operation to cancel, and an enumeration that completes first still returns its devices
		token.ThrowIfCancellationRequested();

		var videoCaptureDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture).AsTask(token);

		cameraView.IsAvailable = videoCaptureDevices.Count > 0;
	}

	// Returns false when Media Foundation could not open the camera (a COMException, for example for a device id that no camera has; Windows 11
	// still opens a camera that another capture holds): the camera discovery skips that camera, the preview reports it
	public static async Task<bool> InitializeCameraForCameraView(this MediaCapture mediaCapture, string deviceId, CancellationToken token)
	{
		// AsTask only asks the WinRT operation to cancel, and an initialization that ends first (Media Foundation rejects an unknown device at once)
		// still reports its own outcome
		token.ThrowIfCancellationRequested();

		try
		{
			var settings = new MediaCaptureInitializationSettings
			{
				VideoDeviceId = deviceId,
				PhotoCaptureSource = PhotoCaptureSource.Auto
			};

			PermissionStatus microphonePermissionStatus = PermissionStatus.Unknown;

			// Permission checks require a packaged app manifest on Windows
			var isMicrophoneCapable =
				AppInfo.PackagingModel == AppPackagingModel.Packaged &&
				Permissions.IsCapabilityDeclared("microphone");

			if (isMicrophoneCapable)
			{
				microphonePermissionStatus = await Permissions.CheckStatusAsync<Permissions.Microphone>();
			}

			if (!isMicrophoneCapable || microphonePermissionStatus != PermissionStatus.Granted)
			{
				settings.StreamingCaptureMode = StreamingCaptureMode.Video;
				settings.MediaCategory = MediaCategory.Media;
				settings.AudioProcessing = AudioProcessing.Default;
			}

			await mediaCapture.InitializeAsync(settings).AsTask(token);

			return true;
		}
		catch (System.Runtime.InteropServices.COMException)
		{
			return false;
		}
	}
}