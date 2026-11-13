namespace CommunityToolkit.Maui.Core;

/// <summary>
/// <see cref="TouchInteractionStatusChangedEventArgs"/>
/// </summary>
public class TouchInteractionStatusChangedEventArgs(TouchInteractionStatus touchInteractionStatus) : EventArgs
{
	/// <summary>
	/// Gets the current touch interaction status.
	/// </summary>
	public TouchInteractionStatus TouchInteractionStatus { get; } = touchInteractionStatus;
}