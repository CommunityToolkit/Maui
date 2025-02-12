// Ignore Spelling: Color

namespace CommunityToolkit.Maui.Core;

/// <summary>Provides functionality to device a rating view.</summary>
public interface IRatingView : IContentView
{
    /// <summary>The rating item shape diameter in points.</summary>
    double ShapeDiameter { get; }

    /// <summary>The custom rating view shape path.</summary>
    string? CustomShapePath { get; }

    /// <summary>The Rating View shape.</summary>
    RatingViewShape Shape { get; }

    /// <summary>The padding on each shape.</summary>
    Thickness ShapePadding { get; }

    /// <summary>The rating shape border thickness</summary>
    double ShapeBorderThickness { get; }

    /// <summary>Get a value indicating the shape border color.</summary>
    Color ShapeBorderColor { get; }

    /// <summary>Get a value indicating the background empty color.</summary>
    Color EmptyColor { get; }

    /// <summary>Get a value indicating the background filled color.</summary>
    Color FilledColor { get; }
}