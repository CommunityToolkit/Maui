namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Represents the result of a popup.
/// </summary>
/// <param name="WasDismissedByTappingOutsideOfPopup">True if Popup is closed by tapping outside of popup</param>
public record PopupResult(bool WasDismissedByTappingOutsideOfPopup);

/// <summary>
/// Represents the result of a popup.
/// </summary>
/// <typeparam name="T">Popup result type</typeparam>
/// <param name="Result">Popup result</param>
/// <param name="WasDismissedByTappingOutsideOfPopup">True if Popup is closed by tapping outside of the popup</param>
public record PopupResult<T>(T? Result, bool WasDismissedByTappingOutsideOfPopup) : PopupResult(WasDismissedByTappingOutsideOfPopup);