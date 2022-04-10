using System.Globalization;
using CommunityToolkit.Maui.Extensions;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Abstract class used to implement converters that implements the ConvertBack logic.
/// </summary>
/// <typeparam name="TFrom">Type of the input value.</typeparam>
/// <typeparam name="TTo">Type of the output value.</typeparam>
/// <typeparam name="TParam">Type of parameter</typeparam>
public abstract class BaseConverter<TFrom, TTo, TParam> : ValueConverterExtension, ICommunityToolkitValueConverter
{
	/// <summary>
	/// Method that will be called by <see cref="ICommunityToolkitValueConverter.Convert(object?, Type, object?, CultureInfo?)"/>.
	/// </summary>
	/// <param name="value">The object to convert <typeparamref name="TFrom"/> to <typeparamref name="TTo"/>.</param>
	/// <param name="parameter">Optional Parameters</param>
	/// <param name="culture">Culture Info</param>
	/// <returns>An object of type <typeparamref name="TTo"/>.</returns>
	public abstract TTo ConvertFrom(TFrom value, TParam? parameter, CultureInfo? culture);
	
	/// <summary>
	/// Method that will be called by <see cref="ICommunityToolkitValueConverter.ConvertBack(object?, Type, object?, CultureInfo?)"/>.
	/// </summary>
	/// <param name="value">Value to be converted from <typeparamref name="TTo"/> to <typeparamref name="TFrom"/>.</param>	
	/// <param name="parameter">Optional Parameters</param>
	/// <param name="culture">Culture Info</param>
	/// <returns>An object of type <typeparamref name="TFrom"/>.</returns>
	public abstract TFrom ConvertBackTo(TTo value, TParam? parameter, CultureInfo? culture);
	
	/// <inheritdoc/>
	object? ICommunityToolkitValueConverter.ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
	{
		ArgumentNullException.ThrowIfNull(targetType);

		// Ensure TFrom can be assigned to the given Target Type
		if (!typeof(TFrom).IsAssignableFrom(targetType) && !IsValidTargetType<TFrom>(targetType))
		{
			throw new ArgumentException($"targetType needs to be typeof {typeof(TFrom)}", nameof(targetType));
		}

		TParam? param;
		if (parameter is null && IsNullable<TParam>())
		{
			param = default;
		}
		else if (parameter is not TParam parameterFrom)
		{
			throw new ArgumentException($"parameter needs to be of type {typeof(TParam)}", nameof(parameter));
		}
		else
		{
			param = parameterFrom;
		}

		if (value is null && IsNullable<TTo>())
		{
#pragma warning disable CS8604 // Possible null reference argument.
			return ConvertBackTo(default, param, culture);
#pragma warning restore CS8604 // Possible null reference argument.
		}

		if (value is null && !IsNullable<TTo>())
		{
			throw new ArgumentNullException(nameof(value), $"value cannot be null because {nameof(TFrom)} is not Nullable");
		}

		if (value is not TTo valueFrom)
		{
			throw new ArgumentException($"value needs to be of type {typeof(TTo)}", nameof(value));
		}

		return ConvertBackTo(valueFrom, param, culture);
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

		TParam? param;
		if (parameter is null && IsNullable<TParam>())
		{
			param = default;
		}
		else if (parameter is not TParam parameterFrom)
		{
			throw new ArgumentException($"parameter needs to be of type {typeof(TParam)}", nameof(parameter));
		}
		else
		{
			param = parameterFrom;
		}
		
		if (value is null && IsNullable<TFrom>())
		{
#pragma warning disable CS8604 // Possible null reference argument.
			return ConvertFrom(default, param, culture);
#pragma warning restore CS8604 // Possible null reference argument.
		}

		if (value is null && !IsNullable<TFrom>())
		{
			throw new ArgumentNullException(nameof(value), $"value cannot be null because {nameof(TFrom)} is not Nullable");
		}

		if (value is not TFrom convertedValue)
		{
			throw new ArgumentException($"value needs to be of type {typeof(TFrom)}");
		}

		return ConvertFrom(convertedValue, param, culture);
	}	
}

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
	/// <param name="culture">Culture Info</param>
	/// <returns>An object of type <typeparamref name="TTo"/>.</returns>
	public abstract TTo ConvertFrom(TFrom value, CultureInfo? culture);

	/// <summary>
	/// Method that will be called by <see cref="ICommunityToolkitValueConverter.ConvertBack(object?, Type, object?, CultureInfo?)"/>.
	/// </summary>
	/// <param name="value">Value to be converted from <typeparamref name="TTo"/> to <typeparamref name="TFrom"/>.</param>
	/// <param name="culture">Culture Info</param>
	/// <returns>An object of type <typeparamref name="TFrom"/>.</returns>
	public abstract TFrom ConvertBackTo(TTo value, CultureInfo? culture);
	
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
			return ConvertBackTo(default, culture);
#pragma warning restore CS8604 // Possible null reference argument.
		}

		if (value is null && !IsNullable<TTo>())
		{
			throw new ArgumentNullException(nameof(value), $"value cannot be null because {nameof(TFrom)} is not Nullable");
		}

		if (value is not TTo valueFrom)
		{
			throw new ArgumentException($"value needs to be of type {typeof(TTo)}", nameof(value));
		}

		return ConvertBackTo(valueFrom, culture);
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
			return ConvertFrom(default, culture);
#pragma warning restore CS8604 // Possible null reference argument.
		}

		if (value is null && !IsNullable<TFrom>())
		{
			throw new ArgumentNullException(nameof(value), $"value cannot be null because {nameof(TFrom)} is not Nullable");
		}

		if (value is not TFrom convertedValue)
		{
			throw new ArgumentException($"value needs to be of type {typeof(TFrom)}");
		}

		return ConvertFrom(convertedValue, culture);
	}
}