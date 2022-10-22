using IElement = Microsoft.Maui.IElement;
using LayoutAlignment = Microsoft.Maui.Primitives.LayoutAlignment;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Represents a small View that pops up at front the Page.
/// </summary>
public interface IPopup : IElement, IVisualTreeElement
{
	/// <summary>
	/// Gets the View that Popup will be anchored.
	/// </summary>
	IView? Anchor { get; }

	/// <summary>
	/// Gets the Popup's color.
	/// </summary>
	Color? Color { get; }

	/// <summary>
	/// Gets the Popup's Content.
	/// </summary>
	IView? Content { get; }

	/// <summary>
	/// Gets the horizontal aspect of this element's arrangement in a container.
	/// </summary>
	LayoutAlignment HorizontalOptions { get; }

	/// <summary>
	/// Gets the CanBeDismissedByTappingOutsideOfPopup property.
	/// </summary>
	bool CanBeDismissedByTappingOutsideOfPopup { get; }

	/// <summary>
	/// Gets the Popup's size.
	/// </summary>
	Size Size { get; }

	/// <summary>
	/// Gets the vertical aspect of this element's arrangement in a container.
	/// </summary>
	LayoutAlignment VerticalOptions { get; }

	/// <summary>
	/// Occurs when the Popup is closed.
	/// </summary>
	/// <param name="result">Return value from the Popup.</param>
	void OnClosed(object? result = null);

	/// <summary>
	/// Occurs when the Popup is opened.
	/// </summary>
	void OnOpened();

	/// <summary>
	/// Occurs when the Popup is dismissed by a user tapping outside of the Popup.
	/// </summary>
	void OnDismissedByTappingOutsideOfPopup();
}