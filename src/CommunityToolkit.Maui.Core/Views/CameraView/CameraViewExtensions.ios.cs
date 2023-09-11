using AVFoundation;
using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Core.Views.CameraView;

public static class CameraViewExtensions
{
	public static AVCaptureFlashMode ToPlatform(this CameraFlashMode flashMode) =>
		flashMode switch
		{
			CameraFlashMode.Off => AVCaptureFlashMode.Off,
			CameraFlashMode.On => AVCaptureFlashMode.On,
			CameraFlashMode.Auto => AVCaptureFlashMode.Auto,
			_ => throw new NotImplementedException(),
		};

	public static void UpdateAvailability(this IAvailability cameraView)
	{
		cameraView.IsAvailable = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video) != null;
	}
}