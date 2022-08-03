using UIKit;

namespace CommunityToolkit.Maui.Core.Views.OnScreenSize;

#if (IOS || MACCATALYST)
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
		var displayInfo = DeviceDisplay.Current.MainDisplayInfo;

		var dimensions = (displayInfo.Width / displayInfo.Density, displayInfo.Height / displayInfo.Density);

		var success = AppleScreenDensityHelper.TryGetPpiWithFallBacks(DeviceInfo.Current.Model, DeviceInfo.Current.Name, dimensions, out var ppi);
		xdpi = ppi;
		ydpi = ppi;
		return success;
	}
}
#endif