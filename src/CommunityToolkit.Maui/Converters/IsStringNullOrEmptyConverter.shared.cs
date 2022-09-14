using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts the incoming value to a <see cref="bool"/> indicating whether or not the value is null or empty.
/// </summary>
public class IsStringNullOrEmptyConverter : BaseConverterOneWay<string?, bool>
{
	/// <inheritdoc/>
	public override bool DefaultConvertReturnValue { get; set; } = false;

	/// <summary>
	/// Converts the incoming string to a <see cref="bool"/> indicating whether or not the string is null or empty using string.IsNullOrEmpty.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="culture">(Not Used)</param>
	/// <returns>A <see cref="bool"/> indicating if the incoming value is null or empty.</returns>
	public override bool ConvertFrom(string? value, CultureInfo? culture = null)
		=> string.IsNullOrEmpty(value);
}