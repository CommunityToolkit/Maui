namespace CommunityToolkit.Maui.Behaviors;

public class HoverStatusChangedEventArgs : EventArgs
{
	internal HoverStatusChangedEventArgs(HoverStatus status)
		=> Status = status;

	public HoverStatus Status { get; }
}