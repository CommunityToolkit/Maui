using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.Pages.Converters;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class ConvertersGalleryViewModel : BaseGalleryViewModel
{
	public ConvertersGalleryViewModel(EqualConverterPage equalConverterPage,
										MultiConverterPage multiConverterPage,
										ColorsConverterPage colorsConverterPage,
										NotEqualConverterPage notEqualConverterPage,
										TextCaseConverterPage textCaseConverterPage,
										EnumToIntConverterPage enumToIntConverterPage,
										IntToBoolConverterPage intToBoolConverterPage,
										EnumToBoolConverterPage enumToBoolConverterPage,
										ItemTappedEventArgsPage itemTappedEventArgsPage,
										DoubleToIntConverterPage doubleToIntConverterPage,
										BoolToObjectConverterPage boolToObjectConverterPage,
										InvertedBoolConverterPage invertedBoolConverterPage,
										ListToStringConverterPage listToStringConverterPage,
										StringToListConverterPage stringToListConverterPage,
										ImageResourceConverterPage imageResourceConverterPage,
										IsNullOrEmptyConverterPage isNullOrEmptyConverterPage,
										DateTimeOffsetConverterPage dateTimeOffsetConverterPage,
										MathExpressionConverterPage mathExpressionConverterPage,
										IsNotNullOrEmptyConverterPage isNotNullOrEmptyConverterPage,
										IndexToArrayItemConverterPage indexToArrayItemConverterPage,
										ListIsNullOrEmptyConverterPage listIsNullOrEmptyConverterPage,
										VariableMultiValueConverterPage variableMultiValueConverterPage,
										ListIsNotNullOrEmptyConverterPage listIsNotNullOrEmptyConverterPage)
		: base(new[]
		{
			new SectionModel(
				boolToObjectConverterPage,
				nameof(BoolToObjectConverter),
				"A converter that allows users to convert a bool value binding to a specific object."),

			new SectionModel(
				isNullOrEmptyConverterPage,
				nameof(IsNullOrEmptyConverter),
				"A converter that allows users to convert an incoming binding to a bool value. This value represents if the incoming binding value is null or empty."),

			new SectionModel(
				isNotNullOrEmptyConverterPage,
				nameof(IsNotNullOrEmptyConverter),
				"A converter that allows users to convert an incoming binding to a bool value. This value represents if the incoming binding value is Not null or empty."),

			new SectionModel(
				invertedBoolConverterPage,
				nameof(InvertedBoolConverter),
				"A converter that allows users to convert a bool value binding to its inverted value.."),

			new SectionModel(
				equalConverterPage,
				nameof(EqualConverter),
				"A converter that allows users to convert any value binding to a bool depending on whether or not it is equal to a different value. "),

			new SectionModel(
				notEqualConverterPage,
				nameof(NotEqualConverter),
				"A converter that allows users to convert any value binding to a bool depending on whether or not it is not equal to a different value. "),

			new SectionModel(
				doubleToIntConverterPage,
				nameof(DoubleToIntConverter),
				"A converter that allows users to convert an incoming double value to an int."),

			new SectionModel(
				indexToArrayItemConverterPage,
				nameof(IndexToArrayItemConverter),
				"A converter that allows users to convert a int value binding to an item in an array."),

			new SectionModel(
				intToBoolConverterPage,
				nameof(IntToBoolConverter),
				"A converter that allows users to convert an incoming int value to a bool."),

			new SectionModel(
				itemTappedEventArgsPage,
				nameof(ItemTappedEventArgsConverter),
				"A converter that allows you to extract the value from ItemTappedEventArgs that can be used in combination with EventToCommandBehavior"),

			new SectionModel(
				textCaseConverterPage,
				nameof(TextCaseConverter),
				"A converter that allows users to convert the casing of an incoming string type binding. The Type property is used to define what kind of casing will be applied to the string."),

			new SectionModel(
				multiConverterPage,
				nameof(MultiConverter),
				"This sample demonstrates how to use Multibinding Converter"),

			new SectionModel(
				dateTimeOffsetConverterPage,
				nameof(DateTimeOffsetConverter),
				"A converter that allows to convert from a DateTimeOffset type to a DateTime type"),

			new SectionModel(
				variableMultiValueConverterPage,
				nameof(VariableMultiValueConverter),
				"A converter that allows you to combine multiple boolean bindings into a single binding."),

			new SectionModel(
				listIsNullOrEmptyConverterPage,
				nameof(ListIsNullOrEmptyConverter),
				"A converter that allows you to check if collection is null or empty"),

			new SectionModel(
				listIsNotNullOrEmptyConverterPage,
				nameof(ListIsNotNullOrEmptyConverter),
				"A converter that allows you to check if collection is not null or empty"),

			new SectionModel(
				listToStringConverterPage,
				nameof(ListToStringConverter),
				"A converter that allows users to convert an incoming binding that implements IEnumerable to a single string value. The Separator property is used to join the items in the IEnumerable."),

			new SectionModel(
				enumToBoolConverterPage,
				nameof(EnumToBoolConverter),
				"A converter that allows you to convert an Enum to boolean value"),

			new SectionModel(
				enumToIntConverterPage,
				nameof(EnumToIntConverter),
				"A converter that allows you to convert an Enum to its underlying int value"),

			new SectionModel(
				imageResourceConverterPage,
				nameof(ImageResourceConverter),
				"A converter that allows you to convert embeded ressource image id to an ImageSource"),

			new SectionModel(
				mathExpressionConverterPage,
				nameof(MathExpressionConverter),
				"A converter that allows users to calculate an expression at runtime."),
			new SectionModel(
				stringToListConverterPage,
				nameof(StringToListConverter),
				"A converter that splits a string by the separator and returns the enumerable sequence of strings as the result."),

			new SectionModel(
				imageResourceConverterPage,
				nameof(ImageResourceConverter),
				"A converter that allows you to convert embeded ressource image id to an ImageSource"),

			new SectionModel(
				colorsConverterPage,
				"ColorConverters",
				"A group of converters that convert a Color to your strings values (RGB, HEX, HSL, etc)"),
		})
	{
	}
}