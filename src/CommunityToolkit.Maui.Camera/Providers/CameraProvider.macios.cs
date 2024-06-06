using AVFoundation;
using CommunityToolkit.Maui.Core.Primitives;
using CoreMedia;
using CoreVideo;
using UIKit;

namespace CommunityToolkit.Maui.Core;

partial class CameraProvider
{
	static readonly AVCaptureDeviceType[] captureDevices = InitializeCaptureDevices();

	public partial ValueTask RefreshAvailableCameras(CancellationToken token)
	{
		var discoverySession = AVCaptureDeviceDiscoverySession.Create(captureDevices, AVMediaTypes.Video, AVCaptureDevicePosition.Unspecified);
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

		return ValueTask.CompletedTask;
	}

	static AVCaptureDeviceType[] InitializeCaptureDevices()
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
				AVCaptureDeviceType.BuiltInTrueDepthCamera];
		}

		if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
		{
			deviceTypes = [.. deviceTypes,
				AVCaptureDeviceType.BuiltInUltraWideCamera,
				AVCaptureDeviceType.BuiltInTripleCamera,
				AVCaptureDeviceType.BuiltInDualWideCamera];
		}

		if (UIDevice.CurrentDevice.CheckSystemVersion(15, 4))
		{
			deviceTypes = [.. deviceTypes,
				AVCaptureDeviceType.BuiltInLiDarDepthCamera];
		}

		return deviceTypes;
	}
}