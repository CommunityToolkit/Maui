using System.Text;
using CommunityToolkit.Maui.Core.Primitives;

#if IOS || MACCATALYST
using AVFoundation;
#elif ANDROID
using AndroidX.Camera.Core;
#elif WINDOWS
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
#endif

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Represents information about a camera device.
/// </summary>
public class CameraInfo(
	string name,
	string deviceId,
	CameraPosition cameraPosition,
	bool isFlashSupported,
	float minimumZoomFactor,
	float maximumZoomFactor,
	IEnumerable<Size> supportedResolutions
#if ANDROID
,
	CameraSelector cameraSelector
#elif IOS || MACCATALYST
,
	AVCaptureDevice captureDevice,
	IEnumerable<AVCaptureDeviceFormat> supportedFormats
#elif WINDOWS
,
    MediaFrameSourceGroup frameSourceGroup,
	IEnumerable<ImageEncodingProperties> imageEncodingProperties
#endif
)
{
	/// <summary>
	/// Gets the name of the camera device.
	/// </summary>
	public string Name { get; } = name;

	/// <summary>
	/// Gets the unique identifier of the camera device.
	/// </summary>
	public string DeviceId { get; } = deviceId;

	/// <summary>
	/// Gets the <see cref="CameraPosition"/> of the camera device.
	/// </summary>
	public CameraPosition Position { get; } = cameraPosition;

	/// <summary>
	/// Gets a value indicating whether the camera device supports flash.
	/// </summary>
	public bool IsFlashSupported { get; } = isFlashSupported;

	/// <summary>
	/// Gets the minimum zoom factor supported by the camera device.
	/// </summary>
	public float MinimumZoomFactor { get; } = minimumZoomFactor;

	/// <summary>
	/// Gets the maximum zoom factor supported by the camera device.
	/// </summary>
	public float MaximumZoomFactor { get; } = maximumZoomFactor;

	/// <summary>
	/// Gets the supported resolutions of the camera device.
	/// </summary>
	public IReadOnlyList<Size> SupportedResolutions { get; } = [.. supportedResolutions];

#if ANDROID
	internal CameraSelector? CameraSelector { get; } = cameraSelector;
#endif

#if IOS || MACCATALYST
	internal AVCaptureDevice? CaptureDevice { get; } = captureDevice;
	internal IReadOnlyList<AVCaptureDeviceFormat> SupportedFormats { get; } = [.. supportedFormats];
#endif

#if WINDOWS
    internal MediaFrameSourceGroup FrameSourceGroup { get; } = frameSourceGroup;
	internal IReadOnlyList<ImageEncodingProperties> ImageEncodingProperties { get; } =  [.. imageEncodingProperties];
#endif

	/// <inheritdoc cref="object.ToString" />
	public override string ToString()
	{
		var resolutionStringBuilder = new StringBuilder();

		foreach (var resolutionSize in SupportedResolutions)
		{
			var endString = resolutionSize == SupportedResolutions.Last()
				? "."
				: ", ";

			resolutionStringBuilder.Append($"{resolutionSize.Width}x{resolutionSize.Height}{endString}");
		}

		return $"Camera Info => Name:{Name}, id:{DeviceId}, position:{Position}, hasFlash:{IsFlashSupported}, minZoom:{MinimumZoomFactor}, maxZoom:{MaximumZoomFactor}\nresolutions {resolutionStringBuilder} ";
	}
}