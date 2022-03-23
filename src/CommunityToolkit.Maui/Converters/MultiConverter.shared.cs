namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts an incoming value using all of the incoming converters in sequence.
/// </summary>
public class MultiConverter : List<ICommunityToolkitValueConverter>, ICommunityToolkitValueConverter
{
	/// <summary>
	/// Uses the incoming converters to convert the value.
	/// </summary>
	/// <param name="value">Value to convert.</param>
	/// <param name="targetType">The type of the binding target property.</param>
	/// <param name="parameter">Parameter to pass into subsequent converters.</param>
	/// <param name="culture">The culture to use in the converter.</param>
	/// <returns>The converted value.</returns>
	public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo? culture)
		=> parameter is IList<MultiConverterParameter> parameters
		? this.Aggregate(value, (current, converter) => converter.Convert(current, targetType,
				 parameters.FirstOrDefault(x => x.ConverterType == converter.GetType())?.Value, culture))
		: this.Aggregate(value, (current, converter) => converter.Convert(current, targetType, parameter, culture));

	/// <summary>
	/// This method is not supported and will throw a <see cref="NotSupportedException"/>.
	/// </summary>
	/// <param name="value">N/A</param>
	/// <param name="targetType">N/A</param>
	/// <param name="parameter">N/A</param>
	/// <param name="culture">N/A</param>
	/// <returns>N/A</returns>
	public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo? culture)
		=> throw new NotSupportedException("Impossible to revert to original value. Consider setting BindingMode to OneWay.");
}