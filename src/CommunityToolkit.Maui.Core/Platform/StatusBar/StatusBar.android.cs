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

		if (Activity.Window is not null)
		{
			var platformColor = color.ToPlatform();

			if (OperatingSystem.IsAndroidVersionAtLeast(35))
			{
				const string statusBarOverlayTag = "StatusBarOverlay";

				var window = Activity.GetCurrentWindow();

				var decorGroup = (ViewGroup)window.DecorView;
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

					decorGroup.AddView(statusBarOverlay);
					statusBarOverlay.SetZ(0);
				}

				statusBarOverlay.SetBackgroundColor(platformColor);
			}
			else
			{
				Activity.Window.SetStatusBarColor(platformColor);
			}

			bool isColorTransparent = platformColor == PlatformColor.Transparent;
			if (isColorTransparent)
			{
				Activity.Window.ClearFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
				Activity.Window.SetFlags(WindowManagerFlags.LayoutNoLimits, WindowManagerFlags.LayoutNoLimits);
			}
			else
			{
				Activity.Window.ClearFlags(WindowManagerFlags.LayoutNoLimits);
				Activity.Window.SetFlags(WindowManagerFlags.DrawsSystemBarBackgrounds, WindowManagerFlags.DrawsSystemBarBackgrounds);
			}

			WindowCompat.SetDecorFitsSystemWindows(Activity.Window, !isColorTransparent);
		}
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
				SetStatusBarAppearance(Activity, true);
				break;

			case StatusBarStyle.Default:
			case StatusBarStyle.LightContent:
				SetStatusBarAppearance(Activity, false);
				break;

			default:
				throw new NotSupportedException($"{nameof(StatusBarStyle)} {style} is not yet supported on Android");
		}
	}

	static void SetStatusBarAppearance(Activity activity, bool isLightStatusBars)
	{
		var window = activity.GetCurrentWindow();
		if (WindowCompat.GetInsetsController(window, window.DecorView) is WindowInsetsControllerCompat windowController)
		{
			windowController.AppearanceLightStatusBars = isLightStatusBars;
		}
	}
}