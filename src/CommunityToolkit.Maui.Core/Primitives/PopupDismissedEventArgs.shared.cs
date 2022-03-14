namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Popup dismissed event arguments used when a popup is dismissed.
/// </summary>
public class PopupClosedEventArgs
{
	/// <summary>
	/// Initialization an instance of <see cref="PopupClosedEventArgs"/>.
	/// </summary>
	/// <param name="result">
	/// The result of the popup.
	/// </param>
	/// <param name="wasDismissedByTappingOutsideOfPopup">
	/// If the popup was dismissed by tapping outside of the Popup.
	/// </param>
	public PopupClosedEventArgs(object? result, bool wasDismissedByTappingOutsideOfPopup)
	{
		Result = result;
		WasDismissedByTappingOutsideOfPopup = wasDismissedByTappingOutsideOfPopup;
	}

	/// <summary>
	/// The resulting object to return.
	/// </summary>
	public object? Result { get; }

	/// <summary>
	/// If true, then the user tapped outside the bounds of
	/// the popup (a light dismiss). If false, then the
	/// popup was dismissed by user action or code.
	/// </summary>
	public bool WasDismissedByTappingOutsideOfPopup { get; }
}
