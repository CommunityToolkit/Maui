// Ignore Spelling: color

using System.ComponentModel;

namespace CommunityToolkit.Maui.Core;

/// <summary>Default Values for RatingView</summary>
static class RatingViewDefaults
{
	/// <summary>Default rating value.</summary>
	public const double DefaultRating = 0.0;

	/// <summary>Default view element read only.</summary>
	public const bool IsReadOnly = false;

	/// <summary>Default size of a rating item shape.</summary>
	public const double ItemShapeSize = 20.0;

	/// <summary>Default maximum value for the rating.</summary>
	public const int MaximumRating = 5;

	/// <summary>Maximum number of ratings.</summary>
	public const int MaximumRatingLimit = 10;

	/// <summary>Default border thickness for a rating shape.</summary>
	public const double ShapeBorderThickness = 1.0;

	/// <summary>Default spacing between ratings.</summary>
	public const double Spacing = 10.0;

	/// <summary>Default color for an empty rating.</summary>
	public static readonly Color EmptyShapeColor = Colors.Transparent;

	/// <summary>Default filled color for a rating.</summary>
	public static readonly Color FillColor = Colors.Yellow;

	/// <summary>Default rating item padding.</summary>
	public static readonly Thickness ShapePadding = new(0);

	/// <summary>Default rating shape.</summary>
	public const RatingViewShape Shape = RatingViewShape.Star;

	/// <summary>Default border color for a rating shape.</summary>
	public static readonly Color ShapeBorderColor = Colors.Grey;

	public const RatingViewFillOption RatingviewfillOption = RatingViewFillOption.Shape;
}