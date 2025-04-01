// Ignore Spelling: color

using System.ComponentModel;

namespace CommunityToolkit.Maui.Core;

/// <summary>Default Values for RatingView</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class RatingViewDefaults
{
	/// <summary>Default rating value.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const double DefaultRating = 0.0;

	/// <summary>Default view element read only.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const bool IsReadOnly = false;

	/// <summary>Default size of a rating item shape.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const double ItemShapeSize = 20.0;

	/// <summary>Default maximum value for the rating.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const int MaximumRating = 5;

	/// <summary>Maximum number of ratings.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const int MaximumRatingLimit = 10;

	/// <summary>Default border thickness for a rating shape.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const double ShapeBorderThickness = 1.0;

	/// <summary>Default spacing between ratings.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const double Spacing = 10.0;

	/// <summary>Default color for an empty rating.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Color EmptyShapeColor { get; } = Colors.Transparent;

	/// <summary>Default filled color for a rating.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Color FillColor { get; } = Colors.Yellow;

	/// <summary>Default rating item padding.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Thickness ShapePadding { get; } = new(0);

	/// <summary>Default rating shape.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static RatingViewShape Shape { get; } = RatingViewShape.Star;

	/// <summary>Default border color for a rating shape.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Color ShapeBorderColor { get; } = Colors.Grey;
}