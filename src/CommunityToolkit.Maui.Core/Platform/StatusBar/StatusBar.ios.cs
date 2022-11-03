using System.Runtime.Versioning;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.Core.Platform;

[UnsupportedOSPlatform("MacCatalyst")]
static partial class StatusBar
{
	static void PlatformSetColor(Color color)
	{
		var uiColor = color.ToPlatform();

		if (OperatingSystem.IsIOSVersionAtLeast(13))
		{
			const int statusBarTag = 38482;
			foreach (var window in UIApplication.SharedApplication.Windows)
			{
				var statusBar = window.ViewWithTag(statusBarTag) ?? new UIView(UIApplication.SharedApplication.StatusBarFrame);
				statusBar.Tag = statusBarTag;
				statusBar.BackgroundColor = uiColor;
				statusBar.TintColor = uiColor;
				statusBar.Frame = UIApplication.SharedApplication.StatusBarFrame;
				window.AddSubview(statusBar);

				UpdateStatusBarAppearance(window);
			}
		}
		else
		{
			if (UIApplication.SharedApplication.ValueForKey(new NSString("statusBar")) is UIView statusBar
				&& statusBar.RespondsToSelector(new ObjCRuntime.Selector("setBackgroundColor:")))
			{
				statusBar.BackgroundColor = uiColor;
			}

			UpdateStatusBarAppearance();
		}
	}

	static void PlatformSetStyle(StatusBarStyle statusBarStyle)
	{
		var uiStyle = statusBarStyle switch
		{
			StatusBarStyle.Default => UIStatusBarStyle.Default,
			StatusBarStyle.LightContent => UIStatusBarStyle.LightContent,
			StatusBarStyle.DarkContent => UIStatusBarStyle.DarkContent,
			_ => throw new NotSupportedException($"{nameof(StatusBarStyle)} {statusBarStyle} is not yet supported on iOS")
		};

		UIApplication.SharedApplication.SetStatusBarStyle(uiStyle, false);

		UpdateStatusBarAppearance();
	}


	static void UpdateStatusBarAppearance()
	{
		if (OperatingSystem.IsIOSVersionAtLeast(13))
		{
			foreach (var window in UIApplication.SharedApplication.Windows)
			{
				UpdateStatusBarAppearance(window);
			}
		}
		else
		{
			var window = UIApplication.SharedApplication.KeyWindow;
			UpdateStatusBarAppearance(window);
		}
	}

	static void UpdateStatusBarAppearance(UIWindow? window)
	{
		var vc = window?.RootViewController ?? WindowStateManager.Default.GetCurrentUIViewController() ?? throw new InvalidOperationException($"{nameof(window.RootViewController)} cannot be null");

		while (vc.PresentedViewController is not null)
		{
			vc = vc.PresentedViewController;
		}

		vc.SetNeedsStatusBarAppearanceUpdate();
	}
}