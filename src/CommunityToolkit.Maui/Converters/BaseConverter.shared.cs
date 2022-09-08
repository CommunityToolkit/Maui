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
	/// Default value to return when the value is null.
	/// </summary>
	public TTo? Default { get; set; } = default;
	
	/// <inheritdoc/>
	public Type FromType { get; } = typeof(TFrom);

	/// <inheritdoc/>
	public Type ToType { get; } = typeof(TTo);

	/// <summary>
	/// Type of TParam
	/// </summary>
	public Type ParamType { get; } = typeof(TParam);

	/// <summary>
	/// Method that will be called by <see cref="ICommunityToolkitValueConverter.Convert(object?, Type, object?, CultureInfo?)"/>.
	/// </summary>
	/// <param name="value">The object to convert <typeparamref name="TFrom"/> to <typeparamref name="TTo"/>.</param>
	/// <param name="parameter">Optional Parameters</param>
	/// <param name="culture">Culture Info</param>
	/// <returns>An object of type <typeparamref name="TTo"/>.</returns>
	public abstract TTo ConvertFrom(TFrom value, TParam parameter, CultureInfo? culture);

	/// <summary>
	/// Method that will be called by <see cref="ICommunityToolkitValueConverter.ConvertBack(object?, Type, object?, CultureInfo?)"/>.
	/// </summary>
	/// <param name="value">Value to be converted from <typeparamref name="TTo"/> to <typeparamref name="TFrom"/>.</param>	
	/// <param name="parameter">Optional Parameters</param>
	/// <param name="culture">Culture Info</param>
	/// <returns>An object of type <typeparamref name="TFrom"/>.</returns>
	public abstract TFrom ConvertBackTo(TTo value, TParam parameter, CultureInfo? culture);

	/// <inheritdoc/>
	object? ICommunityToolkitValueConverter.ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
	{
		return PerformConvertion<object?>(() =>
		{
			ValidateTargetType<TFrom>(targetType);

			var converterParameter = ConvertParameter<TParam>(parameter);
			var converterValue = ConvertValue<TTo>(value);

			return ConvertBackTo(converterValue, converterParameter, culture);
		}, Default);
	}

	/// <inheritdoc/>
	object? ICommunityToolkitValueConverter.Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
	{
		return PerformConvertion(() =>
		{
			ValidateTargetType<TTo>(targetType);

			var converterParameter = ConvertParameter<TParam>(parameter);
			var converterValue = ConvertValue<TFrom>(value);

			return ConvertFrom(converterValue, converterParameter, culture);
		}, Default);
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
	/// Default value to return when the value is null.
	/// </summary>
	public TTo? Default { get; set; } = default;
	
	/// <inheritdoc/>
	public Type FromType { get; } = typeof(TFrom);

	/// <inheritdoc/>
	public Type ToType { get; } = typeof(TTo);

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
		return PerformConvertion<object?>(() =>
		{
			ValidateTargetType<TFrom>(targetType);
			var converterValue = ConvertValue<TTo>(value);
			return ConvertBackTo(converterValue, culture);
		}, Default);
	}

	/// <inheritdoc/>
	object? ICommunityToolkitValueConverter.Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
	{
		return PerformConvertion<object?>(() =>
		{
			ValidateTargetType<TTo>(targetType);
			var converterValue = ConvertValue<TFrom>(value);
			return ConvertFrom(converterValue, culture);
		}, Default);
	}
}