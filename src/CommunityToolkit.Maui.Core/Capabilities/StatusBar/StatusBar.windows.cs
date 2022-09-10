using Microsoft.Maui.Platform;
using Windows.UI.ViewManagement;

namespace CommunityToolkit.Maui.Core.Capabilities;
static partial class StatusBar
{
	static void PlatformSetColor(Color color)
	{
		throw new NotSupportedException("WinUI does not currently support changing the Windows status bar color");
	}

	static void PlatformSetStyle(StatusBarStyle style)
	{
		throw new NotImplementedException();
	}
}
