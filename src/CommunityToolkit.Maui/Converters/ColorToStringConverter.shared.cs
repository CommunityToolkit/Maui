using System.Diagnostics.CodeAnalysis;
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
		if (Color.TryParse(value, out var color))
		{
			return color;
		}

		// A comma-decimal culture writes the alpha as "0,5" ("RGBA(255,0,0,0,5)" in de-DE), which the invariant Color.TryParse rejects.
		// A null culture reads CurrentCulture here so strings that v17.0.0 wrote for a null culture still convert back.
		return TryParseCultureAwareRgba(value, culture ?? CultureInfo.CurrentCulture, out color)
			? color
			: DefaultConvertBackReturnValue;
	}

	/// <summary>
	/// Converts <paramref name="value"/> to its RGBA string.
	/// </summary>
	/// <param name="value">The <see cref="Color"/> to convert.</param>
	/// <param name="culture">The culture that formats the numbers. In a binding, .NET MAUI passes <c>ConverterCulture</c>, or <see cref="CultureInfo.CurrentUICulture"/> when it is not set. <see langword="null"/> or <see cref="CultureInfo.InvariantCulture"/> gives the culture-invariant format, for example <c>RGBA(255,0,0,0.5)</c>.</param>
	/// <returns>The <see cref="string"/> representation of <paramref name="value"/>, for example <c>RGBA(255,0,0,0.5)</c> for <see cref="Colors.Red"/> with an alpha of 0.5 where the format has an alpha.</returns>
	public override string ConvertFrom(Color value, CultureInfo? culture = null)
	{
		ArgumentNullException.ThrowIfNull(value);
		return value.ToRgbaString(culture);
	}

	static bool TryParseCultureAwareRgba(string? value, CultureInfo culture, [NotNullWhen(true)] out Color? color)
	{
		color = null;

		const string prefix = "RGBA(";
		var text = value?.Trim();
		if (text is null
			|| text.Length <= prefix.Length
			|| !text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
			|| text[^1] is not ')')
		{
			return false;
		}

		// The alpha's decimal separator can be a comma, so only the first three commas separate components.
		var components = text[prefix.Length..^1].Split(',', 4);
		if (components.Length is not 4
			|| !byte.TryParse(components[0], NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, CultureInfo.InvariantCulture, out var red)
			|| !byte.TryParse(components[1], NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, CultureInfo.InvariantCulture, out var green)
			|| !byte.TryParse(components[2], NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, CultureInfo.InvariantCulture, out var blue)
			|| !float.TryParse(components[3], NumberStyles.Float, culture, out var alpha)
			|| float.IsNaN(alpha))
		{
			return false;
		}

		color = Color.FromRgba(red / 255f, green / 255f, blue / 255f, Math.Clamp(alpha, 0f, 1f));
		return true;
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

	/// <summary>
	/// Converts <paramref name="value"/> to its CMYK string.
	/// </summary>
	/// <param name="value">The <see cref="Color"/> to convert.</param>
	/// <param name="culture">The culture that formats the numbers. In a binding, .NET MAUI passes <c>ConverterCulture</c>, or <see cref="CultureInfo.CurrentUICulture"/> when it is not set. <see langword="null"/> or <see cref="CultureInfo.InvariantCulture"/> gives the culture-invariant format, for example <c>CMYK(0%,100%,100%,0%)</c>.</param>
	/// <returns>The <see cref="string"/> representation of <paramref name="value"/>, for example <c>CMYK(0%,100%,100%,0%)</c> for <see cref="Colors.Red"/>.</returns>
	public override string ConvertFrom(Color value, CultureInfo? culture = null)
	{
		ArgumentNullException.ThrowIfNull(value);
		return value.ToCmykString(culture);
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

	/// <summary>
	/// Converts <paramref name="value"/> to its CMYKA string.
	/// </summary>
	/// <param name="value">The <see cref="Color"/> to convert.</param>
	/// <param name="culture">The culture that formats the numbers. In a binding, .NET MAUI passes <c>ConverterCulture</c>, or <see cref="CultureInfo.CurrentUICulture"/> when it is not set. <see langword="null"/> or <see cref="CultureInfo.InvariantCulture"/> gives the culture-invariant format, for example <c>CMYKA(0%,100%,100%,0%,0.5)</c>.</param>
	/// <returns>The <see cref="string"/> representation of <paramref name="value"/>, for example <c>CMYKA(0%,100%,100%,0%,0.5)</c> for <see cref="Colors.Red"/> with an alpha of 0.5 where the format has an alpha.</returns>
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

	/// <summary>
	/// Converts <paramref name="value"/> to its HSL string.
	/// </summary>
	/// <param name="value">The <see cref="Color"/> to convert.</param>
	/// <param name="culture">The culture that formats the numbers. In a binding, .NET MAUI passes <c>ConverterCulture</c>, or <see cref="CultureInfo.CurrentUICulture"/> when it is not set. <see langword="null"/> or <see cref="CultureInfo.InvariantCulture"/> gives the culture-invariant format, for example <c>HSL(360,100%,50%)</c>.</param>
	/// <returns>The <see cref="string"/> representation of <paramref name="value"/>, for example <c>HSL(360,100%,50%)</c> for <see cref="Colors.Red"/>.</returns>
	public override string ConvertFrom(Color value, CultureInfo? culture = null)
	{
		ArgumentNullException.ThrowIfNull(value);
		return value.ToHslString(culture);
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

	/// <summary>
	/// Converts <paramref name="value"/> to its HSLA string.
	/// </summary>
	/// <param name="value">The <see cref="Color"/> to convert.</param>
	/// <param name="culture">The culture that formats the numbers. In a binding, .NET MAUI passes <c>ConverterCulture</c>, or <see cref="CultureInfo.CurrentUICulture"/> when it is not set. <see langword="null"/> or <see cref="CultureInfo.InvariantCulture"/> gives the culture-invariant format, for example <c>HSLA(360,100%,50%,0.5)</c>.</param>
	/// <returns>The <see cref="string"/> representation of <paramref name="value"/>, for example <c>HSLA(360,100%,50%,0.5)</c> for <see cref="Colors.Red"/> with an alpha of 0.5 where the format has an alpha.</returns>
	public override string ConvertFrom(Color value, CultureInfo? culture = null)
	{
		ArgumentNullException.ThrowIfNull(value);
		return value.ToHslaString(culture);
	}
}