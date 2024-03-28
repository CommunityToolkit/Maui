using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui;

/// <summary>
/// Defines the contract for a service that manages the presentation of popups.
/// </summary>
public interface IPopupLifecycleController
{
	/// <summary>
	/// Gets the currently displayed popup.
	/// </summary>
	/// <returns>The currently displayed popup.</returns>
	Popup? GetCurrentPopup();

	/// <summary>
	/// Called when a popup is shown.
	/// </summary>
	/// <param name="popup">The popup being shown.</param>
	void OnShowPopup(Popup popup);
}