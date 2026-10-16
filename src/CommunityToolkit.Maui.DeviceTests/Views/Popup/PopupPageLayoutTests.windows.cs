using Microsoft.UI.Xaml;

namespace CommunityToolkit.Maui.DeviceTests;

// Windows: the Popup's platform view is a ContentPanel. Its rendered size (ActualWidth/ActualHeight) follows its Width and Height, which
// .NET MAUI sets from the Popup's explicit size, so it tells whether the platform view was resized or only clipped by its layout slot.
public partial class PopupPageLayoutTests
{
	static Size GetPlatformViewSize(IView view)
	{
		var platformView = (FrameworkElement)view.Handler!.PlatformView!;
		return new Size(platformView.ActualWidth, platformView.ActualHeight);
	}
}