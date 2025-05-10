namespace CommunityToolkit.Maui.Core;

/// <inheritdoc/>
public interface IPopupResult<out T> : IPopupResult
{
	/// <summary>
	/// PopupResult
	/// </summary>
	T? Result { get; }
}

/// <summary>
/// Represents the result of a popup.
/// </summary>
public interface IPopupResult
{
	/// <summary>
	/// True if Popup is closed by tapping outside the popup
	/// </summary>
	bool WasDismissedByTappingOutsideOfPopup { get; }
}