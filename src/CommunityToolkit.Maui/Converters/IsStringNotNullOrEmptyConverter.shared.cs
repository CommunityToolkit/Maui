using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts the incoming value to a <see cref="bool"/> indicating whether or not the value is not null and not empty.
/// </summary>
public class IsStringNotNullOrEmptyConverter : BaseConverterOneWay<string?, bool>
{
	/// <summary>
	/// Converts the incoming string to a <see cref="bool"/> indicating whether or not the value is not null and not empty using string.IsNullOrEmpty.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <param name="culture">(Not Used)</param>
	/// <returns>Whether the the string is not null or empty</returns>
	public override bool ConvertFrom(string? value, CultureInfo? culture)
		=> !string.IsNullOrEmpty(value);
}