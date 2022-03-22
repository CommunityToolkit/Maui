using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <inheritdoc />
public interface ICommunityToolkitMultiValueConverter : IMultiValueConverter
{
	/// <summary>
	/// Converts the incoming values to a different object
	/// </summary>
	/// <param name="values"></param>
	/// <param name="targetType">Target Type</param>
	/// <param name="parameter">Optional Parameters</param>
	/// <param name="culture">Culture Info</param>
	/// <returns>Converted object</returns>
	new object? Convert(object[]? values, Type targetType, object? parameter, CultureInfo? culture);

	/// <summary>
	/// Converts the object back to the outgoing values
	/// </summary>
	/// <param name="value">The object to convert back to an array of values</param>
	/// <param name="targetTypes"></param>
	/// <param name="parameter">Optional Parameters</param>
	/// <param name="culture">Culture Info</param>
	/// <returns>Array of converted objects</returns>
	new object[]? ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo? culture);

	/// <inheritdoc />
	object? IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
		Convert(values, targetType, parameter, culture);

	/// <inheritdoc />
	object[]? IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
		ConvertBack(value, targetTypes, parameter, culture);
}