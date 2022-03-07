using System.Collections;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts the incoming value to a <see cref="bool"/> indicating whether or not the value is not null and not empty.
/// </summary>
public class IsListNotNullOrEmptyConverter : BaseConverterOneWay<IEnumerable?, bool>
{
	/// <summary>
	/// Converts the incoming value to a <see cref="bool"/> indicating whether or not the value is not null and not empty.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	public override bool ConvertFrom(IEnumerable? value) =>
		!IsListNullOrEmptyConverter.IsListNullOrEmpty(value);
}