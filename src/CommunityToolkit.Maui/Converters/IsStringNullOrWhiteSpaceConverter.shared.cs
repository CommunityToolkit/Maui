namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts the incoming value to a <see cref="bool"/> indicating whether or not the value is null or white space.
/// </summary>
public class IsStringNullOrWhiteSpaceConverter : BaseConverterOneWay<string?, bool>
{
	/// <summary>
	/// Converts the incoming string to a <see cref="bool"/> indicating whether or not the string is null or white space using string.IsNullOrWhiteSpace.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <returns>A <see cref="bool"/> indicating if the incoming value is null or white space.</returns>
	public override bool ConvertFrom(string? value) => string.IsNullOrWhiteSpace(value);
}