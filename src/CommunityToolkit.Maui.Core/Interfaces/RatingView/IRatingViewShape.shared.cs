// Ignore Spelling: color
using System.ComponentModel;

namespace CommunityToolkit.Maui.Core;

/// <summary>RatingView interface.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IRatingViewShape
{
	/// <summary>Gets a value indicating the rating item shape size.</summary>
	double ItemShapeSize { get; }

	/// <summary>Gets a value indicating the custom rating view shape path.</summary>
	string? CustomItemShape { get; }

	/// <summary>Gets a value indicating the Rating View shape.</summary>
	RatingViewShape ItemShape { get; }

	/// <summary>Gets a value indicating the Rating View item padding.</summary>
	Thickness ItemPadding { get; }

	/// <summary>Gets a value indicating the rating item shape border thickness</summary>
	double ShapeBorderThickness { get; }

	/// <summary>Get a value indicating the rating item shape border color.</summary>
	Color ShapeBorderColor { get; }

	/// <summary>Get a value indicating the rating item background empty color.</summary>
	Color EmptyColor { get; }

	/// <summary>Get a value indicating the rating item background filled color.</summary>
	Color FilledColor { get; }
}

/// <summary>Rating view shape enumerator.</summary>
public enum RatingViewShape
{
	/// <summary>A star rating shape.</summary>
	Star,

	/// <summary>A heart rating shape.</summary>
	Heart,

	/// <summary>A circle rating shape.</summary>
	Circle,

	/// <summary>A like/thumbs up rating shape.</summary>
	Like,

	/// <summary>A dislike/thumbs down rating shape.</summary>
	Dislike,

	/// <summary>A custom rating shape.</summary>
	Custom
}