using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CommunityToolkit.Maui.Extensions.Internals;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts/Extracts the incoming value from <see cref="ItemTappedEventArgs"/> object and returns the value of <see cref="ItemTappedEventArgs.Item"/> property from it.
/// </summary>
public class ItemTappedEventArgsConverter : ValueConverterExtension, ICommunityToolkitValueConverter
{
	/// <summary>
	/// Converts/Extracts the incoming value from <see cref="ItemTappedEventArgs"/> object and returns the value of <see cref="ItemTappedEventArgs.Item"/> property from it.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">Additional parameter for the converter to handle. This is not implemented.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>A <see cref="ItemTappedEventArgs.Item"/> object from object of type <see cref="ItemTappedEventArgs"/>.</returns>
	[return: NotNullIfNotNull("value")]
	public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
	{
		if (value == null)
			return null;

		return value is ItemTappedEventArgs itemTappedEventArgs
			? itemTappedEventArgs.Item
			: throw new ArgumentException("Expected value to be of type ItemTappedEventArgs", nameof(value));
	}

	/// <summary>
	/// This method is not implemented and will throw a <see cref="NotImplementedException"/>.
	/// </summary>
	/// <param name="value">N/A</param>
	/// <param name="targetType">N/A</param>
	/// <param name="parameter">N/A</param>
	/// <param name="culture">N/A</param>
	/// <returns>N/A</returns>
	public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture)
		=> throw new NotImplementedException();
}