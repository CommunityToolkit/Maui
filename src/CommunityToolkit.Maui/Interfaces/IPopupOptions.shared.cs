using Microsoft.Maui.Controls.Shapes;

namespace CommunityToolkit.Maui;

/// <summary>
/// Interface defining the options used in <see cref="Views.Popup"/>.
/// </summary>
public interface IPopupOptions
{
	/// <summary>
	/// Indicates whether the popup can be dismissed by tapping outside the Popup.
	/// </summary>
	bool CanBeDismissedByTappingOutsideOfPopup { get; }
	/// <summary>
	/// Indicates whether the popup can be dismissed by pressing the back button.
	/// </summary>
	bool CanBeDismissedByBackButtonPressed { get; }

	/// <summary>
	/// Color of the overlay behind the Popup
	/// </summary>
	Color PageOverlayColor { get; }

	/// <summary>
	/// Action to be executed when the user taps outside the Popup.
	/// </summary>
	Action? OnTappingOutsideOfPopup { get; }

	/// <summary>
	/// Action to be executed when the user presses the back button.
	/// </summary>
	Action? OnBackButtonPressed { get; }
	/// <summary>
	/// Action to be executed when the user dismisses the Popup.
	/// </summary>
	Action? OnDismiss { get; }

	/// <summary>
	/// Popup border shape.
	/// </summary>
	Shape? Shape { get; }

	/// <summary>
	/// Popup Shadow
	/// </summary>
	Shadow? Shadow { get; }
}