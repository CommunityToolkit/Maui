using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Extensions;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;

namespace CommunityToolkit.Maui.Core;

partial class CameraProvider
{
	public async partial ValueTask RefreshAvailableCameras(CancellationToken token)
	{
		var deviceInfoCollection = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture).AsTask(token);
		var mediaFrameSourceGroup = await MediaFrameSourceGroup.FindAllAsync().AsTask(token);
		var videoCaptureSourceGroup = mediaFrameSourceGroup.Where(sourceGroup => deviceInfoCollection.Any(deviceInfo => deviceInfo.Id == sourceGroup.Id)).ToList();
		var mediaCapture = new MediaCapture();

		var availableCameras = new List<CameraInfo>();

		foreach (var sourceGroup in videoCaptureSourceGroup)
		{
			await mediaCapture.InitializeCameraForCameraView(sourceGroup.Id, token);

			CameraPosition position = CameraPosition.Unknown;
			var device = deviceInfoCollection.FirstOrDefault(deviceInfo => deviceInfo.Id == sourceGroup.Id);
			if (device?.EnclosureLocation is not null)
			{
				position = device.EnclosureLocation.Panel switch
				{
					Panel.Front => CameraPosition.Front,
					Panel.Back => CameraPosition.Rear,
					_ => CameraPosition.Unknown
				};
			}

			var mediaEncodingPropertiesList = mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.Photo)
				.OfType<ImageEncodingProperties>().OrderByDescending(p => p.Width * p.Height);

			var supportedResolutionsList = new List<Size>();
			var imageEncodingPropertiesList = new List<ImageEncodingProperties>();

			foreach (var mediaEncodingProperties in mediaEncodingPropertiesList)
			{
				var imageEncodingProperties = mediaEncodingProperties;
				if (supportedResolutionsList.Contains(new(imageEncodingProperties.Width, imageEncodingProperties.Height)))
				{
					continue;
				}
				imageEncodingPropertiesList.Add(imageEncodingProperties);
				supportedResolutionsList.Add(new(imageEncodingProperties.Width, imageEncodingProperties.Height));
			}

			var cameraInfo = new CameraInfo(
				sourceGroup.DisplayName,
				sourceGroup.Id,
				position,
				mediaCapture.VideoDeviceController.FlashControl.Supported,
				mediaCapture.VideoDeviceController.ZoomControl.Supported ? mediaCapture.VideoDeviceController.ZoomControl.Min : 1f,
				mediaCapture.VideoDeviceController.ZoomControl.Supported ? mediaCapture.VideoDeviceController.ZoomControl.Max : 1f,
				supportedResolutionsList,
				sourceGroup,
				imageEncodingPropertiesList);

			availableCameras.Add(cameraInfo);
		}

		AvailableCameras = availableCameras;
	}

}