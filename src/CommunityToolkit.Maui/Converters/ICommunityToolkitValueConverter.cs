using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Converters;

/// <inheritdoc />
public interface ICommunityToolkitValueConverter : IValueConverter
{
	new object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture);

	new object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture);

	object? IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
		Convert(value, targetType, parameter, culture);

	object? IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
		ConvertBack(value, targetType, parameter, culture);
}

