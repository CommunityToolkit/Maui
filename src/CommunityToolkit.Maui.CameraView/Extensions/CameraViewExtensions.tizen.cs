using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Extensions;

static class CameraViewExtensions
{
	public static int ToPlatform(this CameraFlashMode flashMode) => throw new NotSupportedException();

	public static void UpdateAvailability(this IAvailability cameraView, Context context) => throw new NotSupportedException();
}