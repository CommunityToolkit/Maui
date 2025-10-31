﻿namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Event data for when the full screen state of the media element has changed.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="FullScreenStateChangedEventArgs"/> class.
/// </remarks>
/// <param name="previousState"></param>
/// <param name="newState"></param>
public sealed class FullScreenStateChangedEventArgs(MediaElementScreenState previousState, MediaElementScreenState newState) : EventArgs
{

	/// <summary>
	/// Gets the previous state that the <see cref="Core.IMediaElement"/> instance is transitioning from.
	/// </summary>
	public MediaElementScreenState PreviousState { get; } = previousState;

	/// <summary>
	/// Gets the new state that the <see cref="Core.IMediaElement"/> instance is transitioning to.
	/// </summary>
	public MediaElementScreenState NewState { get; } = newState;
}
