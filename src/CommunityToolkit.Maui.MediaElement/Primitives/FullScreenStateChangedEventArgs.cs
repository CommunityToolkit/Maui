namespace CommunityToolkit.Maui.Primitives;

/// <summary>
/// Event data for when the full screen state of the media element has changed.
/// </summary>
public sealed class FullScreenStateChangedEventArgs
{
	/// <summary>
	/// Initializes a new instance of the <see cref="FullScreenStateChangedEventArgs"/> class.
	/// </summary>
	/// <param name="previousState"></param>
	/// <param name="newState"></param>
	public FullScreenStateChangedEventArgs(MediaElementScreenState previousState, MediaElementScreenState newState)
	{
		PreviousState = previousState;
		NewState = newState;
	}

	/// <summary>
	/// Gets the previous state that the <see cref="Core.IMediaElement"/> instance is transitioning from.
	/// </summary>
	public MediaElementScreenState PreviousState { get; }

	/// <summary>
	/// Gets the new state that the <see cref="Core.IMediaElement"/> instance is transitioning to.
	/// </summary>
	public MediaElementScreenState NewState { get; }
}
