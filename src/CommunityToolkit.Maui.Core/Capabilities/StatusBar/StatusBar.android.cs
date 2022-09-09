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
