namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Specifies the format of the camera frame data.
/// </summary>
public enum CameraFrameFormat
{
	/// <summary>
	/// Unknown format.
	/// </summary>
	Unknown = 0,

	/// <summary>
	/// YUV 420 888 format.
	/// </summary>
	Yuv420 = 1,

	/// <summary>
	/// BGRA 8888 format.
	/// </summary>
	Bgra8888 = 2,

	/// <summary>
	/// RGBA 8888 format.
	/// </summary>
	Rgba8888 = 3,

	/// <summary>
	/// YUV 420 Bi-Planar format (often used on iOS).
	/// </summary>
	Yuv420BiPlanar = 4,

	/// <summary>
	/// NV12 format.
	/// </summary>
	Nv12 = 5,
}
