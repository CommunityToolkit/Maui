namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Popup dismissed event arguments used when a popup is dismissed.
/// </summary>
public class PopupDismissedEventArgs
{
	/// <summary>
	/// Initialization an instance of <see cref="PopupDismissedEventArgs"/>.
	/// </summary>
	/// <param name="result">
	/// The result of the popup.
	/// </param>
	/// <param name="isLightDismissed">
	/// If the popup was dismissed via light dismiss.
	/// </param>
	public PopupDismissedEventArgs(object? result, bool isLightDismissed)
	{
		Result = result;
		IsLightDismissed = isLightDismissed;
	}

	/// <summary>
	/// The resulting object to return.
	/// </summary>
	public object? Result { get; }

	/// <summary>
	/// Gets if the popup was dismissed via light dismiss.
	/// If true, then the user tapped outside the bounds of
	/// the popup (a light dismiss). If false, then the
	/// popup was dismissed by user action or code.
	/// </summary>
	public bool IsLightDismissed { get; }
}
