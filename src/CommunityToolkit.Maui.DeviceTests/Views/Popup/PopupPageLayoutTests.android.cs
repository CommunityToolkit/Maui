using AView = Android.Views.View;

namespace CommunityToolkit.Maui.DeviceTests;

// Android: the Popup's platform view is a ContentViewGroup; its size is the one Android laid it out with, converted to device-independent units.
public partial class PopupPageLayoutTests
{
	static Size GetPlatformViewSize(IView view)
	{
		var platformView = (AView)view.Handler!.PlatformView!;
		var density = platformView.Context!.Resources!.DisplayMetrics!.Density;
		return new Size(platformView.Width / density, platformView.Height / density);
	}
}