namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Represents event data for when media state has changed.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MediaStateChangedEventArgs"/> class.
/// </remarks>
/// <param name="previousState">The previous state.</param>
/// <param name="newState">The new state.</param>
public sealed class MediaStateChangedEventArgs(MediaElementState previousState, MediaElementState newState) : EventArgs
{
	/// <summary>
	/// Gets the previous state that the <see cref="Core.IMediaElement"/> instance is transitioning from.
	/// </summary>
	public MediaElementState PreviousState { get; } = previousState;

	/// <summary>
	/// Gets the new state that the <see cref="Core.IMediaElement"/> instance is transitioning to.
	/// </summary>
	public MediaElementState NewState { get; } = newState;
}