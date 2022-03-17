namespace CommunityToolkit.Maui.Sample.Models;

static class PopupSizeConstants
{
	// examples for fixed sizes
	public static Size Tiny { get; } = new(100, 100);

	public static Size Small { get; } = new(300, 300);

	// examples for relative to screen sizes
	public static Size Medium { get; } = new(0.7 * (DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density), 0.6 * (DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density));

	public static Size Large { get; } = new(0.9 * (DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density), 0.8 * (DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density));
}
