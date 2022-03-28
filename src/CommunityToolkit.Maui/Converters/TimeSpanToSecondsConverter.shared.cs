using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts a <see cref="TimeSpan"/> to a <see cref="double"/> value expressed in seconds.
/// </summary>
public class TimeSpanToSecondsConverter : BaseConverter<TimeSpan, double>
{
	/// <summary>
	/// Converts a <see cref="TimeSpan"/> to a <see cref="double"/> value expressed in seconds.
	/// </summary>
	/// <param name="value">The <see cref="TimeSpan"/> value to convert.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">Additional parameter for the converter to handle. This is not implemented.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>A <see cref="double"/> value expressed in seconds.</returns>
	public override double ConvertFrom(TimeSpan value, Type targetType, object? parameter, CultureInfo? culture)
	{
		ArgumentNullException.ThrowIfNull(value);
		return value.TotalSeconds;
	}

	/// <summary>
	/// Converts a <see cref="double"/> (value should be in seconds) to a <see cref="TimeSpan"/> value.
	/// </summary>
	/// <param name="value">The <see cref="double"/> value (in seconds) to convert.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">Additional parameter for the converter to handle. This is not implemented.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>The <see cref="TimeSpan"/> value representing the converted <see cref="double"/> value.</returns>
	public override TimeSpan ConvertBackTo(double value, Type targetType, object? parameter, CultureInfo? culture)
	{
		ArgumentNullException.ThrowIfNull(value);
		return TimeSpan.FromSeconds(value);
	}
}