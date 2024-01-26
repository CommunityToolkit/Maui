using System.Runtime.Versioning;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using Microsoft.Maui.Platform;
using Activity = Android.App.Activity;

namespace CommunityToolkit.Maui.Core.Platform;

[SupportedOSPlatform("Android23.0")] // SystemNavigationBar is only supported on Android 23.0+
static partial class SystemNavigationBar
{
	static Activity Activity => Microsoft.Maui.ApplicationModel.Platform.CurrentActivity ?? throw new InvalidOperationException("Android Activity can't be null.");

	static void PlatformSetColor(Color color)
	{
		if (IsSupported())
		{
			Activity.Window?.SetNavigationBarColor(color.ToPlatform());
		}
	}

	static void PlatformSetStyle(SystemNavigationBarStyle style)
	{
		if (!IsSupported())
		{
			return;
		}

		switch (style)
		{
			case SystemNavigationBarStyle.DarkContent:
				SetSystemNavigationBarAppearance(Activity, true);
				break;

			case SystemNavigationBarStyle.Default:
			case SystemNavigationBarStyle.LightContent:
				SetSystemNavigationBarAppearance(Activity, false);
				break;

			default:
				throw new NotSupportedException($"{nameof(SystemNavigationBarStyle)} {style} is not yet supported on Android");
		}
	}

	static void SetSystemNavigationBarAppearance(Activity activity, bool isLightSystemNavigationBars)
	{
		var window = GetCurrentWindow(activity);
		var windowController = WindowCompat.GetInsetsController(window, window.DecorView);
		windowController.AppearanceLightNavigationBars = isLightSystemNavigationBars;

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

		System.Diagnostics.Trace.WriteLine($"This functionality is not available. Minimum supported API is {(int)BuildVersionCodes.M}");
		return false;
	}
}