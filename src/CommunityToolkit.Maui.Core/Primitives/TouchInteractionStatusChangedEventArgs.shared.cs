namespace CommunityToolkit.Maui.Core;

/// <summary>
/// <see cref="TouchInteractionStatusChangedEventArgs"/>
/// </summary>
public class TouchInteractionStatusChangedEventArgs : EventArgs
{
	/// <summary>
	/// Constructor for <see cref="TouchInteractionStatusChangedEventArgs"/>
	/// </summary>
	/// <param name="touchInteractionStatus"></param>
	public TouchInteractionStatusChangedEventArgs(TouchInteractionStatus touchInteractionStatus)
		=> TouchInteractionStatus = touchInteractionStatus;

	/// <summary>
	/// Gets the current touch interaction status.
	/// </summary>
	public TouchInteractionStatus TouchInteractionStatus { get; }
}