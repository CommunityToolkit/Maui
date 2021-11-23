using System;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Maui.Extensions.Internals;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts <see cref="DateTimeOffset"/> to <see cref="DateTime"/> and back.
/// </summary>
public class DateTimeOffsetConverter : ValueConverterExtension, ICommunityToolkitValueConverter
{
	/// <summary>
	/// Converts <see cref="DateTimeOffset"/> to <see cref="DateTime"/>
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">Additional parameter for the converter to handle. This is not implemented.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>The <see cref="DateTime"/> value.</returns>
	[return: NotNull]
	public object? Convert([NotNull] object? value, Type? targetType, object? parameter, System.Globalization.CultureInfo? culture)
		=> value is DateTimeOffset dateTimeOffset
			? dateTimeOffset.DateTime
			: throw new ArgumentException("Value is not a valid DateTimeOffset", nameof(value));

	/// <summary>
	/// Converts <see cref="DateTime"/> back to <see cref="DateTimeOffset"/>.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">Additional parameter for the converter to handle. This is not implemented.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented..</param>
	/// <returns>The <see cref="DateTimeOffset"/> value.</returns>
	[return: NotNull]
	public object? ConvertBack([NotNull] object? value, Type? targetType, object? parameter, System.Globalization.CultureInfo? culture)
		=> value is DateTime dateTime
			? dateTime.Kind switch
			{
				DateTimeKind.Local => new DateTimeOffset(dateTime, DateTimeOffset.Now.Offset),
				DateTimeKind.Utc => new DateTimeOffset(dateTime, DateTimeOffset.UtcNow.Offset),
				_ => new DateTimeOffset(dateTime, TimeSpan.Zero),
			}
			: throw new ArgumentException("Value is not a valid DateTime", nameof(value));
}