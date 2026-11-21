namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Represents event data for when the screen state of the media element has changed.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ScreenStateChangedEventArgs"/> class.
/// </remarks>
/// <param name="previousState">The previous screen state.</param>
/// <param name="newState">The new screen state.</param>
public sealed class ScreenStateChangedEventArgs(MediaElementScreenState previousState, MediaElementScreenState newState) : EventArgs
{
	/// <summary>
	/// Gets the previous screen state that the <see cref="IMediaElement"/> instance is transitioning from.
	/// </summary>
	public MediaElementScreenState PreviousState { get; } = previousState;

	/// <summary>
	/// Gets the new screen state that the <see cref="IMediaElement"/> instance is transitioning to.
	/// </summary>
	public MediaElementScreenState NewState { get; } = newState;
}