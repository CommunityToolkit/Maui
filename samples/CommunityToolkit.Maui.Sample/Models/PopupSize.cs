namespace CommunityToolkit.Maui.Sample.Models;

public class PopupSizeConstants
{
	public PopupSizeConstants(IDeviceDisplay deviceDisplay)
	{
		Tiny = new(100, 100);
		Small = new(300, 300);
		Medium = new(0.4 * (deviceDisplay.MainDisplayInfo.Width / deviceDisplay.MainDisplayInfo.Density), 0.6 * (deviceDisplay.MainDisplayInfo.Height / deviceDisplay.MainDisplayInfo.Density));
		Large = new(0.5 * (deviceDisplay.MainDisplayInfo.Width / deviceDisplay.MainDisplayInfo.Density), 0.8 * (deviceDisplay.MainDisplayInfo.Height / deviceDisplay.MainDisplayInfo.Density));
	}

	// examples for fixed sizes
	public Size Tiny { get; }

	public Size Small { get; }

	// examples for relative to screen sizes
	public Size Medium { get; }

	public Size Large { get; }
}