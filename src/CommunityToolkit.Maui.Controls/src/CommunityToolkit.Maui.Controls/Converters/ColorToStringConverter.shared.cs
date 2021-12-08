using CommunityToolkit.Maui.Extensions;
using Microsoft.Maui.Graphics;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="string"/>.
/// </summary>
public class ColorToRgbStringConverter : BaseConverterOneWay<Color, string>
{
	/// <inheritdoc/>
	public override string ConvertFrom(Color value) => value.ToRgbString();
}

/// <summary>
/// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="string"/>.
/// </summary>
public class ColorToRgbaStringConverter : BaseConverterOneWay<Color, string>
{
	/// <inheritdoc/>
	public override string ConvertFrom(Color value) => value.ToRgbaString();
}

/// <summary>
/// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="string"/> and virce-versa.
/// </summary>
public class ColorToHexRgbStringConverter : BaseConverter<Color, string>
{
	/// <inheritdoc/>
	public override string ConvertFrom(Color value) => value.ToHexRgbString();

	/// <inheritdoc/>
	public override Color ConvertBackTo(string value) => Color.FromArgb(value);
}

/// <summary>
/// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="string"/> and virce-versa.
/// </summary>
public class ColorToHexRgbaStringConverter : BaseConverter<Color, string>
{
	/// <inheritdoc/>
	public override string ConvertFrom(Color value) => value.ToHexRgbaString();

	/// <inheritdoc/>
	public override Color ConvertBackTo(string value) => Color.FromArgb(value);
}

/// <summary>
/// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="string"/>.
/// </summary>
public class ColorToCmykStringConverter : BaseConverterOneWay<Color, string>
{
	/// <inheritdoc/>
	public override string ConvertFrom(Color value) => value.ToCmykString();
}

/// <summary>
/// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="string"/>.
/// </summary>
public class ColorToCmykaStringConverter : BaseConverterOneWay<Color, string>
{
	/// <inheritdoc/>
	public override string ConvertFrom(Color value) => value.ToCmykaString();
}

/// <summary>
/// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="string"/>.
/// </summary>
public class ColorToHslStringConverter : BaseConverterOneWay<Color, string>
{
	/// <inheritdoc/>
	public override string ConvertFrom(Color value) => value.ToHslString();
}

/// <summary>
/// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="string"/>.
/// </summary>
public class ColorToHslaStringConverter : BaseConverterOneWay<Color, string>
{
	/// <inheritdoc/>
	public override string ConvertFrom(Color value) => value.ToHslaString();
}