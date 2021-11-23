using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CommunityToolkit.Maui.Extensions.Internals;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts an <see cref="int"/> index to corresponding array item and vice versa.
/// </summary>
public class IndexToArrayItemConverter : ValueConverterExtension, ICommunityToolkitValueConverter
{
	/// <summary>
	/// Converts an <see cref="int"/> index to corresponding array item.
	/// </summary>
	/// <param name="value">The index of items array.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">The items array.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>The item from the array that corresponds to passed index.</returns>
	public object? Convert([NotNull] object? value, Type? targetType, [NotNull] object? parameter, CultureInfo? culture)
	{
		if (value is not int index)
			throw new ArgumentException("Value is not a valid integer", nameof(value));

		if (parameter is not Array array)
			throw new ArgumentException("Parameter is not a valid array", nameof(parameter));

		if (index < 0 || index >= array.Length)
			throw new ArgumentOutOfRangeException(nameof(value), "Index was out of range");

		return array.GetValue(index);
	}

	/// <summary>
	/// Converts back an array item to corresponding index of the item in the array.
	/// </summary>
	/// <param name="value">The item from the array.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">The items array.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>The index of the item from the array.</returns>
	public object? ConvertBack(object? value, Type? targetType, [NotNull] object? parameter, CultureInfo? culture)
	{
		if (parameter is not Array array)
			throw new ArgumentException("Parameter is not a valid array", nameof(parameter));

		for (var i = 0; i < array.Length; i++)
		{
			var item = array.GetValue(i);
			if ((item != null && item.Equals(value)) || (item == null && value == null))
				return i;
		}

		throw new ArgumentException("Value does not exist in the array", nameof(value));
	}
}