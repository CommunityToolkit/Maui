using System.Diagnostics.CodeAnalysis;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts <see cref="DateTimeOffset"/> to <see cref="DateTime"/> and back.
/// </summary>
public class DateTimeOffsetConverter : BaseConverter<DateTimeOffset, DateTime>
{
	/// <summary>
	/// Converts <see cref="DateTimeOffset"/> to <see cref="DateTime"/>
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">Additional parameter for the converter to handle. This is not implemented.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>The <see cref="DateTime"/> value.</returns>
	public override DateTime ConvertFrom(DateTimeOffset value, Type targetType, object? parameter, System.Globalization.CultureInfo? culture)
	{
		ArgumentNullException.ThrowIfNull(value);
		return value.DateTime;
	}

	/// <summary>
	/// Converts <see cref="DateTime"/> back to <see cref="DateTimeOffset"/>.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">Additional parameter for the converter to handle. This is not implemented.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented..</param>
	/// <returns>The <see cref="DateTimeOffset"/> value.</returns>
	public override DateTimeOffset ConvertBackTo(DateTime value, Type targetType, object? parameter, System.Globalization.CultureInfo? culture)
	{
		ArgumentNullException.ThrowIfNull(value);

		var offset = value.Kind switch
		{
			DateTimeKind.Local => DateTimeOffset.Now.Offset,
			DateTimeKind.Utc => DateTimeOffset.UtcNow.Offset,
			_ => TimeSpan.Zero,
		};

		return culture is null ?
			new DateTimeOffset(value, offset) :
			new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Millisecond, culture.Calendar, offset);

	}
}