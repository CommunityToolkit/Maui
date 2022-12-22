namespace CommunityToolkit.Maui.MediaElement;

class SeekRequestedEventArgs : EventArgs
{
	public TimeSpan RequestedPosition { get; }

	public SeekRequestedEventArgs(TimeSpan requestedPosition)
	{
		RequestedPosition = requestedPosition;
	}
}
