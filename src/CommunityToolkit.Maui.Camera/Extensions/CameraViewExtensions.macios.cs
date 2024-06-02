using AVFoundation;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Extensions;

/// <summary>
/// Extension methods for the CameraView on iOS/macOS.
/// </summary>
static class CameraViewExtensions
{
	/// <summary>
	/// Converts a <see cref="CameraFlashMode"/> to the platform-specific flash mode.
	/// </summary>
	/// <param name="flashMode">The <see cref="CameraFlashMode"/> to convert.</param>
	/// <returns>The platform-specific flash mode.</returns>
	/// <exception cref="NotSupportedException">When the supplied <paramref name="flashMode"/> is not supported.</exception>
	public static AVCaptureFlashMode ToPlatform(this CameraFlashMode flashMode) => flashMode switch
	{
		CameraFlashMode.Off => AVCaptureFlashMode.Off,
		CameraFlashMode.On => AVCaptureFlashMode.On,
		CameraFlashMode.Auto => AVCaptureFlashMode.Auto,
		_ => throw new NotSupportedException($"{flashMode} is not yet supported"),
	};

	/// <summary>
	/// Updates whether the camera feature is available on iOS/macOS.
	/// </summary>
	/// <param name="cameraView">An <see cref="ICameraView"/> implementation.</param>
	public static void UpdateAvailability(this ICameraView cameraView)
	{
		cameraView.IsAvailable = AVCaptureDevice.GetDefaultDevice(AVMediaTypes.Video) is not null;
	}
}