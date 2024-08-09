namespace CommunityToolkit.Maui.Primitives;
/// <summary>
/// A Class that contains the event arguments for the notification changed event.
/// </summary>
sealed class NotificationChangedEventArgs : EventArgs
{
	/// <summary>
	/// A Constructor that initializes the class with the action.
	/// </summary>
	/// <param name="action"></param>
	public NotificationChangedEventArgs(string action)
	{
		Action = action;
	}
	/// <summary>
	/// A Property that contains the action.
	/// </summary>
	public string Action { get; }
}
