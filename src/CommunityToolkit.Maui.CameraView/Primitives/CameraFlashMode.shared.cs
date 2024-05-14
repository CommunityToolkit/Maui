namespace CommunityToolkit.Maui.Core.Primitives;

/// <summary>
/// Enumeration of the possible flash modes supported by the camera.
/// </summary>
public enum CameraFlashMode
{
	/// <summary>
	/// Flash is off and will <b>not</b> be used.
	/// </summary>
	Off,

	/// <summary>
	/// Flash is on and will <b>always</b> be used.
	/// </summary>
	On,

	/// <summary>
	/// Flash will automatically be used based on the lighting conditions.
	/// </summary>
	Auto
}