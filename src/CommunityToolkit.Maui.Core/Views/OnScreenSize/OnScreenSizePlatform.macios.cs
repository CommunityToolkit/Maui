using UIKit;

namespace CommunityToolkit.Maui.Core.Views.OnScreenSize;

#if MACCATALYST
/// <summary>
/// Platform-specifics for getting specific screen information.
/// </summary>
public static partial class OnScreenSizePlatform 
{
	/// <summary>
	/// Returns how many horizontal/vertical pixels-per-inches the current device screen has.
	/// </summary>
	/// <returns></returns>
	public static (double xdpi, double ydpi) GetPixelPerInches()
	{
		var displayInfo = Microsoft.Maui.Devices.DeviceDisplay.Current.MainDisplayInfo;
		
		var scale = displayInfo.Density;
		var dpi = scale * 160;
		
		if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad)
		{
			dpi = scale * 132;
		} 
		else if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone)
		{
			dpi = scale * 163;
		}

		return (dpi, dpi);
	}
}
#endif