namespace CommunityToolkit.Maui.Core.Capabilities;

/// <summary>
/// Helper class that stores the platform version.
/// For internal use only
/// </summary>
public static class PlatformVersion
{
	static bool? isiOS13OrNewer;

	/// <summary>
	/// Verify if the iOS version running in the platform is 13 or newer
	/// </summary>
	public static bool IsiOS13OrNewer => isiOS13OrNewer ??= UIDevice.CurrentDevice.CheckSystemVersion(13, 0);
}