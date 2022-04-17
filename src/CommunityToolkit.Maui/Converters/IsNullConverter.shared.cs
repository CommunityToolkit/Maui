using System.Collections;
using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts the incoming value to a <see cref="bool"/> indicating whether or not the value is null.
/// </summary>
public class IsNullConverter : BaseConverter<object?, bool>
{
	/// <summary>
	/// Converts the incoming object to a <see cref="bool"/> indicating whether or not the value is not null.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="culture">(Not Used)</param>
	/// <returns>A <see cref="bool"/> indicating if the incoming value is not null</returns>
	public override bool ConvertFrom(object? value, CultureInfo? culture = null) => value is null;

	/// <inheritdoc/>
	public override object? ConvertBackTo(bool value, CultureInfo? culture)
	{
		throw new NotImplementedException();
	}
}