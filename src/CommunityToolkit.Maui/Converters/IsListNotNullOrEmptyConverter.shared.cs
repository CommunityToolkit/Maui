using System.Collections;
using System.Globalization;

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
	/// <param name="targetType">(Not Used)</param>
	/// <param name="parameter">(Not Used)</param>
	/// <param name="culture">(Not Used)</param>
	/// <returns>Whether the the list is not null or empty</returns>
	public override bool ConvertFrom(IEnumerable? value, Type targetType, object? parameter, CultureInfo? culture) =>
		!IsListNullOrEmptyConverter.IsListNullOrEmpty(value);
}