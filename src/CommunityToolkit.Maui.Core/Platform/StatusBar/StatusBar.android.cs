using System.Runtime.Versioning;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using CommunityToolkit.Maui.Core.Extensions;
using Microsoft.Maui.Platform;
using Activity = Android.App.Activity;
using DialogFragment = AndroidX.Fragment.App.DialogFragment;
using PlatformColor = Android.Graphics.Color;

namespace CommunityToolkit.Maui.Core.Platform;

[SupportedOSPlatform("Android23.0")] // StatusBar is only supported on Android 23.0+
static partial class StatusBar
{
	static readonly Lazy<bool> isSupportedHolder = new(() =>
	{
		if (OperatingSystem.IsAndroidVersionAtLeast((int)BuildVersionCodes.M))
		{
			return true;
		}

		System.Diagnostics.Trace.WriteLine($"{nameof(StatusBar)} Color + Style functionality is not supported on this version of the Android operating system. Minimum supported Android API is {BuildVersionCodes.M}");

		return false;
	});

	static Activity Activity => Microsoft.Maui.ApplicationModel.Platform.CurrentActivity ?? throw new InvalidOperationException("Android Activity can't be null.");

	static bool IsSupported => isSupportedHolder.Value;

	static void PlatformSetColor(Color color)
	{
		if (!IsSupported)
		{
			return;
		}

		if (GetCurrentWindow() is not Window { DecorView.RootView: not null } window)
		{
			return;
		}

		var platformColor = color.ToPlatform();

		if (OperatingSystem.IsAndroidVersionAtLeast(35))
		{
			const string statusBarOverlayTag = "StatusBarOverlay";

			var decorGroup = (ViewGroup)window.DecorView.RootView;
			var statusBarOverlay = decorGroup.FindViewWithTag(statusBarOverlayTag);

			if (statusBarOverlay is null)
			{
				var statusBarHeight = Activity.Resources?.GetIdentifier("status_bar_height", "dimen", "android") ?? 0;
				var statusBarPixelSize = statusBarHeight > 0 ? Activity.Resources?.GetDimensionPixelSize(statusBarHeight) ?? 0 : 0;

				statusBarOverlay = new(Activity)
				{
					LayoutParameters = new FrameLayout.LayoutParams(Android.Views.ViewGroup.LayoutParams.MatchParent, statusBarPixelSize + 3)
					{
						Gravity = GravityFlags.Top
					}
				};

				statusBarOverlay.Tag = statusBarOverlayTag;
				decorGroup.AddView(statusBarOverlay);
				statusBarOverlay.SetZ(0);
			}

			statusBarOverlay.SetBackgroundColor(platformColor);
		}
		else
		{
			window.SetStatusBarColor(platformColor);
		}

		bool isColorTransparent = platformColor == PlatformColor.Transparent;
		if (isColorTransparent)
		{
			window.ClearFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
			window.SetFlags(WindowManagerFlags.LayoutNoLimits, WindowManagerFlags.LayoutNoLimits);
		}
		else
		{
			window.ClearFlags(WindowManagerFlags.LayoutNoLimits);
			window.SetFlags(WindowManagerFlags.DrawsSystemBarBackgrounds, WindowManagerFlags.DrawsSystemBarBackgrounds);
		}

		WindowCompat.SetDecorFitsSystemWindows(window, !isColorTransparent);
	}

	static void PlatformSetStyle(StatusBarStyle style)
	{
		if (!IsSupported)
		{
			return;
		}

		switch (style)
		{
			case StatusBarStyle.DarkContent:
				SetStatusBarAppearance(true);
				break;

			case StatusBarStyle.Default:
			case StatusBarStyle.LightContent:
				SetStatusBarAppearance(false);
				break;

			default:
				throw new NotSupportedException($"{nameof(StatusBarStyle)} {style} is not yet supported on Android");
		}
	}

	static void SetStatusBarAppearance(bool isLightStatusBars)
	{
		if (GetCurrentWindow() is Window window
		    && WindowCompat.GetInsetsController(window, window.DecorView) is WindowInsetsControllerCompat windowController)
		{
			windowController.AppearanceLightStatusBars = isLightStatusBars;
		}
	}

	// Check if a modal DialogFragment is active and use its window
	static Window? GetCurrentWindow()
	{
		var fragmentManager = Activity.GetFragmentManager();
		if (fragmentManager is null || !fragmentManager.Fragments.OfType<DialogFragment>().Any())
		{
			return Activity.Window; // Fall back to the Activity window for non-modal pages
		}

		var fragments = fragmentManager.Fragments;
		for (var i = fragments.Count - 1; i >= 0; i--)
		{
			if (fragments[i] is DialogFragment { Dialog: { IsShowing: true, Window: not null }, IsVisible: true } dialogFragment)
			{
				return dialogFragment.Dialog.Window;
			}
		}

		return Activity.Window; // Fall back to the Activity window for non-modal pages
	}
}