namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts the incoming value to a <see cref="bool"/> indicating whether or not the value is not null and not white space.
/// </summary>
public class IsStringNotNullOrWhiteSpaceConverter : BaseConverterOneWay<string?, bool>
{
	/// <summary>
	/// Converts the incoming string to a <see cref="bool"/> indicating whether or not the value is not null and not white space using string.IsNullOrWhiteSpace.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <returns>A <see cref="bool"/> indicating if the incoming value is not null and not white space.</returns>
	public override bool ConvertFrom(string? value) => !string.IsNullOrWhiteSpace(value);
}