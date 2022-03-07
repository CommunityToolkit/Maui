using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Maui.Extensions;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Abstract class used to implement converters that implements the Convert logic.
/// </summary>
/// <typeparam name="TFrom">Type of the input value</typeparam>
/// <typeparam name="TTo">Type of the output value</typeparam>
public abstract class BaseConverterOneWay<TFrom, TTo> : BaseConverterOneWay
{
	readonly bool isInitializedInXaml = new StackTrace().GetFrames().Any(x => x.GetMethod()?.Name is "LoadFromXaml");

	/// <summary>
	/// Converts the incoming value from <typeparamref name="TFrom"/>[] and returns the object of a type <typeparamref name="TTo"/>.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">Additional parameter for the converter to handle. This is not implemented.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>An object of type <typeparamref name="TTo"/>.</returns>
	public override sealed object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
	{
		if (value is null && IsNullable<TFrom>())
		{
#pragma warning disable CS8604 // Possible null reference argument.
			return ConvertFrom(default);
#pragma warning restore CS8604 // Possible null reference argument.
		}
		else if (value is null && !IsNullable<TFrom>())
		{
			throw new ArgumentNullException(nameof(value), $"value cannot be null because {nameof(TFrom)} is not Nullable");
		}

		if (value is not TFrom convertedValue)
		{
			throw new ArgumentException($"value needs to be of type {typeof(TFrom)}");
		}

		// This validation only works for Converters called in C#, not XAML 
		if (targetType != typeof(TTo) && !IsValidXamlTargetType())
		{
			throw new ArgumentException($"targetType needs to be typeof {typeof(TTo)}");
		}

		return ConvertFrom(convertedValue);

		// Every targetType called from XAML is typeof(string)
		bool IsValidXamlTargetType() => isInitializedInXaml && targetType == typeof(string);
	}

	/// <summary>
	/// Method that will be called by <see cref="Convert(object, Type, object, CultureInfo)"/>.
	/// </summary>
	/// <param name="value">Value to be converted from <typeparamref name="TFrom"/> to <typeparamref name="TTo"/>.</param>
	/// <returns>An object of type <typeparamref name="TTo"/>.</returns>
	public abstract TTo? ConvertFrom(TFrom value);

	static bool IsNullable<T>()
	{
		var type = typeof(T);

		if (!type.IsValueType)
		{
			return true; // ref-type
		}

		if (Nullable.GetUnderlyingType(type) is not null)
		{
			return true; // Nullable<T>
		}

		return false; // value-type
	}
}

/// <summary>
/// Abstract class used to implement converters that implements the Convert logic.
/// </summary>
public abstract class BaseConverterOneWay : ValueConverterExtension, ICommunityToolkitValueConverter
{
	/// <summary>
	/// Converts the incoming value and returns the result.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="targetType">The type of the binding target property.</param>
	/// <param name="parameter">Additional parameter for the converter to handle.</param>
	/// <param name="culture">The culture to use in the converter.</param>
	/// <returns>The value converted.</returns>
	public abstract object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture);

	/// <summary>
	/// Not supported, use <see cref="BaseConverter{TFrom, TTo}"/>
	/// </summary>
	public virtual object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture) =>
		throw new NotSupportedException("Impossible to revert to original value. Consider setting BindingMode to OneWay.");
}