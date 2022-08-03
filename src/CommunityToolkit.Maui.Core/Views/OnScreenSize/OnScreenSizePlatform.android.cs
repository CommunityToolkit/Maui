using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.App;

namespace CommunityToolkit.Maui.Core.Views.OnScreenSize;

#if ANDROID
/// <summary>
/// Platform-specifics for getting specific screen information.
/// </summary>
public static partial class OnScreenSizePlatform 
{
	/// <summary>
	/// Returns how many horizontal/vertical pixels-per-inches the current device screen has.
	/// </summary>
	/// <returns></returns>
	public static bool TryGetPixelPerInches(out double xdpi, out double ydpi)
	{
		var displayMetrics = Android.App.Application.Context.Resources?.DisplayMetrics;
		
		xdpi = displayMetrics?.Xdpi ?? 0;
		ydpi = displayMetrics?.Ydpi ?? 0;
		return true;
	}
}
#endif