using Android.Content;
using Android.Content.PM;
using AndroidX.Camera.Core;
using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Core.Views.CameraView;
public static class CameraViewExtensions
{
	public static int ToPlatform(this CameraFlashMode flashMode) => flashMode switch
	{
		CameraFlashMode.Off => ImageCapture.FlashModeOff,
		CameraFlashMode.On => ImageCapture.FlashModeOn,
		CameraFlashMode.Auto => ImageCapture.FlashModeAuto,
		_ => throw new NotImplementedException()
	};

	public static void UpdateAvailability(this IAvailability cameraView, Context context)
	{
		cameraView.IsAvailable = context.PackageManager?.HasSystemFeature(PackageManager.FeatureCamera) ?? false;
	}
}
