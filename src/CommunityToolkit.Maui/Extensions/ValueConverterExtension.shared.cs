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

	internal static bool IsNullable<T>()
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

	internal static bool IsValidTargetType<T>(in Type targetType)
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
}