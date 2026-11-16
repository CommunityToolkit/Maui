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
	/// <remarks>
	/// Sizes the view that colors the status bar of every window of the app: to the status bar of the window's scene, or, when
	/// <paramref name="isUsingSafeArea"/> is <see langword="true"/>, to the window's top safe area inset. The view keeps that size when the window
	/// rotates or its width changes, also while no StatusBarBehavior is attached to a page: in landscape, where an iPhone hides its status bar, the view
	/// has no height.
	/// </remarks>
	public static void SetBarSize(bool isUsingSafeArea)
	{
		foreach (var window in GetApplicationWindows())
		{
			var statusBarFrame = window.WindowScene?.StatusBarManager?.StatusBarFrame;
			if (statusBarFrame is null)
			{
				continue;
			}

			var statusBar = FindStatusBarView(window) ?? new StatusBarView();
			statusBar.SizeToStatusBar(window, statusBarFrame.Value, isUsingSafeArea);

			ShowInFront(window, statusBar);

			TryUpdateStatusBarAppearance(window);
		}
	}

	static void PlatformSetColor(Color color)
	{
		var uiColor = color.ToPlatform();

		foreach (var window in GetApplicationWindows())
		{
			var statusBarFrame = window.WindowScene?.StatusBarManager?.StatusBarFrame;
			if (statusBarFrame is null)
			{
				continue;
			}

			var statusBar = FindStatusBarView(window);
			if (statusBar is null)
			{
				statusBar = new StatusBarView();
				statusBar.SizeToStatusBar(window, statusBarFrame.Value, false);
			}

			statusBar.BackgroundColor = uiColor;
			statusBar.TintColor = uiColor;

			ShowInFront(window, statusBar);

			TryUpdateStatusBarAppearance(window);
		}
	}

	// Only the app's own windows get a status bar view. UIApplication.Windows also contains
	// system windows (e.g. UITextEffectsWindow, created after a text input gets focus), which sit
	// above the app windows; painting them leaves the status bar on top of any modal or overlay.
	static IReadOnlyList<UIWindow> GetApplicationWindows()
	{
		var applicationWindows = IPlatformApplication.Current?.Application?.Windows
			.Select(static window => window.Handler?.PlatformView)
			.OfType<UIWindow>()
			.ToList();

		return applicationWindows is { Count: > 0 }
			? applicationWindows
			: UIApplication.SharedApplication.Windows;
	}

	// The status bar view is a subview of the window itself, of the toolkit's own type: UIView.ViewWithTag would also find an app view with the same tag
	// inside the window.
	static StatusBarView? FindStatusBarView(UIWindow window) => window.Subviews.OfType<StatusBarView>().FirstOrDefault();

	static void ShowInFront(UIWindow window, StatusBarView statusBar)
	{
		var statusBarSubViews = window.Subviews.Where(static x => x.Tag == StatusBarView.StatusBarTag).ToList();
		foreach (var statusBarSubView in statusBarSubViews)
		{
			statusBarSubView.RemoveFromSuperview();
		}

		window.AddSubview(statusBar);
	}

	// The deprecated UIApplication.StatusBarFrame is CGRect.Null in an app built with the iOS 27 SDK, so the status bar frame of the window's scene is passed in.
	// A hidden status bar, e.g. of an iPhone in landscape, has an empty frame: the view then gets no height and keeps the width of the window, so that the
	// window still resizes it (UIViewAutoresizing.FlexibleWidth) when it rotates back.
	static CGRect GetStatusBarFrame(in UIWindow window, in CGRect statusBarFrame, in bool isUsingSafeArea)
	{
		var frame = statusBarFrame.IsEmpty ? new CGRect(0, 0, window.Bounds.Width, 0) : statusBarFrame;

		return isUsingSafeArea
			? new CGRect(frame.X, frame.Y, frame.Width, window.SafeAreaInsets.Top)
			: frame;
	}

	internal static UIStatusBarStyle ToUIStatusBarStyle(StatusBarStyle statusBarStyle) => statusBarStyle switch
	{
		StatusBarStyle.Default => UIStatusBarStyle.Default,
		StatusBarStyle.LightContent => UIStatusBarStyle.LightContent,
		StatusBarStyle.DarkContent => UIStatusBarStyle.DarkContent,
		_ => throw new NotSupportedException($"{nameof(StatusBarStyle)} {statusBarStyle} is not yet supported on iOS")
	};

	static void UpdateStatusBarAppearanceOfEveryWindow()
	{
		foreach (var window in GetApplicationWindows())
		{
			TryUpdateStatusBarAppearance(window);
		}
	}

	const string viewControllerBasedStatusBarAppearanceKey = "UIViewControllerBasedStatusBarAppearance";

	// The device tests replace it to show what each value of UIViewControllerBasedStatusBarAppearance does.
	internal static Func<NSObject?> ReadViewControllerBasedStatusBarAppearance { get; set; } = static () => NSBundle.MainBundle.ObjectForInfoDictionary(viewControllerBasedStatusBarAppearanceKey);

	// The device tests replace it: UIKit ignores the call in the runner, whose Info.plist keeps view controller-based status bar appearance.
	internal static Func<UIStatusBarStyle, bool> TrySetApplicationStatusBarStyle { get; set; } = static uiStatusBarStyle =>
	{
		UIApplication.SharedApplication.SetStatusBarStyle(uiStatusBarStyle, false);
		UpdateStatusBarAppearanceOfEveryWindow();

		return UIApplication.SharedApplication.Windows.All(window => window.WindowScene?.StatusBarManager is not UIStatusBarManager statusBarManager || statusBarManager.StatusBarStyle == uiStatusBarStyle);
	};

	// A <false/> value is an NSNumber.
	internal static bool IsViewControllerBasedStatusBarAppearanceOff => ReadViewControllerBasedStatusBarAppearance() is NSNumber { BoolValue: false };

	// An app built with the iOS 27 SDK or later ignores UIApplication.SetStatusBarStyle: only view controller-based status bar appearance can change its style
	internal static void SetStyleOfTheWholeApp(StatusBarStyle statusBarStyle)
	{
		if (!TrySetApplicationStatusBarStyle(ToUIStatusBarStyle(statusBarStyle)))
		{
			Trace.WriteLine("Unable to apply the Status Bar Style because Info.plist sets UIViewControllerBasedStatusBarAppearance to false: remove the key from Info.plist");
		}
	}

	// Set by CommunityToolkit.Maui in UseMauiCommunityToolkit: only Microsoft.Maui.Controls.Window can change the theme, and
	// CommunityToolkit.Maui.Core does not reference Microsoft.Maui.Controls.
	internal static Action<StatusBarTheme>? SetStatusBarThemeOfEveryWindow { get; set; }

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

	// StatusBarTheme names the background of the status bar: light content (light text and icons) is the style of a dark one
	internal static StatusBarTheme ToStatusBarTheme(StatusBarStyle statusBarStyle) => statusBarStyle switch
	{
		StatusBarStyle.Default => StatusBarTheme.Default,
		StatusBarStyle.LightContent => StatusBarTheme.Dark,
		StatusBarStyle.DarkContent => StatusBarTheme.Light,
		_ => throw new NotSupportedException($"{nameof(StatusBarStyle)} {statusBarStyle} is not yet supported on iOS")
	};

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

	// A StatusBarBehavior calls SetBarSize only while it is attached to a page that is on screen, so the view sizes itself again when UIKit lays it
	// out after its window, the window's safe area or the status bar of the window's scene changed, e.g. after a rotation.
	sealed class StatusBarView : UIView
	{
		public const int StatusBarTag = 38482;

		bool isUsingSafeArea;
		StatusBarGeometry? sizedFor;

		public StatusBarView()
		{
			Tag = StatusBarTag;
			UserInteractionEnabled = false;

			// The window resizes the view when it rotates, which lays the view out again
			AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
		}

		public void SizeToStatusBar(UIWindow window, CGRect statusBarFrame, bool isUsingSafeArea)
		{
			this.isUsingSafeArea = isUsingSafeArea;
			sizedFor = new StatusBarGeometry(window.Bounds.Size, window.SafeAreaInsets, statusBarFrame);
			Frame = GetStatusBarFrame(window, statusBarFrame, isUsingSafeArea);
		}

		public override void LayoutSubviews()
		{
			base.LayoutSubviews();

			if (Window is UIWindow window
				&& window.WindowScene?.StatusBarManager?.StatusBarFrame is CGRect statusBarFrame
				&& sizedFor != new StatusBarGeometry(window.Bounds.Size, window.SafeAreaInsets, statusBarFrame))
			{
				SizeToStatusBar(window, statusBarFrame, isUsingSafeArea);

				// iOS 26 lays out the window of a rotation before it updates the status bar frame (still empty when an iPhone turns back to portrait),
				// and nothing lays the view out after the update: one more layout once UIKit has finished the current update reads the new frame.
				CoreFoundation.DispatchQueue.MainQueue.DispatchAsync(SetNeedsLayout);
			}
		}
	}

	readonly record struct StatusBarGeometry(CGSize WindowSize, UIEdgeInsets SafeAreaInsets, CGRect StatusBarFrame);
}