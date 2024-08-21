namespace CommunityToolkit.Maui.Core.Primitives;

/// <summary>Visual options for rating view.</summary>
public class RatingViewOptions
{
	///<summary>Current rating of a rating view.</summary>
	public double? CurrentRating { get; set; }

	///<summary>Gets the parameter associated with the command.</summary>
	public object? RatingViewCommandParameter { get; set; }
}