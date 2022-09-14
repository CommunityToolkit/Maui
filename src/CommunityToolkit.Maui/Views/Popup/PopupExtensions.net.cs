namespace CommunityToolkit.Maui.Views;

public static partial class PopupExtensions
{
	static void PlatformShowPopup(Popup popup, IMauiContext mauiContext) =>
		throw new NotSupportedException($"The current platform '{DeviceInfo.Platform}' does not support CommunityToolkit.Maui.Core.Popup.");

	static Task<object?> PlatformShowPopupAsync(Popup popup, IMauiContext mauiContext) =>
		throw new NotSupportedException($"The current platform '{DeviceInfo.Platform}' does not support CommunityToolkit.Maui.Core.Popup.");
}