namespace CommunityToolkit.Maui.Core.Platform;

static partial class StatusBar
{
	static void PlatformSetColor(Color color) => throw new NotSupportedException($"{nameof(PlatformSetColor)} is only supported on iOS and Android 23 and later");

	static void PlatformSetStyle(StatusBarStyle statusBarStyle) => throw new NotSupportedException($"{nameof(PlatformSetStyle)} is only supported on iOS and Android 23 and later");
}