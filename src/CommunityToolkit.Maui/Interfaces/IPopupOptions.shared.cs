namespace CommunityToolkit.Maui;

/// <summary>
/// Interface defining the options used in <see cref="Views.Popup"/>.
/// </summary>
public interface IPopupOptions
{
	/// <summary>
	/// Gets or sets a value indicating whether the popup can be dismissed by tapping outside the Popup.
	/// </summary>
	bool CanBeDismissedByTappingOutsideOfPopup { get; }
	
	/// <summary>
	/// Color of the overlay behind the Popup
	/// </summary>
	Color PageOverlayColor { get; }
	
	/// <summary>
	/// Popup border stroke.
	/// </summary>
	Brush? BorderStroke { get; }

	/// <summary>
	/// Action to be executed when the user taps outside the Popup.
	/// </summary>
	Action? OnTappingOutsideOfPopup { get; }

	/// <summary>
	/// Popup border shape.
	/// </summary>
	IShape? Shape { get; }

	/// <summary>
	/// Popup margin.
	/// </summary>
	Thickness Margin { get; }

	/// <summary>
	/// Popup padding.
	/// </summary>
	Thickness Padding { get; }

	/// <summary>
	/// Popup vertical options.
	/// </summary>
	LayoutOptions VerticalOptions { get; }

	/// <summary>
	/// Popup horizontal options.
	/// </summary>
	LayoutOptions HorizontalOptions { get; }
}