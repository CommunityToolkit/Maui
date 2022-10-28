using System.Diagnostics;
using System.Runtime.Versioning;
using Android.OS;
using Android.Util;
using Android.Views;
using Microsoft.Maui.Platform;
using Activity = Android.App.Activity;

namespace CommunityToolkit.Maui.Core.Platform;

[UnsupportedOSPlatform("Android"), SupportedOSPlatform("Android23.0")] // StatusBar is only supported on Android +23.0
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
				AddBarAppearanceFlag(Activity, (StatusBarVisibility)SystemUiFlags.LightStatusBar);
				break;

			case StatusBarStyle.Default:
			case StatusBarStyle.LightContent:
				RemoveBarAppearanceFlag(Activity, (StatusBarVisibility)SystemUiFlags.LightStatusBar);
				break;

			default:
				throw new NotSupportedException($"{nameof(StatusBarStyle)} {style} is not yet supported on Android");
		}
	}

	static void AddBarAppearanceFlag(Activity activity, StatusBarVisibility flag) =>
		SetBarAppearance(activity, barAppearance => barAppearance |= flag);


	static void RemoveBarAppearanceFlag(Activity activity, StatusBarVisibility flag) =>
		SetBarAppearance(activity, barAppearance => barAppearance &= ~flag);


	static void SetBarAppearance(Activity activity, Func<StatusBarVisibility, StatusBarVisibility> updateAppearance)
	{
		var window = GetCurrentWindow(activity);
#pragma warning disable CS0618 // Type or member is obsolete
		var appearance = window.DecorView.SystemUiVisibility;
		appearance = updateAppearance(appearance);
		window.DecorView.SystemUiVisibility = appearance;
#pragma warning restore CS0618 // Type or member is obsolete

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