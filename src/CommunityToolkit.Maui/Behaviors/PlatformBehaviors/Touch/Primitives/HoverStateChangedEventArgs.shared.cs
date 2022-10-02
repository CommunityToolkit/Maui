namespace CommunityToolkit.Maui.Behaviors;

public class HoverStateChangedEventArgs : EventArgs
{
	internal HoverStateChangedEventArgs(HoverState state)
		=> State = state;

	public HoverState State { get; }
}