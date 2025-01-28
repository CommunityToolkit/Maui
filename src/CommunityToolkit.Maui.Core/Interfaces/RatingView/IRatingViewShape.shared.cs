// Ignore Spelling: color
using System.ComponentModel;
using CommunityToolkit.Maui.Core.Primitives;

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
	RatingViewShapes ItemShape { get; }

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