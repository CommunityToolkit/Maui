using System.Runtime.Versioning;

namespace CommunityToolkit.Maui.Core.Platform;

[UnsupportedOSPlatform("iOS")]
static partial class SystemNavigationBar
{
	static void PlatformSetColor(Color color)
	{
		throw new NotSupportedException("iOS does not currently support changing the iOS system navigation bar color");
	}

	static void PlatformSetStyle(SystemNavigationBarStyle style)
	{
		throw new NotSupportedException("iOS does not currently support changing the iOS system navigation bar color");
	}
}