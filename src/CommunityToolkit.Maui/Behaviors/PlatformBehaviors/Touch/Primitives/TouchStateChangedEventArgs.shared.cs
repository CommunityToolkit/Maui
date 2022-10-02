namespace CommunityToolkit.Maui.Behaviors;

public class TouchStateChangedEventArgs : EventArgs
{
	internal TouchStateChangedEventArgs(TouchState state)
		=> State = state;

	public TouchState State { get; }
}