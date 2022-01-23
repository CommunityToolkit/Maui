using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.Pages.Converters;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class ConvertersGalleryViewModel : BaseGalleryViewModel
{
	public ConvertersGalleryViewModel()
		: base(new[]
		{
			new SectionModel(
				typeof(BoolToObjectConverterViewModel),
				nameof(BoolToObjectConverter),
				"A converter that allows users to convert a bool value binding to a specific object."),

			new SectionModel(
				typeof(IsNullOrEmptyConverterViewModel),
				nameof(IsNullOrEmptyConverter),
				"A converter that allows users to convert an incoming binding to a bool value. This value represents if the incoming binding value is null or empty."),

			new SectionModel(
				typeof(IsNotNullOrEmptyConverterViewModel),
				nameof(IsNotNullOrEmptyConverter),
				"A converter that allows users to convert an incoming binding to a bool value. This value represents if the incoming binding value is Not null or empty."),

			new SectionModel(
				typeof(InvertedBoolConverterViewModel),
				nameof(InvertedBoolConverter),
				"A converter that allows users to convert a bool value binding to its inverted value.."),

			new SectionModel(
				typeof(EqualConverterViewModel),
				nameof(EqualConverter),
				"A converter that allows users to convert any value binding to a bool depending on whether or not it is equal to a different value. "),

			new SectionModel(
				typeof(NotEqualConverterViewModel),
				nameof(NotEqualConverter),
				"A converter that allows users to convert any value binding to a bool depending on whether or not it is not equal to a different value. "),

			new SectionModel(
				typeof(DoubleToIntConverterViewModel),
				nameof(DoubleToIntConverter),
				"A converter that allows users to convert an incoming double value to an int."),

			new SectionModel(
				typeof(IndexToArrayItemConverterViewModel),
				nameof(IndexToArrayItemConverter),
				"A converter that allows users to convert a int value binding to an item in an array."),

			new SectionModel(
				typeof(IntToBoolConverterViewModel),
				nameof(IntToBoolConverter),
				"A converter that allows users to convert an incoming int value to a bool."),

			new SectionModel(
				typeof(ItemTappedEventArgsViewModel),
				nameof(ItemTappedEventArgsConverter),
				"A converter that allows you to extract the value from ItemTappedEventArgs that can be used in combination with EventToCommandBehavior"),

			new SectionModel(
				typeof(TextCaseConverterViewModel),
				nameof(TextCaseConverter),
				"A converter that allows users to convert the casing of an incoming string type binding. The Type property is used to define what kind of casing will be applied to the string."),

			new SectionModel(
				typeof(MultiConverterViewModel),
				nameof(MultiConverter),
				"This sample demonstrates how to use Multibinding Converter"),

			new SectionModel(
				typeof(DateTimeOffsetConverterViewModel),
				nameof(DateTimeOffsetConverter),
				"A converter that allows to convert from a DateTimeOffset type to a DateTime type"),

			new SectionModel(
				typeof(VariableMultiValueConverterViewModel),
				nameof(VariableMultiValueConverter),
				"A converter that allows you to combine multiple boolean bindings into a single binding."),

			new SectionModel(
				typeof(ListIsNullOrEmptyConverterViewModel),
				nameof(ListIsNullOrEmptyConverter),
				"A converter that allows you to check if collection is null or empty"),

			new SectionModel(
				typeof(ListIsNotNullOrEmptyConverterViewModel),
				nameof(ListIsNotNullOrEmptyConverter),
				"A converter that allows you to check if collection is not null or empty"),

			new SectionModel(
				typeof(ListToStringConverterViewModel),
				nameof(ListToStringConverter),
				"A converter that allows users to convert an incoming binding that implements IEnumerable to a single string value. The Separator property is used to join the items in the IEnumerable."),

			new SectionModel(
				typeof(EnumToBoolConverterViewModel),
				nameof(EnumToBoolConverter),
				"A converter that allows you to convert an Enum to boolean value"),

			new SectionModel(
				typeof(EnumToIntConverterViewModel),
				nameof(EnumToIntConverter),
				"A converter that allows you to convert an Enum to its underlying int value"),

			new SectionModel(
				typeof(ImageResourceConverterViewModel),
				nameof(ImageResourceConverter),
				"A converter that allows you to convert embedded resource image id to an ImageSource"),

			new SectionModel(
				typeof(MathExpressionConverterViewModel),
				nameof(MathExpressionConverter),
				"A converter that allows users to calculate an expression at runtime."),
			new SectionModel(
				typeof(StringToListConverterViewModel),
				nameof(StringToListConverter),
				"A converter that splits a string by the separator and returns the enumerable sequence of strings as the result."),

			new SectionModel(
				typeof(ColorsConvertersViewModel),
				"ColorConverters",
				"A group of converters that convert a Color to your strings values (RGB, HEX, HSL, etc)"),
		})
	{
	}
}

public class NotEqualConverterViewModel : BaseViewModel
{
}

public class MultiConverterViewModel : BaseViewModel
{
}

public class InvertedBoolConverterViewModel : BaseViewModel
{
}

public class TextCaseConverterViewModel : BaseViewModel
{
}

public class VariableMultiValueConverterViewModel : BaseViewModel
{
}

public class EnumToBoolConverterViewModel : BaseViewModel
{
}

public class MathExpressionConverterViewModel : BaseViewModel
{
}

public class StringToListConverterViewModel : BaseViewModel
{
}

public class ImageResourceConverterViewModel : BaseViewModel
{
}

public class ColorsConvertersViewModel : BaseViewModel
{
}

public class BoolToObjectConverterViewModel : BaseViewModel
{

}