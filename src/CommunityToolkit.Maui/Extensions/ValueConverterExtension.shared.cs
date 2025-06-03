using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.Core.Extensions;

namespace CommunityToolkit.Maui.Extensions;

/// <inheritdoc cref="Microsoft.Maui.Controls.Xaml.IMarkupExtension" />
public abstract class ValueConverterExtension : BindableObject, IMarkupExtension<ICommunityToolkitValueConverter>
{
	/// <inheritdoc />
	public ICommunityToolkitValueConverter ProvideValue(IServiceProvider serviceProvider)
		=> (ICommunityToolkitValueConverter)this;

	private protected static bool IsNullable<T>() => typeof(T).IsNullable();

	private protected static bool IsValidTargetType<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TTarget>([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] in Type targetType, bool shouldAllowNullableValueTypes)
	{
		if (IsConvertingToString(targetType) && CanBeConvertedToString())
		{
			return true;
		}

		// Is targetType a Nullable Value Type? Eg TTarget is bool and targetType is bool?
		if (shouldAllowNullableValueTypes && targetType.IsValueType && IsValidNullableValueType(targetType))
		{
			return true;
		}

		try
		{
			var instanceOfT = default(TTarget);
			instanceOfT ??= (TTarget?)Activator.CreateInstance(targetType);

			var result = Convert.ChangeType(instanceOfT, targetType);

			return result is not null;
		}
		catch
		{
			return false;
		}

		static bool IsConvertingToString(in Type targetType) => targetType == typeof(string);
		static bool CanBeConvertedToString() => typeof(TTarget).GetMethods().Any(x => x.Name is nameof(ToString) && x.ReturnType == typeof(string));

		static bool IsValidNullableValueType(Type targetType)
		{
			if (!targetType.IsNullable())
			{
				return false;
			}

			var underlyingType = Nullable.GetUnderlyingType(targetType) ?? throw new InvalidOperationException("Non-nullable are not valid");

			return underlyingType == typeof(TTarget);
		}
	}

	private protected static void ValidateTargetType<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TTarget>([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type targetType, bool shouldAllowNullableValueTypes)
	{
		ArgumentNullException.ThrowIfNull(targetType);

		// Ensure TTo can be assigned to the given Target Type
		if (!typeof(TTarget).IsAssignableFrom(targetType) // Ensure TTarget can be assigned from targetType. Eg TTarget is IEnumerable and targetType is IList
			&& !IsValidTargetType<TTarget>(targetType, shouldAllowNullableValueTypes)) // Ensure targetType be converted to TTarget? Eg `Convert.ChangeType()` returns a non-null value
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

	object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
		=> ((IMarkupExtension<ICommunityToolkitValueConverter>)this).ProvideValue(serviceProvider);
}