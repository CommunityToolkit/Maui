using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Abstract class used to implement converters that implements the Convert logic.
/// </summary>
/// <typeparam name="TFrom">Type of the input value</typeparam>
/// <typeparam name="TTo">Type of the output value</typeparam>
public abstract class BaseConverterOneWay<TFrom, TTo> : BaseConverter<TFrom, TTo>
{
	/// <inheritdoc/>
	public sealed override TFrom DefaultConvertBackReturnValue
	{
		get => throw new NotSupportedException($"{nameof(DefaultConvertBackReturnValue)} is not used for ${nameof(BaseConverterOneWay<TFrom, TTo>)}");
		set => throw new NotSupportedException($"{nameof(DefaultConvertBackReturnValue)} is not used for ${nameof(BaseConverterOneWay<TFrom, TTo>)}");
	}

	/// <inheritdoc/>
	public sealed override TFrom ConvertBackTo(TTo value, CultureInfo? culture) =>
		throw new NotSupportedException("Impossible to revert to original value. Consider setting BindingMode to OneWay.");
}

/// <summary>
/// Abstract class used to implement converters that implements the Convert logic.
/// </summary>
/// <typeparam name="TFrom">Type of the input value</typeparam>
/// <typeparam name="TTo">Type of the output value</typeparam>
/// <typeparam name="TParam">Type of parameter</typeparam>
public abstract class BaseConverterOneWay<TFrom, TTo, TParam> : BaseConverter<TFrom, TTo, TParam>
{
	/// <inheritdoc/>
	public sealed override TFrom DefaultConvertBackReturnValue
	{
		get => throw new NotSupportedException($"{nameof(DefaultConvertBackReturnValue)} is not used for ${nameof(BaseConverterOneWay<TFrom, TTo, TParam>)}");
		set => throw new NotSupportedException($"{nameof(DefaultConvertBackReturnValue)} is not used for ${nameof(BaseConverterOneWay<TFrom, TTo, TParam>)}");
	}

	/// <inheritdoc/>
	public sealed override TFrom ConvertBackTo(TTo value, TParam? parameter, CultureInfo? culture) =>
		throw new NotSupportedException("Impossible to revert to original value. Consider setting BindingMode to OneWay.");
}