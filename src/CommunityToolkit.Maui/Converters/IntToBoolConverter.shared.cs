using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CommunityToolkit.Maui.Extensions.Internals;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts an <see cref="int"/> to corresponding <see cref="bool"/> and vice versa.
/// </summary>
public class IntToBoolConverter : ValueConverterExtension, ICommunityToolkitValueConverter
{
	/// <summary>
	/// Converts an <see cref="int"/> to corresponding <see cref="bool"/>.
	/// </summary>
	/// <param name="value"><see cref="int"/> value.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">Additional parameter for the converter to handle. This is not implemented.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>False if the value is 0, otherwise if the value is anything but 0 it returns True.</returns>
	[return: NotNull]
	public object? Convert([NotNull] object? value, Type? targetType, object? parameter, CultureInfo? culture)
		=> value is int result
			? result != 0
			: throw new ArgumentException("Value is not a valid integer", nameof(value));

	/// <summary>
	/// Converts back <see cref="bool"/> to corresponding <see cref="int"/>.
	/// </summary>
	/// <param name="value"><see cref="bool"/> value.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">Additional parameter for the converter to handle. This is not implemented.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>0 if the value is False, otherwise 1 if the value is True.</returns>
	[return: NotNull]
	public object? ConvertBack([NotNull] object? value, Type? targetType, object? parameter, CultureInfo? culture)
	{
		if (value is bool result)
			return result ? 1 : 0;

		throw new ArgumentException("Value is not a valid boolean", nameof(value));
	}
}