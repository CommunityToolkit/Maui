using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using CommunityToolkit.Maui.Categories;
using CommunityToolkit.Maui.Helpers;

namespace  CommunityToolkit.Maui.Extensions;

#pragma warning disable IDE0040 // Add accessibility modifiers
internal static class ValueConversionExtensions
{
	private static Dictionary<Type, TypeConverter> converter = new Dictionary<Type, TypeConverter>();

	/// <summary>
   /// Attempts to convert <paramref name="value"/> to <paramref name="toType"/>.
   /// </summary>
   /// <param name="value">A XAML user-defined Value for current <see cref="ScreenCategories"/> a device fits in.</param>
   /// <param name="toType"></param>
   /// <param name="bindableProperty"></param>
   /// <returns></returns>
    public static object ConvertTo(this object value, Type toType, BindableProperty bindableProperty)
    {
	    if (Manager.Current.IsLogEnabled)
	    {
			LogHelpers.WriteLine($"Attempting To Convert \"{(value == null ? "null" : value)}\" of type:{(value == null ? "null" : value.GetType())} to Type:{(toType == null ? "null" : toType)} on bindable Property of type:{(bindableProperty == null ? "null" : bindableProperty.ReturnType)}", LogLevels.Verbose);
	    }

	    if (toType == null)
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

		if (toType.Namespace != null && toType.Namespace.StartsWith("Microsoft.Maui."))
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


        if (bindableProperty != null && toType == typeof(System.Double) && bindableProperty.PropertyName.Equals("FontSize", StringComparison.OrdinalIgnoreCase))
        {
            returnValue = new FontSizeConverter().ConvertFromInvariantString((string)value!)!;
			return returnValue;
        }


        returnValue = Convert.ChangeType(value, toType, CultureInfo.InvariantCulture)!;
        return returnValue;
    }


}
