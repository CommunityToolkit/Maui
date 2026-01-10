
using CommunityToolkit.Maui.Extensions;

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
	public new partial Thickness Margin { get; set; } = Options.DefaultPopupSettings.Margin;

	/// <summary>
	/// Gets or sets the padding between the <see cref="Popup"/> border and the <see cref="Popup"/> content.
	/// </summary>
	[BindableProperty]
	public new partial Thickness Padding { get; set; } = Options.DefaultPopupSettings.Padding;

	/// <summary>
	/// Gets or sets the horizontal position of the <see cref="Popup"/> when displayed on screen.
	/// </summary>
	[BindableProperty]
	public new partial LayoutOptions HorizontalOptions { get; set; } = Options.DefaultPopupSettings.HorizontalOptions;

	/// <summary>
	/// Gets or sets the vertical position of the <see cref="Popup"/> when displayed on screen.
	/// </summary>
	[BindableProperty]
	public new partial LayoutOptions VerticalOptions { get; set; } = Options.DefaultPopupSettings.VerticalOptions;

	/// <inheritdoc cref="IPopupOptions.CanBeDismissedByTappingOutsideOfPopup"/> />
	/// <remarks>
	/// When true and the user taps outside the popup, it will dismiss.
	/// On Android - when false the hardware back button is disabled.
	/// </remarks>
	[BindableProperty]
	public partial bool CanBeDismissedByTappingOutsideOfPopup { get; set; } = Options.DefaultPopupSettings.CanBeDismissedByTappingOutsideOfPopup;

	/// <summary>
	/// Close the Popup.
	/// </summary>
	public virtual Task CloseAsync(CancellationToken token = default) => GetPopupPage().CloseAsync(new PopupResult(false), token);

	internal void NotifyPopupIsOpened()
	{
		Opened?.Invoke(this, EventArgs.Empty);
	}

	internal void NotifyPopupIsClosed()
	{
		Closed?.Invoke(this, EventArgs.Empty);
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