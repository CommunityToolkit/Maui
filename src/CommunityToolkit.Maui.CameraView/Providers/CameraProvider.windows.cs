using Windows.Devices.Enumeration;
using Windows.Media.Capture.Frames;
using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Core;

public partial class CameraProvider
{

    public partial void RefreshAvailableCameras()
    {
        AvailableCameras.Clear();

        var deviceInfoCollection = DeviceInformation.FindAllAsync(DeviceClass.VideoCapture).GetAwaiter().GetResult();
        var mediaFrameSourceGroup = MediaFrameSourceGroup.FindAllAsync().GetAwaiter().GetResult();
        var videoCaptureSourceGroup = mediaFrameSourceGroup.Where(sourceGroup => deviceInfoCollection.Any(deviceInfo => deviceInfo.Id == sourceGroup.Id)).ToList();

        foreach (var sourceGroup in videoCaptureSourceGroup)
        {
            CameraPosition position = CameraPosition.Unknown;
            var device = deviceInfoCollection.FirstOrDefault(deviceInfo => deviceInfo.Id == sourceGroup.Id);
            if (device is not null)
            {
				position = device.EnclosureLocation.Panel switch
				{
					Panel.Front => CameraPosition.Front,
					Panel.Back => CameraPosition.Rear,
					_ => CameraPosition.Unknown
				};
            }

            var camInfo = new CameraInfo
            {
                Name = sourceGroup.DisplayName,
                DeviceId = sourceGroup.Id,
                Position = position,
                FrameSourceGroup = sourceGroup
            };

            AvailableCameras.Add(camInfo);
        }
    }

}
