namespace CommunityToolkit.Maui.Core.Platform;

static partial class StatusBar
{
	static void PlatformSetColor(Color color) => throw new NotSupportedException($"{nameof(PlatformSetColor)} is only supported on net6.0-ios, net6.0-android, net6.0-maccatalyst and net6.0-windows");

	static void PlatformSetStyle(StatusBarStyle statusBarStyle) => throw new NotSupportedException($"{nameof(PlatformSetStyle)} is only supported on net6.0-ios, net6.0-android, net6.0-maccatalyst and net6.0-windows");
}