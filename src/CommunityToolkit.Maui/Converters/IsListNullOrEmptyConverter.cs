using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CommunityToolkit.Maui.Extensions;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts the incoming value to a <see cref="bool"/> indicating whether or not the value is null or empty.
/// </summary>
public class IsListNullOrEmptyConverter : BaseConverterOneWay
{
	/// <summary>
	/// Converts the incoming value to a <see cref="bool"/> indicating whether or not the value is null or empty.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">Additional parameter for the converter to handle. This is not implemented.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>A <see cref="bool"/> indicating if the incoming value is null or empty.</returns>
	[return: NotNull]
	public override object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture) => IsListNullOrEmpty(value);

	internal static bool IsListNullOrEmpty([NotNullWhen(false)] object? value)
	{
		if (value is null)
		{
			return true;
		}

		if (value is IEnumerable list)
		{
			return !list.GetEnumerator().MoveNext();
		}

		throw new ArgumentException("Value is not a valid IEnumerable or null", nameof(value));
	}
}