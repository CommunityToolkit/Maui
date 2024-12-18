using System.Globalization;

using CommunityToolkit.Maui.Core.Extensions;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="string"/>.
/// </summary>
[AcceptEmptyServiceProvider]
public partial class ColorToRgbStringConverter : BaseConverter<Color, string>
{
	/// <inheritdoc/>
	public override string DefaultConvertReturnValue { get; set; } = string.Empty;

	/// <inheritdoc/>
	public override Color DefaultConvertBackReturnValue { get; set; } = Colors.Transparent;

	/// <inheritdoc/>
	public override Color ConvertBackTo(string value, CultureInfo? culture)
	{
		if (Color.TryParse(value, out Color color))
		{
			return color;
		}

		return DefaultConvertBackReturnValue;
	}

	/// <inheritdoc/>
	public override string ConvertFrom(Color value, CultureInfo? culture = null)
	{
		ArgumentNullException.ThrowIfNull(value);
		return value.ToRgbString();
	}
}

/// <summary>
/// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="string"/>.
/// </summary>
[AcceptEmptyServiceProvider]
public partial class ColorToRgbaStringConverter : BaseConverter<Color, string>
{
	/// <inheritdoc/>
	public override string DefaultConvertReturnValue { get; set; } = string.Empty;

	/// <inheritdoc/>
	public override Color DefaultConvertBackReturnValue { get; set; } = Colors.Transparent;

	/// <inheritdoc/>
	public override Color ConvertBackTo(string value, CultureInfo? culture)
	{
		return Color.TryParse(value, out var color) ? color : DefaultConvertBackReturnValue;
	}

	/// <inheritdoc/>
	public override string ConvertFrom(Color value, CultureInfo? culture = null)
	{
		ArgumentNullException.ThrowIfNull(value);
		return value.ToRgbaString(culture);
	}
}

/// <summary>
/// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="string"/> and vice-versa.
/// </summary>
[AcceptEmptyServiceProvider]
public partial class ColorToHexRgbStringConverter : BaseConverter<Color, string>
{
	/// <inheritdoc/>
	public override string DefaultConvertReturnValue { get; set; } = string.Empty;

	/// <inheritdoc/>
	public override Color DefaultConvertBackReturnValue { get; set; } = Colors.Transparent;

	/// <inheritdoc/>
	public override string ConvertFrom(Color value, CultureInfo? culture = null)
	{
		ArgumentNullException.ThrowIfNull(value);
		return value.ToRgbaHex(false);
	}

	/// <inheritdoc/>
	public override Color ConvertBackTo(string value, CultureInfo? culture = null)
	{
		ArgumentNullException.ThrowIfNull(value);
		return Color.FromRgba(value);
	}
}

/// <summary>
/// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="string"/> and vice-versa.
/// </summary>
[AcceptEmptyServiceProvider]
public partial class ColorToHexRgbaStringConverter : BaseConverter<Color, string>
{
	/// <inheritdoc/>
	public override string DefaultConvertReturnValue { get; set; } = string.Empty;

	/// <inheritdoc/>
	public override Color DefaultConvertBackReturnValue { get; set; } = Colors.Transparent;

	/// <inheritdoc/>
	public override string ConvertFrom(Color value, CultureInfo? culture = null)
	{
		ArgumentNullException.ThrowIfNull(value);
		return value.ToRgbaHex(true);
	}

	/// <inheritdoc/>
	public override Color ConvertBackTo(string value, CultureInfo? culture = null)
	{
		ArgumentNullException.ThrowIfNull(value);
		return Color.FromRgba(value);
	}
}

/// <summary>
/// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="string"/> and vice-versa.
/// </summary>
[AcceptEmptyServiceProvider]
public partial class ColorToHexArgbStringConverter : BaseConverter<Color, string>
{
	/// <inheritdoc/>
	public override string DefaultConvertReturnValue { get; set; } = string.Empty;

	/// <inheritdoc/>
	public override Color DefaultConvertBackReturnValue { get; set; } = Colors.Transparent;

	/// <inheritdoc/>
	public override string ConvertFrom(Color value, CultureInfo? culture = null)
	{
		ArgumentNullException.ThrowIfNull(value);
		return value.ToArgbHex(true);
	}

	/// <inheritdoc/>
	public override Color ConvertBackTo(string value, CultureInfo? culture = null)
	{
		ArgumentNullException.ThrowIfNull(value);
		return Color.FromArgb(value);
	}
}

/// <summary>
/// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="string"/>.
/// </summary>
[AcceptEmptyServiceProvider]
public partial class ColorToCmykStringConverter : BaseConverterOneWay<Color, string>
{
	/// <inheritdoc/>
	public override string DefaultConvertReturnValue { get; set; } = string.Empty;

	/// <inheritdoc/>
	public override string ConvertFrom(Color value, CultureInfo? culture = null)
	{
		ArgumentNullException.ThrowIfNull(value);
		return value.ToCmykString();
	}
}

/// <summary>
/// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="string"/>.
/// </summary>
[AcceptEmptyServiceProvider]
public partial class ColorToCmykaStringConverter : BaseConverterOneWay<Color, string>
{
	/// <inheritdoc/>
	public override string DefaultConvertReturnValue { get; set; } = string.Empty;

	/// <inheritdoc/>
	public override string ConvertFrom(Color value, CultureInfo? culture = null)
	{
		ArgumentNullException.ThrowIfNull(value);
		return value.ToCmykaString(culture);
	}
}

/// <summary>
/// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="string"/>.
/// </summary>
[AcceptEmptyServiceProvider]
public partial class ColorToHslStringConverter : BaseConverterOneWay<Color, string>
{
	/// <inheritdoc/>
	public override string DefaultConvertReturnValue { get; set; } = string.Empty;

	/// <inheritdoc/>
	public override string ConvertFrom(Color value, CultureInfo? culture = null)
	{
		ArgumentNullException.ThrowIfNull(value);
		return value.ToHslString();
	}
}

/// <summary>
/// Converts the incoming value from <see cref="Color"/> and returns the object of a type <see cref="string"/>.
/// </summary>
[AcceptEmptyServiceProvider]
public partial class ColorToHslaStringConverter : BaseConverterOneWay<Color, string>
{
	/// <inheritdoc/>
	public override string DefaultConvertReturnValue { get; set; } = string.Empty;

	/// <inheritdoc/>
	public override string ConvertFrom(Color value, CultureInfo? culture = null)
	{
		ArgumentNullException.ThrowIfNull(value);
		return value.ToHslaString(culture);
	}
}