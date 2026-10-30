using AView = Android.Views.View;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class PopupPageLayoutTests
{
	static Size GetPlatformViewSize(IView view)
	{
		var platformView = (AView)view.Handler!.PlatformView!;
		var density = platformView.Context!.Resources!.DisplayMetrics!.Density;
		return new Size(platformView.Width / density, platformView.Height / density);
	}
}