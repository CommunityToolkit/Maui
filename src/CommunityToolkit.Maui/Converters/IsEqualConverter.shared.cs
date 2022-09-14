using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Checks whether the incoming value equals the provided parameter.
/// </summary>
public class IsEqualConverter : BaseConverterOneWay<object?, bool, object?>
{
	/// <inheritdoc/>
	public override bool DefaultConvertReturnValue { get; set; } = false;

	/// <summary>
	/// Checks whether the incoming value doesn't equal the provided parameter.
	/// </summary>
	/// <param name="value">The first object to compare.</param>
	/// <param name="parameter">The second object to compare.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>True if <paramref name="value"/> and <paramref name="parameter"/> are equal, False if they are not equal.</returns>
	public override bool ConvertFrom(object? value, object? parameter, CultureInfo? culture = null)
		=> EqualityComparer<object?>.Default.Equals(value, parameter);
}