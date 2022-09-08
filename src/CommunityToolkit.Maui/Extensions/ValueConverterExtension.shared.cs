using System.Diagnostics;
using CommunityToolkit.Maui.Converters;

namespace CommunityToolkit.Maui.Extensions;

/// <inheritdoc />
public abstract class ValueConverterExtension : IMarkupExtension<ICommunityToolkitValueConverter>
{
	/// <inheritdoc />
	public ICommunityToolkitValueConverter ProvideValue(IServiceProvider serviceProvider)
		=> (ICommunityToolkitValueConverter)this;

	object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
		=> ((IMarkupExtension<ICommunityToolkitValueConverter>)this).ProvideValue(serviceProvider);

	private protected static bool IsNullable<T>()
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

	private protected static bool IsValidTargetType<T>(in Type targetType)
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

	private protected static void ValidateTargetType<TTarget>(Type targetType)
	{
		ArgumentNullException.ThrowIfNull(targetType);

		// Ensure TTo can be assigned to the given Target Type
		if (!typeof(TTarget).IsAssignableFrom(targetType) && !IsValidTargetType<TTarget>(targetType))
		{
			throw new ArgumentException($"targetType needs to be assignable from {typeof(TTarget)}.", nameof(targetType));
		}
	}

#pragma warning disable CS8603 // Possible null reference return. If TParam is null (e.g. `string?`), a null return value is expected
	private protected static TParam ConvertParameter<TParam>(object? parameter) => parameter switch
	{
		null when IsNullable<TParam>() => default,
		null when !IsNullable<TParam>() => throw new ArgumentNullException(nameof(parameter), $"Value cannot be null because {nameof(TParam)} is not nullable."),
		TParam convertedParameter => convertedParameter,
		_ => throw new ArgumentException($"Parameter needs to be of type {typeof(TParam)}", nameof(parameter))
	};
#pragma warning restore CS8603 // Possible null reference return.

#pragma warning disable CS8603 // Possible null reference return. If TValue is null (e.g. `string?`), a null return value is expected
	private protected static TValue ConvertValue<TValue>(object? value) => value switch
	{
		null when IsNullable<TValue>() => default,
		null when !IsNullable<TValue>() => throw new ArgumentNullException(nameof(value), $"Value cannot be null because {nameof(TValue)} is not nullable"),
		TValue convertedValue => convertedValue,
		_ => throw new ArgumentException($"Value needs to be of type {typeof(TValue)}", nameof(value))
	};
#pragma warning restore CS8603 // Possible null reference return.
}