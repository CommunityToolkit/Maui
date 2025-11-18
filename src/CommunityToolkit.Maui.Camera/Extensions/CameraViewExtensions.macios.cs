using AVFoundation;
using CommunityToolkit.Maui.Core;
using CoreMedia;

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

	extension(AVCaptureDeviceFormat avCaptureDeviceFormat)
	{
		/// <summary>
		/// Gets the total resolution area in pixels (width × height) of the <see cref="AVCaptureDeviceFormat"/>.
		/// </summary>
		/// <value>
		/// The total number of pixels, calculated as width multiplied by height.
		/// </value>
		public int ResolutionArea
		{
			get
			{
				var dimensions = ((CMVideoFormatDescription)avCaptureDeviceFormat.FormatDescription).Dimensions;
				return dimensions.Width * dimensions.Height;
			}
		}

	}
}