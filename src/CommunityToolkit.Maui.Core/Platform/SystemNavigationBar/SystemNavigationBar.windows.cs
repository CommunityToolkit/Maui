using System.Runtime.Versioning;

namespace CommunityToolkit.Maui.Core.Platform;

[UnsupportedOSPlatform("Windows")]
static partial class SystemNavigationBar
{
	static void PlatformSetColor(Color color)
	{
		throw new NotSupportedException("WinUI does not currently support changing the Windows system navigation bar color");
	}

	static void PlatformSetStyle(SystemNavigationBarStyle style)
	{
		throw new NotSupportedException("WinUI does not currently support changing the Windows system navigation bar color");
	}
}