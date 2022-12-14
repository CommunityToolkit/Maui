namespace CommunityToolkit.Maui.MediaElement;

public sealed class MediaStateChangedEventArgs : EventArgs
{
	/// <summary>
	/// Gets the previous state that the <see cref="IMediaElement"/> instance is transitioning from.
	/// </summary>
	public MediaElementState PreviousState { get; }

	/// <summary>
	/// Gets the new state that the <see cref="IMediaElement"/> instance is transitioning to.
	/// </summary>
	public MediaElementState NewState { get; }

	public MediaStateChangedEventArgs(MediaElementState previousState,
		MediaElementState newState)
	{
		PreviousState = previousState;
		NewState = newState;
	}
}
