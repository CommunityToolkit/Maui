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

	/// <summary>Default enabled rating view.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const bool IsEnabled = true;

	/// <summary>Default maximum value for the rating.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const byte MaximumRating = 5;

	/// <summary>Maximum number of ratings.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const byte MaximumRatings = 25;

	/// <summary>Default border thickness for a rating shape.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const double ShapeBorderThickness = 1.0;

	/// <summary>Default size of a rating item shape.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const double ItemShapeSize = 20.0;

	/// <summary>Default spacing between ratings.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const double Spacing = 10.0;

	/// <summary>Default background color for an empty rating.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Color EmptyBackgroundColor { get; } = Colors.Transparent;

	/// <summary>Default filled color for a rating.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Color FilledBackgroundColor { get; } = Colors.Yellow;

	/// <summary>Default rating shape.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static RatingViewShape Shape { get; } = RatingViewShape.Star;

	/// <summary>Default border color for a rating shape.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Color ShapeBorderColor { get; } = Colors.Grey;

	/// <summary>Default rating item padding.</summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Thickness ItemPadding { get; } = new(0);
}