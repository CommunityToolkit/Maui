namespace CommunityToolkit.Maui.MediaPlayer;

/// <summary>
/// Represents event data for when media state has changed.
/// </summary>
public sealed class MediaStateChangedEventArgs : EventArgs
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MediaStateChangedEventArgs"/> class.
	/// </summary>
	/// <param name="previousState">The previous state.</param>
	/// <param name="newState">The new state.</param>
	public MediaStateChangedEventArgs(MediaPlayerState previousState,
		MediaPlayerState newState)
	{
		PreviousState = previousState;
		NewState = newState;
	}

	/// <summary>
	/// Gets the previous state that the <see cref="Core.IMediaPlayer"/> instance is transitioning from.
	/// </summary>
	public MediaPlayerState PreviousState { get; }

	/// <summary>
	/// Gets the new state that the <see cref="Core.IMediaPlayer"/> instance is transitioning to.
	/// </summary>
	public MediaPlayerState NewState { get; }
}
