using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts the incoming value to a <see cref="bool"/> indicating whether or not the value is not null and not white space.
/// </summary>
public class IsStringNotNullOrWhiteSpaceConverter : BaseConverterOneWay<string?, bool>
{
	/// <inheritdoc/>
	public override bool DefaultConvertReturnValue { get; set; } = false;

	/// <summary>
	/// Converts the incoming string to a <see cref="bool"/> indicating whether or not the value is not null and not white space using string.IsNullOrWhiteSpace.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="culture">(Not Used)</param>
	/// <returns>Whether the the list is not null or empty</returns>
	/// <returns>A <see cref="bool"/> indicating if the incoming value is not null and not white space.</returns>
	public override bool ConvertFrom(string? value, CultureInfo? culture = null)
		=> !string.IsNullOrWhiteSpace(value);
}