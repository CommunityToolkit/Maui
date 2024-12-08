namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Popup dismissed event arguments used when a popup is dismissed.
/// </summary>
/// <remarks>
/// Initialization an instance of <see cref="PopupClosedEventArgs"/>.
/// </remarks>
/// <param name="result">
/// The result of the popup.
/// </param>
/// <param name="wasDismissedByTappingOutsideOfPopup">
/// If the popup was dismissed by tapping outside the Popup.
/// </param>
public class PopupClosedEventArgs(object? result, bool wasDismissedByTappingOutsideOfPopup)
{
	/// <summary>
	/// The resulting object to return.
	/// </summary>
	public object? Result { get; } = result;

	/// <summary>
	/// If true, then the user tapped outside the bounds of the popup (aka "Light Dismiss").
	/// If false, then the popup was dismissed by user action or code.
	/// </summary>
	public bool WasDismissedByTappingOutsideOfPopup { get; } = wasDismissedByTappingOutsideOfPopup;
}