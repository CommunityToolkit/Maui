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
			var isMicrophoneCapable = Permissions.IsCapabilityDeclared("microphone");

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

			var cameraPermissionStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
			if (cameraPermissionStatus == PermissionStatus.Granted)
			{
				await mediaCapture.InitializeAsync(settings).AsTask(token);
			}
			else
			{
				throw new CameraException("Camera permission is not granted.");
			}
		}
		catch (System.Runtime.InteropServices.COMException)
		{
			// Camera already initialized
		}
	}
}