using System;
using System.Runtime.Versioning;

namespace CommunityToolkit.Maui.Core.Platform;

[UnsupportedOSPlatform("MacCatalyst"), UnsupportedOSPlatform("MacOS")]
static partial class SystemNavigationBar
{
	static void PlatformSetColor(Color color)
	{
		throw new NotSupportedException("MacCatalyst does not currently support changing the macOS system navigation bar color");
	}

	static void PlatformSetStyle(SystemNavigationBarStyle style)
	{
		throw new NotSupportedException("MacCatalyst does not currently support changing the macOS system navigation bar color");
	}
}