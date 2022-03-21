using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <inheritdoc />
public interface ICommunityToolkitValueConverter : IValueConverter
{
	/// <summary>
	/// Converts the incoming values to a different object
	/// </summary>
	/// <param name="value">The object to convert</param>
	/// <param name="targetType">Target Type</param>
	/// <param name="parameter">Optional Parameters</param>
	/// <param name="culture">Culture Info</param>
	/// <returns>The converted object</returns>
	new object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture);

	/// <summary>
	/// Converts the object back to the outgoing values
	/// </summary>
	/// <param name="value">The object to convert back</param>
	/// <param name="targetType">Target Type</param>
	/// <param name="parameter">Optional Parameters</param>
	/// <param name="culture">Culture Info</param>
	/// <returns>The object converted back</returns>
	new object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture);

	/// <inheritdoc />
	object? IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
		Convert(value, targetType, parameter, culture);

	/// <inheritdoc />
	object? IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
		ConvertBack(value, targetType, parameter, culture);
}