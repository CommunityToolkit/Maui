
using CommunityToolkit.Maui.Extensions;
#if ANDROID
using Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;
#elif IOS  && !NET10_0_OR_GREATER
using Foundation;
using UIKit;
#endif

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// Represents a small View that pops up at front the Page.
/// </summary>
public partial class Popup : ContentView
{
	/// <summary>
	/// Initializes Popup
	/// </summary>
	public Popup()
	{
		Margin = Options.DefaultPopupSettings.Margin;
		Padding = Options.DefaultPopupSettings.Padding;
		HorizontalOptions = Options.DefaultPopupSettings.HorizontalOptions;
		VerticalOptions = Options.DefaultPopupSettings.VerticalOptions;
		BackgroundColor = Options.DefaultPopupSettings.BackgroundColor;
		CanBeDismissedByTappingOutsideOfPopup = Options.DefaultPopupSettings.CanBeDismissedByTappingOutsideOfPopup;
	}

	/// <summary>
	/// Event occurs when <see cref="Popup"/> is opened.
	/// </summary>
	public event EventHandler? Opened;

	/// <summary>
	/// Event occurs when <see cref="Popup"/> is closed.
	/// </summary>
	public event EventHandler? Closed;

	/// <summary>
	/// Gets or sets the margin between the <see cref="Popup"/> and the edge of the window.
	/// </summary>
	[BindableProperty]
	public new partial Thickness Margin { get; set; }

	/// <summary>
	/// Gets or sets the horizontal position of the <see cref="Popup"/> when displayed on screen.
	/// </summary>
	[BindableProperty]
	public new partial LayoutOptions HorizontalOptions { get; set; }

	/// <summary>
	/// Gets or sets the vertical position of the <see cref="Popup"/> when displayed on screen.
	/// </summary>
	[BindableProperty]
	public new partial LayoutOptions VerticalOptions { get; set; }

	/// <inheritdoc cref="IPopupOptions.CanBeDismissedByTappingOutsideOfPopup"/> />
	/// <remarks>
	/// When true and the user taps outside the popup, it will dismiss.
	/// On Android - when false the hardware back button is disabled.
	/// </remarks>
	[BindableProperty]
	public partial bool CanBeDismissedByTappingOutsideOfPopup { get; set; }

	/// <summary>
	/// Gets or sets the padding between the <see cref="Popup"/> border and the <see cref="Popup"/> content.
	/// </summary>
	public new Thickness Padding
	{
		get => base.Padding;
		set => base.Padding = value;
	}

	/// <summary>
	/// Close the Popup.
	/// </summary>
	public virtual Task CloseAsync(CancellationToken token = default) => GetPopupPage().CloseAsync(new PopupResult(false), token);

	#if IOS  && !NET10_0_OR_GREATER
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
	#endif

	internal void NotifyPopupIsOpened()
	{
		Opened?.Invoke(this, EventArgs.Empty);

		#if ANDROID
		// On Android, configure the window soft input mode to resize when the keyboard appears
		Microsoft.Maui.Controls.Application.Current?.On<Microsoft.Maui.Controls.PlatformConfiguration.Android>().UseWindowSoftInputModeAdjust(WindowSoftInputModeAdjust.Resize);
		#elif IOS  && !NET10_0_OR_GREATER
		// On iOS, store the native view and subscribe to keyboard events to adjust safe area insets
		if (Handler?.PlatformView is UIView view)
		{
			popupNativeView = view;
		}

		willShow = UIKeyboard.Notifications.ObserveWillShow((_, args) => HandleKeyboard(args));

		willHide = UIKeyboard.Notifications.ObserveWillHide((_, args) => ResetSafeArea());
		#endif
	}

	internal void NotifyPopupIsClosed()
	{
		Closed?.Invoke(this, EventArgs.Empty);

		#if ANDROID
		// On Android, reset the window soft input mode to unspecified when the popup closes
		Microsoft.Maui.Controls.Application.Current?.On<Microsoft.Maui.Controls.PlatformConfiguration.Android>().UseWindowSoftInputModeAdjust(WindowSoftInputModeAdjust.Unspecified);
		#elif IOS  && !NET10_0_OR_GREATER
		// On iOS, dispose of keyboard event observers and clean up stored references
		willShow?.Dispose();
		willHide?.Dispose();

		willShow = willHide = null;

		popupNativeView = null;
		viewController = null;
		#endif
	}

	private protected PopupPage GetPopupPage()
	{
		var parent = Parent;

		while (parent is not null)
		{
			if (parent.Parent is PopupPage popupPage)
			{
				return popupPage;
			}

			parent = parent.Parent;
		}

		throw new PopupNotFoundException();
	}

	#if IOS && !NET10_0_OR_GREATER
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

		// Get the view controller associated with the popup's native view
		viewController ??= popupNativeView.Window?.RootViewController?.PresentedViewController;

		if (viewController is null)
		{
			return;
		}

		// Adjust the bottom safe area inset to account for keyboard height
		viewController.AdditionalSafeAreaInsets = new UIEdgeInsets(0, 0, args.FrameEnd.Height, 0);
	}

	/// <summary>
	/// Resets the safe area insets when the keyboard is hidden on iOS.
	/// </summary>
	void ResetSafeArea()
	{
		if (viewController is not null)
		{
			viewController.AdditionalSafeAreaInsets = UIEdgeInsets.Zero;
		}
	}
	#endif
}

/// <summary>
/// Represents a small View that pops up at front the Page.
/// </summary>
public partial class Popup<T> : Popup
{
	/// <summary>
	/// Close the Popup with a result.
	/// </summary>
	/// <param name="result">Popup result</param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	public virtual Task CloseAsync(T result, CancellationToken token = default) => GetPopupPage().CloseAsync(new PopupResult<T>(result, false), token);
}

sealed class PopupNotFoundException() : InvalidPopupOperationException($"Unable to close popup: could not locate {nameof(PopupPage)}. {nameof(PopupExtensions.ShowPopup)} or {nameof(PopupExtensions.ShowPopupAsync)} must be called before {nameof(Popup.CloseAsync)}. If using a custom implementation of {nameof(Popup)}, override the {nameof(Popup.CloseAsync)} method");

sealed class PopupBlockedException(in Page currentVisibleModalPage) : InvalidPopupOperationException($"Unable to close Popup because it is blocked by the Modal Page {currentVisibleModalPage.GetType().FullName}. Please call `{nameof(Page.Navigation)}.{nameof(Page.Navigation.PopModalAsync)}()` to first remove {currentVisibleModalPage.GetType().FullName} from the {nameof(Page.Navigation.ModalStack)}");

class InvalidPopupOperationException(in string message) : InvalidOperationException(message);