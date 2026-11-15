using UIKit;
using PlatformView = UIKit.UIView;

namespace CommunityToolkit.Maui.DeviceTests;

// Resigning the first responder also dismisses the iOS keyboard, so iOS and Mac Catalyst only check the focus.
public abstract partial class KeyboardDismissalTestBase
{
#if IOS
	// These tests only check the focus. The software keyboard's bring-up can block the CI simulator's main thread long enough
	// for UIKit to drop the focus.
	static partial void ReplaceSoftwareKeyboard(InputView input) => ((UITextField)GetPlatformView(input)).InputView = new UIView();
#endif

	protected static PlatformView GetPlatformView(InputView input) =>
		(input.Handler as IPlatformViewHandler)?.PlatformView ?? throw new InvalidOperationException("The view has no platform view.");

	protected static void SetPlatformText(PlatformView platformView, string text) => ((UITextField)platformView).Text = text;

	protected static bool IsPlatformViewFocused(PlatformView platformView) => platformView.IsFirstResponder;
}