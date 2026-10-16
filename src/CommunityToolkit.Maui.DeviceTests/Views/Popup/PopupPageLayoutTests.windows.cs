using Microsoft.UI.Xaml;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class PopupPageLayoutTests
{
	// The rendered size (ActualWidth/ActualHeight) follows the Width and Height that .NET MAUI sets from the Popup's explicit size, so it tells
	// whether the platform view was resized or only clipped by its layout slot.
	static Size GetPlatformViewSize(IView view)
	{
		var platformView = (FrameworkElement)view.Handler!.PlatformView!;
		return new Size(platformView.ActualWidth, platformView.ActualHeight);
	}
}