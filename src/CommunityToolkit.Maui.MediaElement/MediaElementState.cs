namespace CommunityToolkit.Maui.MediaElement;

public enum MediaElementState
{
	/// <summary>The media source has been closed.</summary>
	Closed,

	/// <summary>The media source is currently being opened.</summary>
	Opening,

	/// <summary>The media source is being buffered for playing.</summary>
	Buffering,

	/// <summary>The media is currently being played.</summary>
	Playing,

	/// <summary>The media playing has been paused.</summary>
	Paused,

	/// <summary>The media playing has been stopped.</summary>
	Stopped,
}
