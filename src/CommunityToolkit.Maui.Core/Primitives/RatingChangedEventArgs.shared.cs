namespace CommunityToolkit.Maui.Core;

/// <summary>Event args containing all contextual information related to rating changed event.</summary>
/// <param name="rating">The new rating value.</param>
public class RatingChangedEventArgs(double rating) : EventArgs
{
	/// <summary>Gets the rating for the rating changed event.</summary>
	public double Rating { get; } = rating;
}