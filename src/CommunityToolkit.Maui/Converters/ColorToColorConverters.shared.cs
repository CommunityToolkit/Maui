using System.Globalization;
using CommunityToolkit.Maui.Core.Extensions;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="Color"/>.
/// </summary>
public class ColorToBlackOrWhiteConverter : BaseConverterOneWay<Color, Color>
{
	/// <inheritdoc/>
	public override Color ConvertFrom(Color value, Type targetType, object? parameter, CultureInfo? culture)
	{
		ArgumentNullException.ThrowIfNull(value);
		return value.ToBlackOrWhite();
	}
}

/// <summary>
/// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="Color"/>.
/// </summary>
public class ColorToColorForTextConverter : BaseConverterOneWay<Color, Color>
{
	/// <inheritdoc/>
	public override Color ConvertFrom(Color value, Type targetType, object? parameter, CultureInfo? culture)
	{
		ArgumentNullException.ThrowIfNull(value);
		return value.ToBlackOrWhiteForText();
	}
}

/// <summary>
/// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="Color"/>.
/// </summary>
public class ColorToGrayScaleColorConverter : BaseConverterOneWay<Color, Color>
{
	/// <inheritdoc/>
	public override Color ConvertFrom(Color value, Type targetType, object? parameter, CultureInfo? culture)
	{
		ArgumentNullException.ThrowIfNull(value);
		return value.ToGrayScale();
	}
}

/// <summary>
/// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="Color"/>.
/// </summary>
public class ColorToInverseColorConverter : BaseConverterOneWay<Color, Color>
{
	/// <inheritdoc/>
	public override Color ConvertFrom(Color value, Type targetType, object? parameter, CultureInfo? culture)
	{
		ArgumentNullException.ThrowIfNull(value);
		return value.ToInverseColor();
	}
}