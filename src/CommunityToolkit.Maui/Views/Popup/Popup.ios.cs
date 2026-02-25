using Foundation;
using UIKit;

namespace CommunityToolkit.Maui.Views;

public partial class Popup
{
    /// <summary>
	/// Stores the keyboard will show notification observer to manage keyboard lifecycle.
	/// </summary>
	NSObject? willShow;

	/// <summary>
	/// Stores the keyboard will hide notification observer to manage keyboard lifecycle.
	/// </summary>
	NSObject? willHide;

	/// <summary>
	/// Stores the native platform view to adjust safe area insets when keyboard appears.
	/// </summary>
	UIView? popupNativeView;

	/// <summary>
	/// Stores the view controller associated with the popup to adjust safe area insets.
	/// </summary>
	UIViewController? viewController;

	partial void OnPlatformPopupOpened()
    {
        if (Handler?.PlatformView is UIView view)
		{
			popupNativeView = view;
		}

		willShow = UIKeyboard.Notifications.ObserveWillShow((_, args) => HandleKeyboard(args));

		willHide = UIKeyboard.Notifications.ObserveWillHide((_, args) => ResetSafeArea());
    }

	partial void OnPlatformPopupClosed()
    {
        willShow?.Dispose();
		willHide?.Dispose();

		willShow = willHide = null;

		popupNativeView = null;
		viewController = null;
    }

    /// <summary>
	/// Adjusts the safe area insets when the keyboard appears on iOS.
	/// </summary>
	/// <param name="args">The keyboard event arguments containing the keyboard frame.</param>
	void HandleKeyboard(UIKeyboardEventArgs args)
	{
		if (popupNativeView is null)
		{
			return;
		}

		viewController ??= popupNativeView.Window?.RootViewController?.PresentedViewController;

		if (viewController is null)
		{
			return;
		}

		viewController.AdditionalSafeAreaInsets = new UIEdgeInsets(0, 0, args.FrameEnd.Height, 0);
	}

	/// <summary>
	/// Resets the safe area insets when the keyboard is hidden on iOS.
	/// </summary>
	void ResetSafeArea()
	{
		viewController?.AdditionalSafeAreaInsets = UIEdgeInsets.Zero;
	}
}