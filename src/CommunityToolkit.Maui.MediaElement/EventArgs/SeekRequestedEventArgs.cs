namespace CommunityToolkit.Maui.MediaElement;

/// <summary>
/// Represents event data for when a seek operation is requested on media.
/// </summary>
class MediaSeekRequestedEventArgs : EventArgs
{
	/// <summary>
	/// Gets the requested position to seek to.
	/// </summary>
	public TimeSpan RequestedPosition { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="MediaSeekRequestedEventArgs"/> class.
	/// </summary>
	/// <param name="requestedPosition">The requested position to seek to.</param>
	public MediaSeekRequestedEventArgs(TimeSpan requestedPosition)
	{
		RequestedPosition = requestedPosition;
	}
}
