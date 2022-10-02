namespace CommunityToolkit.Maui.Behaviors;

public class TouchStatusChangedEventArgs : EventArgs
{
	internal TouchStatusChangedEventArgs(TouchStatus status)
		=> Status = status;

	public TouchStatus Status { get; }
}