using System.Runtime.Versioning;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using Microsoft.Maui.Platform;
using Activity = Android.App.Activity;

namespace CommunityToolkit.Maui.Core.Platform;

[SupportedOSPlatform("Android23.0")] // StatusBar is only supported on Android 23.0+
static partial class StatusBar
{
	static Activity Activity => Microsoft.Maui.ApplicationModel.Platform.CurrentActivity ?? throw new InvalidOperationException("Android Activity can't be null.");

	static void PlatformSetColor(Color color)
	{
		if (IsSupported())
		{
			Activity.Window?.SetStatusBarColor(color.ToPlatform());
		}
	}

	static void PlatformSetStyle(StatusBarStyle style)
	{
		if (!IsSupported())
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
		var window = GetCurrentWindow(activity);
		var windowController = WindowCompat.GetInsetsController(window, window.DecorView);
		windowController.AppearanceLightStatusBars = isLightStatusBars;

		static Window GetCurrentWindow(Activity activity)
		{
			var window = activity.Window ?? throw new InvalidOperationException($"{nameof(activity.Window)} cannot be null");
			window.ClearFlags(WindowManagerFlags.TranslucentStatus);
			window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
			return window;
		}
	}

	static bool IsSupported()
	{
		if (OperatingSystem.IsAndroidVersionAtLeast((int)BuildVersionCodes.M))
		{
			return true;
		}

		System.Diagnostics.Debug.WriteLine($"This functionality is not available. Minimum supported API is {(int)BuildVersionCodes.M}");
		return false;
	}
}