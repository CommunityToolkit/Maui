using AVFoundation;
using CoreMedia;
using CoreVideo;
using UIKit;
using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Core;

public partial class CameraProvider
{

    public partial void RefreshAvailableCameras()
    {
        AvailableCameras.Clear();

		AVCaptureDeviceType[] deviceTypes = 
			[
				AVCaptureDeviceType.BuiltInWideAngleCamera, 
				AVCaptureDeviceType.BuiltInTelephotoCamera,
				AVCaptureDeviceType.BuiltInDualCamera 
			];

		if (UIDevice.CurrentDevice.CheckSystemVersion(11, 1))
		{
			deviceTypes = [.. deviceTypes, 
				AVCaptureDeviceType.BuiltInTrueDepthCamera ];
		}

		if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
		{
			deviceTypes = [ .. deviceTypes,
				AVCaptureDeviceType.BuiltInUltraWideCamera, 
				AVCaptureDeviceType.BuiltInTripleCamera, 
				AVCaptureDeviceType.BuiltInDualWideCamera ];
		}

		if (UIDevice.CurrentDevice.CheckSystemVersion(15, 4))
		{
			deviceTypes = [.. deviceTypes, 
				AVCaptureDeviceType.BuiltInLiDarDepthCamera ];
		}

		var discoverySession = AVCaptureDeviceDiscoverySession.Create(deviceTypes, AVMediaTypes.Video, AVCaptureDevicePosition.Unspecified);

        foreach (var device in discoverySession.Devices)
        {
            var cameraInfo = new CameraInfo();

            cameraInfo.DeviceId = device.UniqueID;
            cameraInfo.Name = device.LocalizedName;

            cameraInfo.Position = device.Position switch
            {
                AVCaptureDevicePosition.Front => CameraPosition.Front,
                AVCaptureDevicePosition.Back => CameraPosition.Rear,
                _ => CameraPosition.Unknown
            };

            cameraInfo.IsFlashSupported = device.HasFlash;
            cameraInfo.MinZoomFactor = (float)device.MinAvailableVideoZoomFactor;
            cameraInfo.MaxZoomFactor = (float)device.MaxAvailableVideoZoomFactor;
            cameraInfo.CaptureDevice = device;

            foreach (var format in device.Formats)
            {
                var dimension = ((CMVideoFormatDescription)format.FormatDescription).Dimensions;

                if (((int)format.FormatDescription.VideoCodecType) == ((int)CVPixelFormatType.CV420YpCbCr8BiPlanarVideoRange))
                {
                    continue;
                }

                if (cameraInfo.SupportedResolutions.Contains(new(dimension.Width, dimension.Height)))
                {
                    continue;
                }

                cameraInfo.formats.Add(format);
                cameraInfo.SupportedResolutions.Add(new(dimension.Width, dimension.Height));
            }

            AvailableCameras.Add(cameraInfo);
        }
    }

}
