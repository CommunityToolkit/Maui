using Android.OS;

namespace CommunityToolkit.Maui.Core.Capabilities;

/// <summary>
/// Helper class that stores the platform version.
/// For internal use only.
/// </summary>
public static class PlatformVersion
{
	static int? sdkInt;

	/// <summary>
	/// Android version running on the device
	/// </summary>
	public static int SdkInt => sdkInt ??= (int)Build.VERSION.SdkInt;
}