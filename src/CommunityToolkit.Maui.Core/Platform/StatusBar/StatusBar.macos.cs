using System;
using System.Runtime.Versioning;

namespace CommunityToolkit.Maui.Core.Platform;

[UnsupportedOSPlatform("MacCatalyst"), UnsupportedOSPlatform("MacOS")]
static partial class StatusBar
{
	static void PlatformSetColor(Color color)
	{
		throw new NotSupportedException("MacCatalyst does not currently support changing the macOS status bar color");
	}

	static void PlatformSetStyle(StatusBarStyle style)
	{
		throw new NotSupportedException("MacCatalyst does not currently support changing the macOS status bar color");
	}
}