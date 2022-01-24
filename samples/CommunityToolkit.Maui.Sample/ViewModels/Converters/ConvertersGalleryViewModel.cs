using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.Pages.Converters;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class ConvertersGalleryViewModel : BaseGalleryViewModel
{
	public ConvertersGalleryViewModel()
		: base(new[]
		{
			SectionModel.Create<BoolToObjectConverterPage>(nameof(BoolToObjectConverter),
				"A converter that allows users to convert a bool value binding to a specific object."),

			SectionModel.Create<IsNullOrEmptyConverterPage>(nameof(IsNullOrEmptyConverter),
				"A converter that allows users to convert an incoming binding to a bool value. This value represents if the incoming binding value is null or empty."),

			SectionModel.Create<IsNotNullOrEmptyConverterPage>(nameof(IsNotNullOrEmptyConverter),
				"A converter that allows users to convert an incoming binding to a bool value. This value represents if the incoming binding value is Not null or empty."),

			SectionModel.Create<InvertedBoolConverterPage>(nameof(InvertedBoolConverter),
				"A converter that allows users to convert a bool value binding to its inverted value.."),

			SectionModel.Create<EqualConverterPage>(nameof(EqualConverter),
				"A converter that allows users to convert any value binding to a bool depending on whether or not it is equal to a different value. "),

			SectionModel.Create<NotEqualConverterPage>(nameof(NotEqualConverter),
				"A converter that allows users to convert any value binding to a bool depending on whether or not it is not equal to a different value. "),

			SectionModel.Create<DoubleToIntConverterPage>(nameof(DoubleToIntConverter),
				"A converter that allows users to convert an incoming double value to an int."),

			SectionModel.Create<IndexToArrayItemConverterPage>(nameof(IndexToArrayItemConverter),
				"A converter that allows users to convert a int value binding to an item in an array."),

			SectionModel.Create<IntToBoolConverterPage>(nameof(IntToBoolConverter),
				"A converter that allows users to convert an incoming int value to a bool."),

			SectionModel.Create<ItemTappedEventArgsPage>(nameof(ItemTappedEventArgsConverter),
				"A converter that allows you to extract the value from ItemTappedEventArgs that can be used in combination with EventToCommandBehavior"),

			SectionModel.Create<TextCaseConverterPage>(nameof(TextCaseConverter),
				"A converter that allows users to convert the casing of an incoming string type binding. The Type property is used to define what kind of casing will be applied to the string."),

			SectionModel.Create<MultiConverterPage>(nameof(MultiConverter),
				"This sample demonstrates how to use Multibinding Converter"),

			SectionModel.Create<DateTimeOffsetConverterPage>(nameof(DateTimeOffsetConverter),
				"A converter that allows to convert from a DateTimeOffset type to a DateTime type"),

			SectionModel.Create<VariableMultiValueConverterPage>(nameof(VariableMultiValueConverter),
				"A converter that allows you to combine multiple boolean bindings into a single binding."),

			SectionModel.Create<ListIsNullOrEmptyConverterPage>(nameof(ListIsNullOrEmptyConverter),
				"A converter that allows you to check if collection is null or empty"),

			SectionModel.Create<ListIsNotNullOrEmptyConverterPage>(nameof(ListIsNotNullOrEmptyConverter),
				"A converter that allows you to check if collection is not null or empty"),

			SectionModel.Create<ListToStringConverterPage>(nameof(ListToStringConverter),
				"A converter that allows users to convert an incoming binding that implements IEnumerable to a single string value. The Separator property is used to join the items in the IEnumerable."),

			SectionModel.Create<EnumToBoolConverterPage>(nameof(EnumToBoolConverter),
				"A converter that allows you to convert an Enum to boolean value"),

			SectionModel.Create<EnumToIntConverterPage>(nameof(EnumToIntConverter),
				"A converter that allows you to convert an Enum to its underlying int value"),

			SectionModel.Create<ImageResourceConverterPage>(nameof(ImageResourceConverter),
				"A converter that allows you to convert embeded ressource image id to an ImageSource"),

			SectionModel.Create<MathExpressionConverterPage>(nameof(MathExpressionConverter),
				"A converter that allows users to calculate an expression at runtime."),

			SectionModel.Create<StringToListConverterPage>(nameof(StringToListConverter),
				"A converter that splits a string by the separator and returns the enumerable sequence of strings as the result."),

			SectionModel.Create<ImageResourceConverterPage>(nameof(ImageResourceConverter),
				"A converter that allows you to convert embeded ressource image id to an ImageSource"),

			SectionModel.Create<ColorsConverterPage>("ColorConverters",
				"A group of converters that convert a Color to your strings values (RGB, HEX, HSL, etc)"),
		})
	{
	}
}