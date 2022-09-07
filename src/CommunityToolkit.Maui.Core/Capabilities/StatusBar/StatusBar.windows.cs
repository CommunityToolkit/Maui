using Microsoft.Maui.Platform;
using Windows.UI.ViewManagement;

namespace CommunityToolkit.Maui.Core.Capabilities;
static partial class StatusBar
{
	static void PlatformSetColor(Color color)
	{
		var windowsColor = color.ToWindowsColor();
		UpdateStatusBar(tb => tb.BackgroundColor = windowsColor);
	}

	static void PlatformSetStyle(StatusBarStyle style)
	{
		var foregroundColor = style switch
		{
			StatusBarStyle.LightContent => Colors.White.ToWindowsColor(),
			StatusBarStyle.DarkContent => Colors.Black.ToWindowsColor(),
			_ => Colors.Transparent.ToWindowsColor(),
		};

		UpdateStatusBar(tb => tb.ForegroundColor = foregroundColor);
	}

	static void UpdateStatusBar(Action<ApplicationViewTitleBar> updateTitleBar)
	{
		
		var view = ApplicationView.GetForCurrentView();
		var titleBar = ApplicationView.GetForCurrentView()?.TitleBar;
		if (titleBar is not null)
		{
			updateTitleBar(titleBar);
		}
	}
}
