using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Converters;

public interface ICommunityToolkitIMultiValueConverter : IMultiValueConverter
{
	new object? Convert(object[]? values, Type? targetType, object? parameter, CultureInfo? culture);

	new object[]? ConvertBack(object? value, Type[]? targetTypes, object? parameter, CultureInfo? culture);

	object? IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
		Convert(values, targetType, parameter, culture);

	object[]? IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
		ConvertBack(value, targetTypes, parameter, culture);
}

