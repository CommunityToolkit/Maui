namespace CommunityToolkit.Maui.MediaView;

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
	public MediaStateChangedEventArgs(MediaViewState previousState,
		MediaViewState newState)
	{
		PreviousState = previousState;
		NewState = newState;
	}

	/// <summary>
	/// Gets the previous state that the <see cref="IMediaView"/> instance is transitioning from.
	/// </summary>
	public MediaViewState PreviousState { get; }

	/// <summary>
	/// Gets the new state that the <see cref="IMediaView"/> instance is transitioning to.
	/// </summary>
	public MediaViewState NewState { get; }
}
