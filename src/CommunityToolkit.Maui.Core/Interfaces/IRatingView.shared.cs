// Ignore Spelling: Color

namespace CommunityToolkit.Maui.Core;

/// <summary>Provides functionality to device a rating view.</summary>
public interface IRatingView : IContentView
{
	/// <summary>The shape diameter in points.</summary>
	double ShapeDiameter { get; }

	/// <summary>The SVG path for a custom rating view shape.</summary>
	string? CustomShapePath { get; }

	/// <summary>The Rating View shape.</summary>
	/// <remarks><see cref="RatingViewShape.Custom"/> requires <see cref="CustomShapePath"/>.</remarks>
	RatingViewShape Shape { get; }

	/// <summary>The padding on each shape.</summary>
	Thickness ShapePadding { get; }

	/// <summary>The rating shape border thickness</summary>
	double ShapeBorderThickness { get; }

	/// <summary>Get a value indicating the shape border color.</summary>
	Color ShapeBorderColor { get; }

	/// <summary>Get a value indicating the background empty color.</summary>
	Color EmptyShapeColor { get; }

	/// <summary>Gets or sets the color of the fill used to display the current rating.</summary>
	Color FillColor { get; }
}