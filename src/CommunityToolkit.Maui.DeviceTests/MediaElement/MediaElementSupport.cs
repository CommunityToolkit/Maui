namespace CommunityToolkit.Maui.DeviceTests;

// The app's Android minimum is 24 (the libraries' lowest), but MediaElement needs 26 (it is [SupportedOSPlatform("Android26.0")]).
static class MediaElementSupport
{
	public const string UnsupportedReason = "MediaElement needs Android 8.0 (API 26).";

	public static bool IsSupported => !OperatingSystem.IsAndroid() || OperatingSystem.IsAndroidVersionAtLeast(26);

	public static void SkipIfUnsupported() => Skip.IfNot(IsSupported, UnsupportedReason);
}
