using CommunityToolkit.Maui.Core.Extensions;
using Microsoft.Maui.Graphics;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="Color"/>.
/// </summary>
public class ColorToBlackOrWhiteConverter : BaseConverterOneWay<Color, Color>
{
	/// <inheritdoc/>
	public override Color ConvertFrom(Color value) => value.ToBlackOrWhite();
}

/// <summary>
/// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="Color"/>.
/// </summary>
public class ColorToColorForTextConverter : BaseConverterOneWay<Color, Color>
{
	/// <inheritdoc/>
	public override Color ConvertFrom(Color value) => value.ToBlackOrWhiteForText();
}

/// <summary>
/// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="Color"/>.
/// </summary>
public class ColorToGrayScaleColorConverter : BaseConverterOneWay<Color, Color>
{
	/// <inheritdoc/>
	public override Color ConvertFrom(Color value) => value.ToGrayScale();
}

/// <summary>
/// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="Color"/>.
/// </summary>
public class ColorToInverseColorConverter : BaseConverterOneWay<Color, Color>
{
	/// <inheritdoc/>
	public override Color ConvertFrom(Color value) => value.ToInverseColor();
}