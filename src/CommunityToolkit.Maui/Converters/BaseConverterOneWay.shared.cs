using System;
using System.Globalization;
using CommunityToolkit.Maui.Extensions.Internals;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Abstract class used to implement converters that implements the ConvertBack logic.
/// </summary>
/// <typeparam name="TFrom">Type of the input value</typeparam>
/// <typeparam name="TTo">Type of the output value</typeparam>
public abstract class BaseConverterOneWay<TFrom, TTo> : ValueConverterExtension, ICommunityToolkitValueConverter
{
	/// <summary>
	/// Converts the incoming value from <see cref="TFrom"/>[] and returns the object of a type <see cref="TTo"/>.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">Additional parameter for the converter to handle. This is not implemented.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>An object of type <see cref="TTo"/>.</returns>
	public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
	{
		if (value is not TFrom valueFrom)
			throw new ArgumentException($"value needs to be of type {typeof(TFrom)}");

		if (targetType != typeof(TTo) && !(typeof(TFrom) != typeof(string)))
			throw new ArgumentException($"targetType needs to be typeof {typeof(TTo)}");

		return ConvertFrom(valueFrom);
	}

	/// <summary>
	/// Method that will be called by <see cref="Convert(object, Type, object, CultureInfo)"/>.
	/// </summary>
	/// <param name="value">Value to be converted from <see cref="TFrom"/> to <see cref="TTo"/>.</param>
	/// <returns>An object of type <see cref="TTo"/>.</returns>
	public abstract TTo? ConvertFrom(TFrom value);

	/// <summary>
	/// Not implemented, use <see cref="BaseConverter{TFrom, TTo}"/>
	/// </summary>
	public virtual object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture)
		=> throw new NotImplementedException("Impossible to revert to original value. Consider setting BindingMode to OneWay.");
}