using UIKit;
using PlatformView = UIKit.UIView;

namespace CommunityToolkit.Maui.DeviceTests;

// iOS and Mac Catalyst: an Entry is a UITextField, which has the focus while it is the first responder. Resigning the first responder also
// dismisses the iOS keyboard, so these platforms only check the focus.
public abstract partial class KeyboardDismissalTestBase
{
	protected static void SetPlatformText(PlatformView platformView, string text) => ((UITextField)platformView).Text = text;

	protected static bool IsPlatformViewFocused(PlatformView platformView) => platformView.IsFirstResponder;
}