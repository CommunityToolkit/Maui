using System.Globalization;
using CommunityToolkit.Maui.Extensions;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Abstract class used to implement converters that implements the ConvertBack logic.
/// </summary>
/// <typeparam name="TFrom">Type of the input value.</typeparam>
/// <typeparam name="TTo">Type of the output value.</typeparam>
public abstract class BaseConverter<TFrom, TTo> : ValueConverterExtension, ICommunityToolkitValueConverter
{
	/// <summary>
	/// Method that will be called by <see cref="ICommunityToolkitValueConverter.Convert(object?, Type, object?, CultureInfo?)"/>.
	/// </summary>
	/// <param name="value">The object to convert <typeparamref name="TFrom"/> to <typeparamref name="TTo"/>.</param>
	/// <param name="targetType">Target Type</param>
	/// <param name="parameter">Optional Parameters</param>
	/// <param name="culture">Culture Info</param>
	/// <returns>An object of type <typeparamref name="TTo"/>.</returns>
	public abstract TTo ConvertFrom(TFrom value, Type targetType, object? parameter, CultureInfo? culture);

	/// <summary>
	/// Method that will be called by <see cref="ICommunityToolkitValueConverter.ConvertBack(object?, Type, object?, CultureInfo?)"/>.
	/// </summary>
	/// <param name="value">Value to be converted from <typeparamref name="TTo"/> to <typeparamref name="TFrom"/>.</param>
	/// <param name="targetType">Target Type</param>
	/// <param name="parameter">Optional Parameters</param>
	/// <param name="culture">Culture Info</param>
	/// <returns>An object of type <typeparamref name="TFrom"/>.</returns>
	public abstract TFrom ConvertBackTo(TTo value, Type targetType, object? parameter, CultureInfo? culture);

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

	static bool IsValidTargetType<T>(in Type targetType)
	{
		if (IsConvertingToString(targetType) && CanBeConvertedToString())
		{
			return true;
		}

		try
		{
			var instanceOfT = default(T);
			instanceOfT ??= (T?)Activator.CreateInstance(targetType);

			var result = Convert.ChangeType(instanceOfT, targetType);

			return result is not null;
		}
		catch
		{
			return false;
		}

		static bool IsConvertingToString(in Type targetType) => targetType == typeof(string);
		static bool CanBeConvertedToString() => typeof(T).GetMethods().Any(x => x.Name is nameof(ToString) && x.ReturnType == typeof(string));
	}


	/// <inheritdoc/>
	object? ICommunityToolkitValueConverter.ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
	{
		ArgumentNullException.ThrowIfNull(targetType);

		// Ensure TFrom can be assigned to the given Target Type
		if (!typeof(TFrom).IsAssignableFrom(targetType) && !IsValidTargetType<TFrom>(targetType))
		{
			throw new ArgumentException($"targetType needs to be typeof {typeof(TFrom)}", nameof(targetType));
		}

		if (value is null && IsNullable<TTo>())
		{
#pragma warning disable CS8604 // Possible null reference argument.
			return ConvertBackTo(default, targetType, parameter, culture);
#pragma warning restore CS8604 // Possible null reference argument.
		}
		else if (value is null && !IsNullable<TTo>())
		{
			throw new ArgumentNullException(nameof(value), $"value cannot be null because {nameof(TFrom)} is not Nullable");
		}

		if (value is not TTo valueFrom)
		{
			throw new ArgumentException($"value needs to be of type {typeof(TTo)}", nameof(value));
		}

		return ConvertBackTo(valueFrom, targetType, parameter, culture);
	}

	/// <inheritdoc/>
	object? ICommunityToolkitValueConverter.Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
	{
		ArgumentNullException.ThrowIfNull(targetType);

		// Ensure TTo can be assigned to the given Target Type
		if (!typeof(TTo).IsAssignableFrom(targetType) && !IsValidTargetType<TTo>(targetType))
		{
			throw new ArgumentException($"targetType needs to be assignable from {typeof(TTo)}", nameof(targetType));
		}

		if (value is null && IsNullable<TFrom>())
		{
#pragma warning disable CS8604 // Possible null reference argument.
			return ConvertFrom(default, targetType, parameter, culture);
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

		return ConvertFrom(convertedValue, targetType, parameter, culture);
	}
}