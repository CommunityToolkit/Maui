namespace CommunityToolkit.Maui.Core.Platform;

static partial class StatusBar
{
	static void PlatformSetColor(Color color) => throw new NotSupportedException($"{nameof(PlatformSetColor)} is only supported on net6.0-ios and net6.0-android");

	static void PlatformSetStyle(StatusBarStyle statusBarStyle) => throw new NotSupportedException($"{nameof(PlatformSetStyle)} is only supported on net6.0-ios and net6.0-android");
}