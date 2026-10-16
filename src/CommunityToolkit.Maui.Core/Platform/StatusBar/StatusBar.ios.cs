using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.Core.Platform;

[UnsupportedOSPlatform("MacCatalyst")]
static partial class StatusBar
{
	/// <summary>
	/// Update the status bar size.
	/// </summary>
	public static void SetBarSize(bool isUsingSafeArea)
	{
		var communityToolkitStatusBarTag = new IntPtr(38482);
		foreach (var window in UIApplication.SharedApplication.Windows)
		{
			var statusBarFrame = window.WindowScene?.StatusBarManager?.StatusBarFrame;
			if (statusBarFrame is null)
			{
				continue;
			}

			// The status bar view is a subview of the window itself: UIView.ViewWithTag would also find an app view with the same tag inside the window
			var statusBar = window.Subviews.FirstOrDefault(x => x.Tag == communityToolkitStatusBarTag) ?? new UIView(statusBarFrame.Value);
			statusBar.Tag = communityToolkitStatusBarTag;
			statusBar.Frame = GetStatusBarFrame(window, statusBarFrame.Value, isUsingSafeArea);

			var statusBarSubViews = window.Subviews.Where(x => x.Tag == communityToolkitStatusBarTag).ToList();
			foreach (var statusBarSubView in statusBarSubViews)
			{
				statusBarSubView.RemoveFromSuperview();
			}

			window.AddSubview(statusBar);

			TryUpdateStatusBarAppearance(window);
		}
	}

	static void PlatformSetColor(Color color)
	{
		var uiColor = color.ToPlatform();

		var statusBarTag = new IntPtr(38482);
		foreach (var window in UIApplication.SharedApplication.Windows)
		{
			// The status bar view is a subview of the window itself: UIView.ViewWithTag would also find an app view with the same tag inside the window
			var statusBar = window.Subviews.FirstOrDefault(x => x.Tag == statusBarTag);
			var statusBarFrame = window.WindowScene?.StatusBarManager?.StatusBarFrame;
			if (statusBarFrame is null)
			{
				continue;
			}

			statusBar ??= new UIView(statusBarFrame.Value);
			statusBar.Tag = statusBarTag;
			statusBar.BackgroundColor = uiColor;
			statusBar.TintColor = uiColor;

			var statusBarSubViews = window.Subviews.Where(x => x.Tag == statusBarTag).ToList();
			foreach (var statusBarSubView in statusBarSubViews)
			{
				statusBarSubView.RemoveFromSuperview();
			}

			window.AddSubview(statusBar);

			TryUpdateStatusBarAppearance(window);
		}
	}

	// The deprecated UIApplication.StatusBarFrame is CGRect.Null in an app built with the iOS 27 SDK, so the status bar frame of the window's scene is passed in.
	static CGRect GetStatusBarFrame(in UIWindow window, in CGRect statusBarFrame, in bool isUsingSafeArea)
	{
		return isUsingSafeArea
			? new CGRect(statusBarFrame.X, statusBarFrame.Y, statusBarFrame.Width, window.SafeAreaInsets.Top)
			: statusBarFrame;
	}

	internal static UIStatusBarStyle ToUIStatusBarStyle(StatusBarStyle statusBarStyle) => statusBarStyle switch
	{
		StatusBarStyle.Default => UIStatusBarStyle.Default,
		StatusBarStyle.LightContent => UIStatusBarStyle.LightContent,
		StatusBarStyle.DarkContent => UIStatusBarStyle.DarkContent,
		_ => throw new NotSupportedException($"{nameof(StatusBarStyle)} {statusBarStyle} is not yet supported on iOS")
	};

	static bool TryUpdateStatusBarAppearance()
	{
		var didUpdateAllStatusBars = true;

		foreach (var window in UIApplication.SharedApplication.Windows)
		{
			didUpdateAllStatusBars &= TryUpdateStatusBarAppearance(window);
		}

		return didUpdateAllStatusBars;
	}

#if NET11_0_OR_GREATER
	const string viewControllerBasedStatusBarAppearanceKey = "UIViewControllerBasedStatusBarAppearance";

