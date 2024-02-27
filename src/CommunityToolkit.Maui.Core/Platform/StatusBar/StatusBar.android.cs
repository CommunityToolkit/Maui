using System.Runtime.Versioning;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using CommunityToolkit.Maui.Core.Extensions;
using Microsoft.Maui.Platform;
using Activity = Android.App.Activity;

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
		if (IsSupported)
		{
			Activity.Window?.SetStatusBarColor(color.ToPlatform());
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
		var windowController = WindowCompat.GetInsetsController(window, window.DecorView);
		windowController.AppearanceLightStatusBars = isLightStatusBars;
	}
}