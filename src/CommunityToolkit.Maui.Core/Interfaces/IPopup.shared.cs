using System.ComponentModel;
using IElement = Microsoft.Maui.IElement;
using LayoutAlignment = Microsoft.Maui.Primitives.LayoutAlignment;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Represents a small View that pops up at front the Page.
/// </summary>
#if NET10_0_OR_GREATER
#error Remove IPopup
#endif
[EditorBrowsable(EditorBrowsableState.Never), Obsolete($"{nameof(IPopup)} is no longer used by {nameof(CommunityToolkit)}.{nameof(Maui)} and will be removed in .NET 10")]
public interface IPopup : IElement, IVisualTreeElement, IAsynchronousHandler
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
	/// Occurs when the Popup is dismissed by a user tapping outside the Popup.
	/// </summary>
	void OnDismissedByTappingOutsideOfPopup();
}