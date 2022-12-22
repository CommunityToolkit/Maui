namespace CommunityToolkit.Maui.MediaElement;

class SeekRequestedEventArgs
{
	public TimeSpan RequestedPosition { get; }

	public SeekRequestedEventArgs(TimeSpan requestedPosition)
	{
		RequestedPosition = requestedPosition;
	}
}
