using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CommunityToolkit.Maui.Extensions.Internals;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Checks whether the incoming value equals the provided parameter.
/// </summary>
public class EqualConverter : ValueConverterExtension, ICommunityToolkitValueConverter
{
	/// <summary>
	/// Checks whether the incoming value doesn't equal the provided parameter.
	/// </summary>
	/// <param name="value">The first object to compare.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">The second object to compare.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>True if <paramref name="value"/> and <paramref name="parameter"/> are equal, False if they are not equal.</returns>
	[return: NotNull]
	public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture) => ConvertInternal(value, parameter);

	/// <summary>
	/// This method is not implemented and will throw a <see cref="NotImplementedException"/>.
	/// </summary>
	/// <param name="value">N/A</param>
	/// <param name="targetType">N/A</param>
	/// <param name="parameter">N/A</param>
	/// <param name="culture">N/A</param>
	/// <returns>N/A</returns>
	[return: NotNull]
	public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture)
		=> throw new NotImplementedException();

	internal static bool ConvertInternal(object? value, object? parameter) =>
		(value != null && value.Equals(parameter)) || (value == null && parameter == null);
}