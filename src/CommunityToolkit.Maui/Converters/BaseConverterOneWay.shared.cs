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
	public sealed override TFrom ConvertBackTo(TTo value, Type targetType, object? parameter, CultureInfo? culture) =>
		throw new NotSupportedException("Impossible to revert to original value. Consider setting BindingMode to OneWay.");
}