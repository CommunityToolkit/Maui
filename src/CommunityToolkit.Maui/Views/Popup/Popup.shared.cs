using System.ComponentModel;
using CommunityToolkit.Maui.Extensions;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// Represents a small View that pops up at front the Page.
/// </summary>
public partial class Popup : ContentView
{
	/// <summary>
	/// Gets or sets the margin between the <see cref="Popup"/> and the edge of the window.
	/// </summary>
	[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateDefaultMargin))]
	public partial Thickness Margin { get; set; }
	static object CreateDefaultMargin(global::Microsoft.Maui.Controls.BindableObject? _) => Options.DefaultPopupSettings.Margin;

	/// <summary>
	/// Gets or sets the padding between the <see cref="Popup"/> border and the <see cref="Popup"/> content.
	/// </summary>
	[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateDefaultPadding))]
	public partial Thickness Padding { get; set; }
	static object CreateDefaultPadding(global::Microsoft.Maui.Controls.BindableObject? _) => Options.DefaultPopupSettings.Padding;

	/// <summary>
	/// Gets or sets the horizontal layout options used to position the <see cref="Popup"/> when displayed on screen.
	/// </summary>
	[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateDefaultHorizontalOptions))]
	public partial LayoutOptions HorizontalOptions { get; set; }
	static object CreateDefaultHorizontalOptions(global::Microsoft.Maui.Controls.BindableObject? _) => Options.DefaultPopupSettings.HorizontalOptions;

	/// <summary>
	/// Gets or sets the vertical layout options used to position the <see cref="Popup"/> when displayed on screen.
	/// </summary>
	[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateDefaultVerticalOptions))]
	public partial LayoutOptions VerticalOptions { get; set; }
	static object CreateDefaultVerticalOptions(global::Microsoft.Maui.Controls.BindableObject? _) => Options.DefaultPopupSettings.VerticalOptions;

	/// <summary>
	/// Gets or sets a value indicating whether the <see cref="Popup"/> can be dismissed by tapping outside of the popup.
	/// </summary>
	[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateCanBeDismissedByTappingOutsideOfPopup))]
	public partial bool CanBeDismissedByTappingOutsideOfPopup { get; set; }
	static object CreateCanBeDismissedByTappingOutsideOfPopup(global::Microsoft.Maui.Controls.BindableObject? _) => Options.DefaultPopupSettings.CanBeDismissedByTappingOutsideOfPopup;


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