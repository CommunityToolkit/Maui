namespace CommunityToolkit.Maui.Core.Primitives;

/// <summary>
/// Enumeration of the possible positions that a camera can be placed.
/// </summary>
public enum CameraPosition
{
	/// <summary>
	/// The camera is in an unknown position.
	/// </summary>
	Unknown,

	/// <summary>
	/// The camera is located at the rear of the device.
	/// </summary>
	/// <remarks>
	/// This is typically the better quality camera.
	/// </remarks>
	Rear,

	/// <summary>
	/// The camera is located at the front of the device.
	/// </summary>
	Front
}