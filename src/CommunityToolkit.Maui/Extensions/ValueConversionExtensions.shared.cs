using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Helpers;
using Microsoft.Extensions.Logging;

namespace CommunityToolkit.Maui.Extensions;

static class ValueConversionExtensions
{
	static Dictionary<Type, TypeConverter> converter = new();

	/// <summary>
	/// Attempts to convert <paramref name="value"/> to <paramref name="toType"/>.
	/// </summary>
	/// <param name="value">A XAML user-defined Value for current <see cref="ScreenCategories"/> a device fits in.</param>
	/// <param name="toType"></param>
	/// <param name="bindableProperty"></param>
	/// <returns></returns>	
	public static object ConvertTo(this object value, Type? toType, BindableProperty? bindableProperty)
	{
		if (toType is null)
		{
			return null!;
		}

		object returnValue;
		if (ValueConversionExtensions.converter.TryGetValue(toType, out var converter))
		{
			returnValue = converter.ConvertFromInvariantString((string)value!)!;
			return returnValue;
		}

		if (toType.IsEnum)
		{
			returnValue = Enum.Parse(toType, (string)value!);
			return returnValue;
		}

		if (toType == typeof(RowDefinitionCollection))
		{
			converter = (TypeConverter)new RowDefinitionCollectionTypeConverter();
			ValueConversionExtensions.converter.Add(toType, converter);
			var value1 = converter.ConvertFromInvariantString((string)value!);
			return value1!;
		}

		if (toType == typeof(ColumnDefinitionCollection))
		{
			converter = (TypeConverter)new ColumnDefinitionCollectionTypeConverter();
			ValueConversionExtensions.converter.Add(toType, converter);
			var value1 = converter.ConvertFromInvariantString((string)value!);
			return value1!;
		}


		if (toType == typeof(Thickness))
		{
			converter = (TypeConverter)new Microsoft.Maui.Converters.ThicknessTypeConverter();
			ValueConversionExtensions.converter.Add(toType, converter);
			var value1 = converter.ConvertFromInvariantString((string)value!);
			return value1!;
		}

		if (toType.Namespace is not null && toType.Namespace.StartsWith("Microsoft.Maui."))
		{
			var typeConverter = toType.GetCustomAttribute<TypeConverterAttribute>(true);

			if (typeConverter != null && typeConverter.ConverterTypeName != null)
			{
				var converterType = Type.GetType(typeConverter.ConverterTypeName);

				converter = (TypeConverter)Activator.CreateInstance(converterType!)!;

				ValueConversionExtensions.converter.Add(toType, converter);
				return converter.ConvertFromInvariantString((string)value!)!;
			}
		}


		if (bindableProperty != null && toType == typeof(System.Double) &&
		    bindableProperty.PropertyName.Equals("FontSize", StringComparison.OrdinalIgnoreCase))
		{
			returnValue = new FontSizeConverter().ConvertFromInvariantString((string)value!)!;
			return returnValue;
		}


		returnValue = Convert.ChangeType(value, toType, CultureInfo.InvariantCulture)!;
		return returnValue;
	}
}