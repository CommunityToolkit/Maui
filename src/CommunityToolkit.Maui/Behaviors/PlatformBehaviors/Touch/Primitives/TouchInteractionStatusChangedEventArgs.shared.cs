namespace CommunityToolkit.Maui.Behaviors;

public class TouchInteractionStatusChangedEventArgs : EventArgs
{
	internal TouchInteractionStatusChangedEventArgs(TouchInteractionStatus touchInteractionStatus)
		=> TouchInteractionStatus = touchInteractionStatus;

	public TouchInteractionStatus TouchInteractionStatus { get; }
}