using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using CommunityToolkit.Maui.Core.Extensions;
using Microsoft.Maui.Platform;
using Activity = Android.App.Activity;
using PlatformColor = Android.Graphics.Color;

namespace CommunityToolkit.Maui.Core.Platform;

[SupportedOSPlatform("Android23.0")] // StatusBar is only supported on Android 23.0+
static partial class StatusBar
{
	const int statusBarHeightPadding = 3;
	const string statusBarOverlayTag = "StatusBarOverlay";

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

	/// <summary>
	/// Update the status bar size
	/// </summary>
	[SupportedOSPlatform("android35.0")]
	public static void UpdateBarSize()
	{
		if (!TryGetWindowAndDecorGroup(out var window, out var decorGroup))
		{
			return;
		}

		var statusBarOverlay = decorGroup.FindViewWithTag(statusBarOverlayTag);
		statusBarOverlay?.LayoutParameters?.Height = GetStatusBarHeight(window) + statusBarHeightPadding;
	}

	static void PlatformSetColor(Color color)
	{
		if (!TryGetWindowAndDecorGroup(out var window, out var decorGroup))
		{
			return;
		}

		var platformColor = color.ToPlatform();
		if (!OperatingSystem.IsAndroidVersionAtLeast(35))
		{
			PlatformSetColor_AndroidApiLessThan35(window, platformColor);
		}
		else if (OperatingSystem.IsAndroidVersionAtLeast(36))
		{
			PlatformSetColor_AndroidApi36AndHigher(window, platformColor, decorGroup);
		}
		else
		{
			PlatformSetColor_AndroidApi35(window, platformColor, decorGroup);
		}
	}

	[SupportedOSPlatform("android36.0")]
	static void PlatformSetColor_AndroidApi36AndHigher(Window window, PlatformColor platformColor, ViewGroup decorGroup)
	{
		var statusBarOverlay = decorGroup.FindViewWithTag(statusBarOverlayTag);

		if (statusBarOverlay is null)
		{
			window.DecorView.Post(() =>
			{
				var existingOverlay = decorGroup.FindViewWithTag(statusBarOverlayTag);

				if (existingOverlay is not null)
				{
					existingOverlay.SetBackgroundColor(platformColor);
					ApplyWindowFlags(window, platformColor);
					return;
				}

				ApplyStatusBarOverlay(window, platformColor, decorGroup);
				ApplyWindowFlags(window, platformColor);
			});
		}
		else
		{
			statusBarOverlay.SetBackgroundColor(platformColor);
			ApplyWindowFlags(window, platformColor);
		}
	}

	[SupportedOSPlatform("android35.0"), UnsupportedOSPlatform("android36.0")]
	static void PlatformSetColor_AndroidApi35(in Window window, in PlatformColor platformColor, in ViewGroup decorGroup)
	{
		var statusBarOverlay = decorGroup.FindViewWithTag(statusBarOverlayTag);
		if (statusBarOverlay is null)
		{
			ApplyStatusBarOverlay(window, platformColor, decorGroup);
		}
		else
		{
			statusBarOverlay.SetBackgroundColor(platformColor);
		}

		ApplyWindowFlags(window, platformColor);
	}

	[SupportedOSPlatform("android"), UnsupportedOSPlatform("android35.0")]
	static void PlatformSetColor_AndroidApiLessThan35(in Window window, in PlatformColor platformColor)
	{
		window.SetStatusBarColor(platformColor);
		ApplyWindowFlags(window, platformColor);
	}

	[SupportedOSPlatform("android35.0")]
	static void ApplyStatusBarOverlay(in Window window, in PlatformColor platformColor, in ViewGroup decorGroup)
	{
		var statusBarOverlay = new View(Activity)
		{
			LayoutParameters = new FrameLayout.LayoutParams(
				ViewGroup.LayoutParams.MatchParent,
				GetStatusBarHeight(window) + statusBarHeightPadding)
			{
				Gravity = GravityFlags.Top
			},
			Tag = statusBarOverlayTag
		};

		decorGroup.AddView(statusBarOverlay);
		statusBarOverlay.SetZ(0);
		statusBarOverlay.SetBackgroundColor(platformColor);
	}

	static void ApplyWindowFlags(Window window, PlatformColor platformColor)
	{
		bool isTransparent = platformColor == PlatformColor.Transparent;

		if (isTransparent)
		{
			window.ClearFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
			window.SetFlags(WindowManagerFlags.LayoutNoLimits, WindowManagerFlags.LayoutNoLimits);
		}
		else
		{
			window.ClearFlags(WindowManagerFlags.LayoutNoLimits);
			window.SetFlags(
				WindowManagerFlags.DrawsSystemBarBackgrounds,
				WindowManagerFlags.DrawsSystemBarBackgrounds);
		}

		WindowCompat.SetDecorFitsSystemWindows(window, !isTransparent);
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
		if (Activity.GetCurrentWindow() is Window window
			&& WindowCompat.GetInsetsController(window, window.DecorView) is WindowInsetsControllerCompat windowController)
		{
			windowController.AppearanceLightStatusBars = isLightStatusBars;
		}
	}

	static bool TryGetWindowAndDecorGroup([NotNullWhen(true)] out Window? window, [NotNullWhen(true)] out ViewGroup? decorGroup)
	{
		if (!IsSupported || Activity.GetCurrentWindow() is not Window { DecorView.RootView: not null } currentWindow)
		{
			window = null;
			decorGroup = null;
			return false;
		}

		window = currentWindow;
		decorGroup = (ViewGroup)window.DecorView.RootView;

		return true;
	}

	[SupportedOSPlatform("android35.0")]
	static int GetStatusBarHeight(in Window window)
	{
		if (!OperatingSystem.IsAndroidVersionAtLeast(35))
		{
			throw new NotSupportedException("StatusBar overlay is only supported on Android API 35+");
		}

		if (OperatingSystem.IsAndroidVersionAtLeast(36))
		{
			var insets = window.DecorView.RootWindowInsets;
			return insets?.GetInsets(WindowInsets.Type.StatusBars()).Top ?? 0;
		}
		else
		{
			var resId = Activity.Resources?.GetIdentifier("status_bar_height", "dimen", "android");
			return resId is not null
				? Activity.Resources?.GetDimensionPixelSize(resId.Value) ?? 0
				: 0;
		}
	}
}