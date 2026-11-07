using UIKit;
using PlatformView = UIKit.UIView;

namespace CommunityToolkit.Maui.DeviceTests;

// Resigning the first responder also dismisses the iOS keyboard, so iOS and Mac Catalyst only check the focus.
public abstract partial class KeyboardDismissalTestBase
{
	protected static PlatformView GetPlatformView(InputView input) =>
		(input.Handler as IPlatformViewHandler)?.PlatformView ?? throw new InvalidOperationException("The view has no platform view.");

	protected static void SetPlatformText(PlatformView platformView, string text) => ((UITextField)platformView).Text = text;

	protected static bool IsPlatformViewFocused(PlatformView platformView) => platformView.IsFirstResponder;
}