using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts a <see cref="TimeSpan"/> to a <see cref="double"/> value expressed in seconds.
/// </summary>
public class TimeSpanToDoubleConverter : ICommunityToolkitValueConverter
{
	/// <summary>
	/// Converts a <see cref="TimeSpan"/> to a <see cref="double"/> value expressed in seconds.
	/// </summary>
	/// <param name="value">The <see cref="TimeSpan"/> value to convert.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">Additional parameter for the converter to handle. This is not implemented.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>A <see cref="double"/> value expressed in seconds.</returns>
	[return: NotNull]
	public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
	{
		if (value is TimeSpan timespan)
		{
			return timespan.TotalSeconds;
		}

		return 1.0;
	}

	/// <summary>
	/// Converts a <see cref="double"/> (value should be in seconds) to a <see cref="TimeSpan"/> value.
	/// </summary>
	/// <param name="value">The <see cref="double"/> value (in seconds) to convert.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">Additional parameter for the converter to handle. This is not implemented.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>The <see cref="TimeSpan"/> value representing the converted <see cref="double"/> value.</returns>
	[return: NotNull]
	public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture)
	{
		if (value is double doubleValue)
		{
			return TimeSpan.FromSeconds(doubleValue);
		}

		return TimeSpan.Zero;
	}
}