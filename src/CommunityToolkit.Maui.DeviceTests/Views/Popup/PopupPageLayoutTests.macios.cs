using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

// iOS and Mac Catalyst: the Popup's platform view is a ContentView (UIView); its size is the size of its frame.
public partial class PopupPageLayoutTests
{
	static Size GetPlatformViewSize(IView view)
	{
		var frame = ((UIView)view.Handler!.PlatformView!).Frame;
		return new Size(frame.Width, frame.Height);
	}
}