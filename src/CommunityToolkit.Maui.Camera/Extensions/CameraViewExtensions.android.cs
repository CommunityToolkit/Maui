using Android.Content;
using Android.Content.PM;
using AndroidX.Camera.Core;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Extensions;

/// <summary>
/// Extension methods for the CameraView on Android.
/// </summary>
static class CameraViewExtensions
{
	/// <summary>
	/// Converts a <see cref="CameraFlashMode"/> to the platform-specific flash mode.
	/// </summary>
	/// <param name="flashMode">The <see cref="CameraFlashMode"/> to convert.</param>
	/// <returns>The platform-specific flash mode.</returns>
	/// <exception cref="NotSupportedException">When the supplied <paramref name="flashMode"/> is not supported.</exception>
	public static int ToPlatform(this CameraFlashMode flashMode) => flashMode switch
	{
		CameraFlashMode.Off => ImageCapture.FlashModeOff,
		CameraFlashMode.On => ImageCapture.FlashModeOn,
		CameraFlashMode.Auto => ImageCapture.FlashModeAuto,
		_ => throw new NotSupportedException($"Flash mode {flashMode} not supported."),
	};

	/// <summary>
	/// Updates whether the camera feature is available on Android.
	/// </summary>
	/// <param name="cameraView">An <see cref="ICameraView"/> implementation.</param>
	/// <param name="context">The <see cref="Context"/> used to determine whether the environment supports camera access.</param>
	public static void UpdateAvailability(this ICameraView cameraView, Context context)
	{
		cameraView.IsAvailable = context.PackageManager?.HasSystemFeature(PackageManager.FeatureCamera) ?? false;
	}
}