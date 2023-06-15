using System.Runtime.Versioning;

namespace CommunityToolkit.Maui.Core.Platform;

[UnsupportedOSPlatform("Windows")]
static partial class StatusBar
{
	static void PlatformSetColor(Color color)
	{
		throw new NotSupportedException("WinUI does not currently support changing the Windows status bar color");
	}

	static void PlatformSetStyle(StatusBarStyle style)
	{
		throw new NotSupportedException("WinUI does not currently support changing the Windows status bar color");
	}
}