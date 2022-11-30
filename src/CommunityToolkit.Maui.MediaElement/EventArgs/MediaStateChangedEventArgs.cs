namespace CommunityToolkit.Maui.MediaElement;

public class MediaStateChangedEventArgs : EventArgs
{
	/// <summary>
	/// The previous state that the <see cref="IMediaElement"/> instance is transitioning from.
	/// </summary>
	public MediaElementState PreviousState { get; }

	/// <summary>
	/// The new state that the <see cref="IMediaElement"/> instance is transitioning to.
	/// </summary>
	public MediaElementState NewState { get; }

	internal MediaStateChangedEventArgs(MediaElementState previousState, MediaElementState newState)
	{
		PreviousState = previousState;
		NewState = newState;
	}
}
