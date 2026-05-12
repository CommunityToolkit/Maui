using CommunityToolkit.Maui.Core;
using Windows.Devices.Enumeration;
using Windows.Media;
using Windows.Media.Capture;

namespace CommunityToolkit.Maui.Extensions;

static class CameraViewExtensions
{
	public static async Task UpdateAvailability(this ICameraView cameraView, CancellationToken token)
	{
		var videoCaptureDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture).AsTask(token);

		cameraView.IsAvailable = videoCaptureDevices.Count > 0;
	}

	public static async Task InitializeCameraForCameraView(this MediaCapture mediaCapture, string deviceId, CancellationToken token)
	{
		try
		{
			var settings = new MediaCaptureInitializationSettings
			{
				VideoDeviceId = deviceId,
				PhotoCaptureSource = PhotoCaptureSource.Auto
			};

			PermissionStatus microphonePermissionStatus = PermissionStatus.Unknown;
			
			// unpackaged apps always have the capability
			var isMicrophoneCapable =
				AppInfo.PackagingModel != AppPackagingModel.Packaged ||
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
		}
		catch (System.Runtime.InteropServices.COMException)
		{
			// Camera already initialized
		}
	}
}