using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.Core.Capabilities;

static partial class StatusBar
{
	static void PlatformSetColor(Color color)
	{
		var uiColor = color.ToPlatform();

		if (PlatformVersion.IsiOS13OrNewer)
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
			StatusBarStyle.LightContent => UIStatusBarStyle.LightContent,
			StatusBarStyle.DarkContent => UIStatusBarStyle.DarkContent,
			_ => UIStatusBarStyle.Default
		};
		UIApplication.SharedApplication.SetStatusBarStyle(uiStyle, false);

		UpdateStatusBarAppearance();
	}


	static void UpdateStatusBarAppearance()
	{
		if (PlatformVersion.IsiOS13OrNewer)
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
		var vc = window?.RootViewController ?? WindowStateManager.Default.GetCurrentUIViewController() ?? throw new NullReferenceException(nameof(window.RootViewController));

		while (vc.PresentedViewController is not null)
		{
			vc = vc.PresentedViewController;
		}

		vc.SetNeedsStatusBarAppearanceUpdate();
	}
}