	// Set by CommunityToolkit.Maui in UseMauiCommunityToolkit: only Microsoft.Maui.Controls.Window can change the theme, and
	// CommunityToolkit.Maui.Core does not reference Microsoft.Maui.Controls.
	internal static Action<StatusBarTheme>? SetStatusBarThemeOfEveryWindow { get; set; }

	// The device tests replace it to show what each value of UIViewControllerBasedStatusBarAppearance does.
	internal static Func<NSObject?> ReadViewControllerBasedStatusBarAppearance { get; set; } = static () => NSBundle.MainBundle.ObjectForInfoDictionary(viewControllerBasedStatusBarAppearanceKey);

	// The device tests replace it: UIKit ignores the call in the runner, whose Info.plist keeps view controller-based status bar appearance.
	internal static Func<UIStatusBarStyle, bool> TrySetApplicationStatusBarStyle { get; set; } = static uiStatusBarStyle =>
	{
		UIApplication.SharedApplication.SetStatusBarStyle(uiStatusBarStyle, false);
		TryUpdateStatusBarAppearance();

		return UIApplication.SharedApplication.Windows.All(window => window.WindowScene?.StatusBarManager is not UIStatusBarManager statusBarManager || statusBarManager.StatusBarStyle == uiStatusBarStyle);
	};

	// A <false/> value is an NSNumber. False is the setup that StatusBarBehavior documents for .NET 10.
	internal static bool IsViewControllerBasedStatusBarAppearanceOff => ReadViewControllerBasedStatusBarAppearance() is NSNumber { BoolValue: false };

	// The view controllers of .NET MAUI return the style of their window's StatusBarTheme. UIKit takes the style from them unless Info.plist turns
	// view controller-based status bar appearance off: then it shows the style that UIApplication.SetStatusBarStyle sets for the whole app.
	static void PlatformSetStyle(StatusBarStyle statusBarStyle)
	{
		var statusBarTheme = ToStatusBarTheme(statusBarStyle);

		if (IsViewControllerBasedStatusBarAppearanceOff)
		{
			SetStyleOfTheWholeApp(statusBarStyle);
		}
		else if (SetStatusBarThemeOfEveryWindow is null)
		{
			Trace.WriteLine("Unable to set the Status Bar Style because the windows of the app are unknown: call UseMauiCommunityToolkit() in MauiProgram.cs");
		}

		SetStatusBarThemeOfEveryWindow?.Invoke(statusBarTheme);
	}

	// An app built with the iOS 27 SDK or later ignores UIApplication.SetStatusBarStyle: only view controller-based status bar appearance can change its style
	internal static void SetStyleOfTheWholeApp(StatusBarStyle statusBarStyle)
	{
		if (!TrySetApplicationStatusBarStyle(ToUIStatusBarStyle(statusBarStyle)))
		{
			Trace.WriteLine("Unable to apply the Status Bar Style because Info.plist sets UIViewControllerBasedStatusBarAppearance to false: remove the key from Info.plist");
		}
	}

	// StatusBarTheme names the background of the status bar: light content (light text and icons) is the style of a dark one
	internal static StatusBarTheme ToStatusBarTheme(StatusBarStyle statusBarStyle) => statusBarStyle switch
	{
		StatusBarStyle.Default => StatusBarTheme.Default,
		StatusBarStyle.LightContent => StatusBarTheme.Dark,
		StatusBarStyle.DarkContent => StatusBarTheme.Light,
		_ => throw new NotSupportedException($"{nameof(StatusBarStyle)} {statusBarStyle} is not yet supported on iOS")
	};
#else
	static void PlatformSetStyle(StatusBarStyle statusBarStyle)
	{
		UIApplication.SharedApplication.SetStatusBarStyle(ToUIStatusBarStyle(statusBarStyle), false);

		TryUpdateStatusBarAppearance();
	}
#endif

	static bool TryUpdateStatusBarAppearance(UIWindow? window)
	{
		var vc = window?.RootViewController ?? WindowStateManager.Default.GetCurrentUIViewController();

		if (vc is null)
		{
			Trace.WriteLine("Unable to update Status Bar Appearance because Current UIViewController is null");
			return false;
		}

		while (vc.PresentedViewController is not null)
		{
			vc = vc.PresentedViewController;
		}

		vc.SetNeedsStatusBarAppearanceUpdate();

		return true;
	}
}