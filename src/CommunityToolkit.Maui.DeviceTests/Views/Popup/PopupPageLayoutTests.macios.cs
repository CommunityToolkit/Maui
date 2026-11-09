using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class PopupPageLayoutTests
{
	static Size GetPlatformViewSize(IView view)
	{
		var frame = ((UIView)view.Handler!.PlatformView!).Frame;
		return new Size(frame.Width, frame.Height);
	}
}