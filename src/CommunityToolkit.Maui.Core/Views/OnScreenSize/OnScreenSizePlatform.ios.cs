using UIKit;


using System;
using System.Runtime.InteropServices;
using Foundation;
using ObjCRuntime;
#pragma warning disable CS8603
namespace CommunityToolkit.Maui.Core.Views.OnScreenSize;


#if IOS
/// <summary>
/// Platform-specifics for getting specific screen information.
/// </summary>
public static partial  class OnScreenSizePlatform
{
	/// <summary>
	/// Returns how many horizontal/vertical pixels-per-inches the current device screen has.
	/// </summary>
	/// <returns></returns>
	public static (double xdpi, double ydpi) GetPixelPerInches()
	{
		var displayInfo = DeviceDisplay.Current.MainDisplayInfo;

		var dimensions = (displayInfo.Width / displayInfo.Density, displayInfo.Height / displayInfo.Density);

		AppleScreenDensityHelper.TryGetPpiWithFallBacks(DeviceInfo.Current.Model, DeviceInfo.Current.Name, dimensions, out var ppi);
		return (ppi , ppi);
	}
}

#endif