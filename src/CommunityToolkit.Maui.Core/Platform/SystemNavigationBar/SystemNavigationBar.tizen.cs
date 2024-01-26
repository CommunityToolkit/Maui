using System.Runtime.Versioning;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.Core.Platform;

[UnsupportedOSPlatform("Tizen")]
static partial class SystemNavigationBar
{
	static void PlatformSetColor(Color color)
	{
		throw new NotSupportedException("Tizen does not currently support changing the system navigation bar color");
	}

	static void PlatformSetStyle(SystemNavigationBarStyle style)
	{
		throw new NotSupportedException("Tizen does not currently support changing the system navigation bar color");
	}
}