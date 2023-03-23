namespace CommunityToolkit.Maui.Core.Primitives;

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
	public MediaStateChangedEventArgs(MediaElementState previousState, MediaElementState newState)
	{
		PreviousState = previousState;
		NewState = newState;
	}

	/// <summary>
	/// Gets the previous state that the <see cref="Core.IMediaElement"/> instance is transitioning from.
	/// </summary>
	public MediaElementState PreviousState { get; }

	/// <summary>
	/// Gets the new state that the <see cref="Core.IMediaElement"/> instance is transitioning to.
	/// </summary>
	public MediaElementState NewState { get; }
}