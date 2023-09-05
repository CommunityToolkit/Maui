using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Core.Views.CameraView;

// TODO: Implement :)
public static class CameraViewExtensions
{
	public static int ToPlatform(this CameraFlashMode flashMode) =>
		flashMode switch
		{
			CameraFlashMode.Off => throw new NotImplementedException(),
			CameraFlashMode.On => throw new NotImplementedException(),
			CameraFlashMode.Auto => throw new NotImplementedException(),
			_ => throw new NotImplementedException(),
		};

	public static void UpdateAvailability(this IAvailability cameraView)
	{
		cameraView.IsAvailable = true;
	}
}