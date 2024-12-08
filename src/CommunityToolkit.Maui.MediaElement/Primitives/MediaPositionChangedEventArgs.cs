namespace CommunityToolkit.Maui.Core.Primitives;

/// <summary>
/// Represents event data for when media position has changed.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MediaPositionChangedEventArgs"/> class.
/// </remarks>
/// <param name="position">The position associated to this event.</param>
public sealed class MediaPositionChangedEventArgs(TimeSpan position) : EventArgs
{
	/// <summary>
	/// Gets the position the media has progressed to.
	/// </summary>
	public TimeSpan Position { get; } = position;
}