namespace CommunityToolkit.Maui.Core;

/// <summary>Default Values for RatingView</summary>
static class RatingViewDefaults
{
	/// <summary>Default rating value.</summary>
	public const double Rating = 0.0;

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

	/// <summary>Default rating shape.</summary>
	public const RatingViewShape Shape = RatingViewShape.Star;

	/// <summary>Default Fill When Tapped</summary>
	public const RatingViewFillOption FillWhenTapped = RatingViewFillOption.Shape;

	/// <summary>Default Fill Option</summary>
	public const RatingViewFillOption FillOption = RatingViewFillOption.Shape;

	/// <summary>Default color for an empty rating.</summary>
	public static Color EmptyShapeColor { get; } = Colors.Transparent;

	/// <summary>Default filled color for a rating.</summary>
	public static Color FillColor { get; } = Colors.Yellow;

	/// <summary>Default rating item padding.</summary>
	public static Thickness ShapePadding { get; } = new(0);

	/// <summary>Default border color for a rating shape.</summary>
	public static Color ShapeBorderColor { get; } = Colors.Grey;
}