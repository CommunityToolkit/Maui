using AVFoundation;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Extensions;

static class CameraViewExtensions
{
	public static AVCaptureFlashMode ToPlatform(this CameraFlashMode flashMode) => flashMode switch
	{
		CameraFlashMode.Off => AVCaptureFlashMode.Off,
		CameraFlashMode.On => AVCaptureFlashMode.On,
		CameraFlashMode.Auto => AVCaptureFlashMode.Auto,
		_ => throw new NotSupportedException($"{flashMode} is not yet supported"),
	};

	public static void UpdateAvailability(this IAvailability cameraView)
	{
		cameraView.IsAvailable = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video) is not null;
	}
}