using CoreGraphics;
using Foundation;
using System.Diagnostics;
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

	UIEdgeInsets? originalAdditionalSafeAreaInsets;

	partial void OnPlatformPopupOpened()
	{
		willShow?.Dispose();
		willHide?.Dispose();

		if (Handler?.PlatformView is UIView view)
		{
			popupNativeView = view;
		}

		willShow = UIKeyboard.Notifications.ObserveWillChangeFrame((_, args) => HandleKeyboard(args));

		willHide = UIKeyboard.Notifications.ObserveWillHide((_, args) => ResetSafeArea());
	}

	partial void OnPlatformPopupClosed()
	{
		willShow?.Dispose();
		willHide?.Dispose();

		willShow = willHide = null;

		ResetSafeArea();

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

		if (viewController is null)
		{
			for (UIResponder? responder = popupNativeView.NextResponder; responder is not null; responder = responder.NextResponder)
			{
				if (responder is UIViewController parentViewController)
				{
					viewController = parentViewController;
					break;
				}
			}
		}

		if (viewController is null)
		{
			Trace.WriteLine("Failed to retrieve the presented view controller.");

			return;
		}

		if (viewController.View is not UIView controllerView)
		{
			Trace.WriteLine("Failed to retrieve the popup view controller's view.");

			return;
		}

		originalAdditionalSafeAreaInsets ??= viewController.AdditionalSafeAreaInsets;

		var window = controllerView.Window;

		if (window is null)
		{
			return;
		}

		var windowOrigin = window.Frame.Location;
		var keyboardOrigin = args.FrameEnd.Location;

		var keyboardInWindow = new CGRect(
			new CGPoint(keyboardOrigin.X - windowOrigin.X, keyboardOrigin.Y - windowOrigin.Y),
			args.FrameEnd.Size
		);
		var keyboardFrame = controllerView.ConvertRectFromView(keyboardInWindow, window);

		var bounds = controllerView.Bounds;

		var boundsBottom = bounds.Y + bounds.Height;

		var keyboardBottom = keyboardFrame.Y + keyboardFrame.Height;

		var overlapsHorizontally = keyboardFrame.X < bounds.X + bounds.Width && keyboardFrame.X + keyboardFrame.Width > bounds.X;

		var bottomOverlap = overlapsHorizontally && keyboardBottom >= boundsBottom
			? boundsBottom - (keyboardFrame.Y > bounds.Y ? keyboardFrame.Y : bounds.Y)
			: 0;

		if (bottomOverlap < 0)
		{
			bottomOverlap = 0;
		}

		var originalInsets = originalAdditionalSafeAreaInsets.Value;

		// Recalculate the system-only safe area inset using the current geometry.
		var currentAdditionalInsets = viewController.AdditionalSafeAreaInsets;

		var systemBottomInset = controllerView.SafeAreaInsets.Bottom
			- currentAdditionalInsets.Bottom;

		if (systemBottomInset < 0)
		{
			systemBottomInset = 0;
		}

		var keyboardInset = bottomOverlap - systemBottomInset;

		var bottomInset = keyboardInset > originalInsets.Bottom
			? keyboardInset
			: originalInsets.Bottom;

		viewController.AdditionalSafeAreaInsets = new UIEdgeInsets(
			originalInsets.Top,
			originalInsets.Left,
			bottomInset,
			originalInsets.Right
		);
	}

	/// <summary>
	/// Resets the safe area insets when the keyboard is hidden on iOS.
	/// </summary>
	void ResetSafeArea()
	{
		if (viewController is not null && originalAdditionalSafeAreaInsets is UIEdgeInsets originalInsets)
		{
			viewController.AdditionalSafeAreaInsets = originalInsets;
		}

		originalAdditionalSafeAreaInsets = null;

		// viewController?.AdditionalSafeAreaInsets = UIEdgeInsets.Zero;
	}
}