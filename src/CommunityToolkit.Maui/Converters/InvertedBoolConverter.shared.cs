using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CommunityToolkit.Maui.Extensions.Internals;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts true to false and false to true. Simple as that!
/// </summary>
public class InvertedBoolConverter : ValueConverterExtension, ICommunityToolkitValueConverter
{
	/// <summary>
	/// Converts a <see cref="bool"/> to its inverse value.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">Additional parameter for the converter to handle. This is not implemented.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>An inverted <see cref="bool"/> from the one coming in.</returns>
	[return: NotNull]
	public object? Convert([NotNull] object? value, Type? targetType, object? parameter, CultureInfo? culture)
		=> InverseBool(value);

	/// <summary>
	/// Converts a <see cref="bool"/> to its inverse value.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">Additional parameter for the converter to handle. This is not implemented.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>An inverted <see cref="bool"/> from the one coming in.</returns>
	[return: NotNull]
	public object? ConvertBack([NotNull] object? value, Type? targetType, object? parameter, CultureInfo? culture)
		=> InverseBool(value);

	/// <summary>
	/// Inverses an incoming <see cref="bool"/>.
	/// </summary>
	/// <param name="value">The value to inverse.</param>
	/// <returns>The inverted value of the incoming <see cref="bool"/>.</returns>
	static bool InverseBool([NotNull] object? value)
	{
		if (value is bool result)
			return !result;

		throw new ArgumentException("Value is not a valid boolean", nameof(value));
	}
}