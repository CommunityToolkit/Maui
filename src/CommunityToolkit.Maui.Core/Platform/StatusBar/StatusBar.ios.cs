using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Platform;
using ObjCRuntime;

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

	const string viewControllerBasedStatusBarAppearanceKey = "UIViewControllerBasedStatusBarAppearance";

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

	// An app built with the iOS 27 SDK or later ignores UIApplication.SetStatusBarStyle: only view controller-based status bar appearance can change its style
	internal static void SetStyleOfTheWholeApp(StatusBarStyle statusBarStyle)
	{
		if (!TrySetApplicationStatusBarStyle(ToUIStatusBarStyle(statusBarStyle)))
		{
			Trace.WriteLine("Unable to apply the Status Bar Style because Info.plist sets UIViewControllerBasedStatusBarAppearance to false: remove the key from Info.plist");
		}
	}

#if NET11_0_OR_GREATER
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
#else
	// While no style is requested (SetStyle(Default)), every view controller returns its own style.
	const nint noRequestedStatusBarStyle = -1;

	static readonly Lock preferredStatusBarStyleLock = new();

	// One implementation per class: each calls the implementation that it replaced in its own class, so a call to the implementation of the
	// superclass ([super preferredStatusBarStyle]) never comes back to the same one.
	static readonly unsafe IntPtr toolkitViewControllerImplementation = (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, nint>)&ViewControllerPreferredStatusBarStyle;
	static readonly unsafe IntPtr toolkitNavigationControllerImplementation = (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, nint>)&NavigationControllerPreferredStatusBarStyle;
	static readonly unsafe IntPtr toolkitTabBarControllerImplementation = (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, nint>)&TabBarControllerPreferredStatusBarStyle;
	static readonly unsafe IntPtr toolkitSplitViewControllerImplementation = (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, nint>)&SplitViewControllerPreferredStatusBarStyle;

	static nint requestedStatusBarStyle = noRequestedStatusBarStyle;
	static bool isPreferredStatusBarStyleReplaced;

	// IntPtr.Zero for a class whose implementation the toolkit did not replace.
	static IntPtr replacedViewControllerImplementation, replacedNavigationControllerImplementation, replacedTabBarControllerImplementation, replacedSplitViewControllerImplementation;

	// Unless Info.plist turns view controller-based status bar appearance off, UIKit takes the style from the view controllers, and those of .NET MAUI 10
	// choose none (an app built with the iOS 27 SDK also ignores UIApplication.SetStatusBarStyle). So the first LightContent or DarkContent replaces the
	// implementation of -preferredStatusBarStyle (public Objective-C runtime API) of UIViewController and of the UIKit containers that have their own;
	// view controllers that override preferredStatusBarStyle keep their style.
	static void PlatformSetStyle(StatusBarStyle statusBarStyle)
	{
		// UIKit asks the view controllers for their style on the main thread, and .NET for iOS does not check the thread of UIKit calls in a Release build.
		if (!NSThread.IsMain)
		{
			throw new UIKitThreadAccessException();
		}

		var uiStatusBarStyle = ToUIStatusBarStyle(statusBarStyle);

		// UIKit ignores the style of the view controllers then, so they are left alone.
		if (IsViewControllerBasedStatusBarAppearanceOff)
		{
			SetStyleOfTheWholeApp(statusBarStyle);
			return;
		}

		Volatile.Write(ref requestedStatusBarStyle, statusBarStyle is StatusBarStyle.Default ? noRequestedStatusBarStyle : (nint)uiStatusBarStyle);
		if (statusBarStyle is not StatusBarStyle.Default)
		{
			ReplacePreferredStatusBarStyle();
		}

		TryUpdateStatusBarAppearance();
	}

	// On iOS 27 UINavigationController has its own implementation; UITabBarController and UISplitViewController are replaced too in case an iOS
	// version gives them one. The lock makes a second call, also one on another thread at the same time, change nothing.
	static void ReplacePreferredStatusBarStyle()
	{
		lock (preferredStatusBarStyleLock)
		{
			if (isPreferredStatusBarStyleReplaced)
			{
				return;
			}

			isPreferredStatusBarStyleReplaced = true;

			var selector = Selector.GetHandle("preferredStatusBarStyle");
			var viewControllerMethod = GetInstanceMethod("UIViewController", selector);
			if (viewControllerMethod == IntPtr.Zero)
			{
				return;
			}

			ReplaceImplementation(viewControllerMethod, toolkitViewControllerImplementation, ref replacedViewControllerImplementation);
			ReplaceOwnImplementation("UINavigationController", selector, viewControllerMethod, toolkitNavigationControllerImplementation, ref replacedNavigationControllerImplementation);
			ReplaceOwnImplementation("UITabBarController", selector, viewControllerMethod, toolkitTabBarControllerImplementation, ref replacedTabBarControllerImplementation);
			ReplaceOwnImplementation("UISplitViewController", selector, viewControllerMethod, toolkitSplitViewControllerImplementation, ref replacedSplitViewControllerImplementation);
		}
	}

	// A class that has no implementation of its own has the method of its superclass, and UIViewController's is replaced already.
	static void ReplaceOwnImplementation(string className, IntPtr selector, IntPtr viewControllerMethod, IntPtr implementation, ref IntPtr replacedImplementation)
	{
		var method = GetInstanceMethod(className, selector);
		if (method != viewControllerMethod)
		{
			ReplaceImplementation(method, implementation, ref replacedImplementation);
		}
	}

	// Never replaces one of the toolkit's own implementations: the toolkit would call itself.
	static void ReplaceImplementation(IntPtr method, IntPtr implementation, ref IntPtr replacedImplementation)
	{
		if (method == IntPtr.Zero)
		{
			return;
		}

		var currentImplementation = method_getImplementation(method);
		if (currentImplementation == IntPtr.Zero || IsToolkitImplementation(currentImplementation))
		{
			return;
		}

		Volatile.Write(ref replacedImplementation, currentImplementation);
		method_setImplementation(method, implementation);
	}

	static bool IsToolkitImplementation(IntPtr implementation) =>
		implementation == toolkitViewControllerImplementation
		|| implementation == toolkitNavigationControllerImplementation
		|| implementation == toolkitTabBarControllerImplementation
		|| implementation == toolkitSplitViewControllerImplementation;

	static IntPtr GetInstanceMethod(string className, IntPtr selector)
	{
		IntPtr objectiveCClass = Class.GetHandle(className);
		return objectiveCClass == IntPtr.Zero ? IntPtr.Zero : class_getInstanceMethod(objectiveCClass, selector);
	}

	[UnmanagedCallersOnly]
	static nint ViewControllerPreferredStatusBarStyle(IntPtr self, IntPtr selector) =>
		PreferredStatusBarStyle(self, selector, Volatile.Read(ref replacedViewControllerImplementation));

	[UnmanagedCallersOnly]
	static nint NavigationControllerPreferredStatusBarStyle(IntPtr self, IntPtr selector) =>
		PreferredStatusBarStyle(self, selector, Volatile.Read(ref replacedNavigationControllerImplementation));

	[UnmanagedCallersOnly]
	static nint TabBarControllerPreferredStatusBarStyle(IntPtr self, IntPtr selector) =>
		PreferredStatusBarStyle(self, selector, Volatile.Read(ref replacedTabBarControllerImplementation));

	[UnmanagedCallersOnly]
	static nint SplitViewControllerPreferredStatusBarStyle(IntPtr self, IntPtr selector) =>
		PreferredStatusBarStyle(self, selector, Volatile.Read(ref replacedSplitViewControllerImplementation));

	static unsafe nint PreferredStatusBarStyle(IntPtr self, IntPtr selector, IntPtr replacedImplementation)
	{
		var statusBarStyle = Volatile.Read(ref requestedStatusBarStyle);
		if (statusBarStyle != noRequestedStatusBarStyle)
		{
			return statusBarStyle;
		}

		return replacedImplementation == IntPtr.Zero
			? (nint)UIStatusBarStyle.Default
			: ((delegate* unmanaged<IntPtr, IntPtr, nint>)replacedImplementation)(self, selector);
	}

	[LibraryImport(ObjCRuntime.Constants.ObjectiveCLibrary)]
	private static partial IntPtr class_getInstanceMethod(IntPtr cls, IntPtr name);

	[LibraryImport(ObjCRuntime.Constants.ObjectiveCLibrary)]
	private static partial IntPtr method_getImplementation(IntPtr method);

	[LibraryImport(ObjCRuntime.Constants.ObjectiveCLibrary)]
	private static partial IntPtr method_setImplementation(IntPtr method, IntPtr implementation);
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