namespace CommunityToolkit.Maui.Core.Primitives;

/// <summary>
/// Represents event data for when media position has changed.
/// </summary>
public sealed class MediaPositionChangedEventArgs : EventArgs
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MediaPositionChangedEventArgs"/> class.
	/// </summary>
	/// <param name="position">The position associated to this event.</param>
	public MediaPositionChangedEventArgs(TimeSpan position)
	{
		Position = position;
	}

	/// <summary>
	/// Gets the position the media has progressed to.
	/// </summary>
	public TimeSpan Position { get; }
}