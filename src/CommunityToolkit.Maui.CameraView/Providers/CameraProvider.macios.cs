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
		var availableCameras = new List<CameraInfo>();
		
        foreach (var device in discoverySession.Devices)
        {
            var position = device.Position switch
            {
                AVCaptureDevicePosition.Front => CameraPosition.Front,
                AVCaptureDevicePosition.Back => CameraPosition.Rear,
				AVCaptureDevicePosition.Unspecified => CameraPosition.Unknown,
                _ => throw new NotSupportedException($"{device.Position} is not yet supported")
            };

			var supportedFormats = new List<AVCaptureDeviceFormat>();
			var supportedResolutions = new List<Size>();

            foreach (var format in device.Formats)
            {
                var dimension = ((CMVideoFormatDescription)format.FormatDescription).Dimensions;

                if ((int)format.FormatDescription.VideoCodecType == (int)CVPixelFormatType.CV420YpCbCr8BiPlanarVideoRange)
                {
                    continue;
                }

                if (supportedResolutions.Contains(new(dimension.Width, dimension.Height)))
                {
                    continue;
                }

				supportedFormats.Add(format);
				supportedResolutions.Add(new(dimension.Width, dimension.Height));
            }
			
			var cameraInfo = new CameraInfo(device.LocalizedName, 
				device.UniqueID, 
				position, 
				device.HasFlash,
				(float)device.MinAvailableVideoZoomFactor,
				(float)device.MaxAvailableVideoZoomFactor,
				supportedResolutions,
				device,
				supportedFormats
			);

			availableCameras.Add(cameraInfo);
        }

		AvailableCameras = availableCameras;
	}

}
