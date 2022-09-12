using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts the incoming value to a <see cref="bool"/> indicating whether or not the value is not null.
/// </summary>
public class IsNotNullConverter : BaseConverterOneWay<object?, bool>
{
	/// <inheritdoc/>
	public override bool DefaultConvertReturnValue { get; set; } = false;

	/// <summary>
	/// Converts the incoming object to a <see cref="bool"/> indicating whether or not the value is not null.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="culture">(Not Used)</param>
	/// <returns>A <see cref="bool"/> indicating if the incoming value is not null</returns>
	public override bool ConvertFrom(object? value, CultureInfo? culture = null) => value is not null;
}