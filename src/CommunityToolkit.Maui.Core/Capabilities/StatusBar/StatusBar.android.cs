using System.Diagnostics;
using Android.OS;
using Android.Util;
using Android.Views;
using Microsoft.Maui.Platform;
using Activity = Android.App.Activity;
using Debug = System.Diagnostics.Debug;

namespace CommunityToolkit.Maui.Core.Capabilities;
static partial class StatusBar
{
	static void PlatformSetColor(Color color)
	{
		if (!IsSupported())
		{
			return;
		}
		Activity.Window?.SetStatusBarColor(color.ToPlatform());
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
			default:
				RemoveBarAppearanceFlag(Activity, (StatusBarVisibility)SystemUiFlags.LightStatusBar);
				break;
		}
	}

	static void AddBarAppearanceFlag(Activity activity, StatusBarVisibility flag) =>
			SetBarAppearance(activity, barAppearance => barAppearance |= flag);

	static void RemoveBarAppearanceFlag(Activity activity, StatusBarVisibility flag) =>
		SetBarAppearance(activity, barAppearance => barAppearance &= ~flag);

	[Obsolete]
	static void SetBarAppearance(Activity activity, Func<StatusBarVisibility, StatusBarVisibility> updateAppearance)
	{
		var window = GetCurrentWindow(activity);
		var appearance = window.DecorView.SystemUiVisibility;
		appearance = updateAppearance(appearance);
		window.DecorView.SystemUiVisibility = appearance;

		static Window GetCurrentWindow(Activity activity)
		{
			var window = activity.Window ?? throw new NullReferenceException();
			window.ClearFlags(WindowManagerFlags.TranslucentStatus);
			window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
			return window;
		}
	}


	static bool IsSupported()
	{
		if (PlatformVersion.SdkInt < (int)BuildVersionCodes.M)
		{
			Debug.WriteLine($"This functionality is not available. Minimum supported API is {(int)BuildVersionCodes.M}");
			return false;
		}

		return true;
	}

	static Activity Activity => Microsoft.Maui.ApplicationModel.Platform.CurrentActivity ?? throw new NullReferenceException("Android Activity can't be null.");
}