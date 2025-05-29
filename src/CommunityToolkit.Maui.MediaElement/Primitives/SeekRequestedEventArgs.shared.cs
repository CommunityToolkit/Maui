namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Represents event data for when a seek operation is requested on media.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MediaSeekRequestedEventArgs"/> class.
/// </remarks>
/// <param name="requestedPosition">The requested position to seek to.</param>
sealed class MediaSeekRequestedEventArgs(TimeSpan requestedPosition) : EventArgs
{
	/// <summary>
	/// Gets the requested position to seek to.
	/// </summary>
	public TimeSpan RequestedPosition { get; } = requestedPosition;
}