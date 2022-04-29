namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Popup opened event arguments used when a popup is opened.
/// </summary>
public class PopupOpenedEventArgs : EventArgs
{
	/// <summary>
	/// Empty version of <see cref= "PopupOpenedEventArgs"/>.
	/// </summary>
	public new static PopupOpenedEventArgs Empty { get; } = new();
}