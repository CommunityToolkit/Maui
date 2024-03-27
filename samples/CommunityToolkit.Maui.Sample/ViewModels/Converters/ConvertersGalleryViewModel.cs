using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.Sample.Models;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class ConvertersGalleryViewModel() : BaseGalleryViewModel(
[
	SectionModel.Create<BoolToObjectConverterViewModel>(nameof(BoolToObjectConverter), "A converter that allows users to convert a bool value binding to a specific object."),
	SectionModel.Create<IsStringNullOrEmptyConverterViewModel>(nameof(IsStringNullOrEmptyConverter), "A converter that allows users to convert an incoming binding to a bool value. This value represents if the incoming binding value is null or empty."),
	SectionModel.Create<IsStringNotNullOrEmptyConverterViewModel>(nameof(IsStringNotNullOrEmptyConverter), "A converter that allows users to convert an incoming binding to a bool value. This value represents if the incoming binding value is Not null or empty."),
	SectionModel.Create<IsStringNullOrWhiteSpaceConverterViewModel>(nameof(IsStringNullOrWhiteSpaceConverter), "A converter that allows users to convert an incoming binding to a bool value. This value represents if the incoming binding value is null or white space."),
	SectionModel.Create<IsStringNotNullOrWhiteSpaceConverterViewModel>(nameof(IsStringNotNullOrWhiteSpaceConverter), "A converter that allows users to convert an incoming binding to a bool value. This value represents if the incoming binding value is not null or white space."),
	SectionModel.Create<InvertedBoolConverterViewModel>(nameof(InvertedBoolConverter), "A converter that allows users to convert a bool value binding to its inverted value."),
	SectionModel.Create<IsEqualConverterViewModel>(nameof(IsEqualConverter), "A converter that allows users to convert any value binding to a bool depending on whether or not it is equal to a different value. "),
	SectionModel.Create<IsNotEqualConverterViewModel>(nameof(IsNotEqualConverter), "A converter that allows users to convert any value binding to a bool depending on whether or not it is not equal to a different value. "),
	SectionModel.Create<DoubleToIntConverterViewModel>(nameof(DoubleToIntConverter), "A converter that allows users to convert an incoming double value to an int."),
	SectionModel.Create<IndexToArrayItemConverterViewModel>(nameof(IndexToArrayItemConverter), "A converter that allows users to convert a int value binding to an item in an array."),
	SectionModel.Create<IntToBoolConverterViewModel>(nameof(IntToBoolConverter), "A converter that allows users to convert an incoming int value to a bool."),
	SectionModel.Create<ItemTappedEventArgsConverterViewModel>(nameof(ItemTappedEventArgsConverter), "A converter that allows you to extract the value from ItemTappedEventArgs that can be used in combination with EventToCommandBehavior"),
	SectionModel.Create<TextCaseConverterViewModel>(nameof(TextCaseConverter), "A converter that allows users to convert the casing of an incoming string type binding. The Type property is used to define what kind of casing will be applied to the string."),
	SectionModel.Create<MultiConverterViewModel>(nameof(MultiConverter), "This sample demonstrates how to use MultiBinding Converter"),
	SectionModel.Create<DateTimeOffsetConverterViewModel>(nameof(DateTimeOffsetConverter), "A converter that allows to convert from a DateTimeOffset type to a DateTime type"),
	SectionModel.Create<VariableMultiValueConverterViewModel>(nameof(VariableMultiValueConverter), "A converter that allows you to combine multiple boolean bindings into a single binding."),
	SectionModel.Create<IsListNullOrEmptyConverterViewModel>(nameof(IsListNullOrEmptyConverter), "A converter that allows you to check if collection is null or empty"),
	SectionModel.Create<IsListNotNullOrEmptyConverterViewModel>(nameof(IsListNotNullOrEmptyConverter), "A converter that allows you to check if collection is not null or empty"),
	SectionModel.Create<ListToStringConverterViewModel>(nameof(ListToStringConverter), "A converter that allows users to convert an incoming binding that implements IEnumerable to a single string value. The Separator property is used to join the items in the IEnumerable."),
	SectionModel.Create<EnumToBoolConverterViewModel>(nameof(EnumToBoolConverter), "A converter that allows you to convert an Enum to boolean value"),
	SectionModel.Create<EnumToIntConverterViewModel>(nameof(EnumToIntConverter), "A converter that allows you to convert an Enum to its underlying int value"),
	SectionModel.Create<ImageResourceConverterViewModel>(nameof(ImageResourceConverter), "A converter that allows you to convert embedded resource image id to an ImageSource"),
	SectionModel.Create<MathExpressionConverterViewModel>(nameof(MathExpressionConverter), "A converter that allows users to calculate an expression at runtime."),
	SectionModel.Create<MultiMathExpressionConverterViewModel>(nameof(MultiMathExpressionConverter), "A converter that allows users to calculate multiple math expressions at runtime."),
	SectionModel.Create<StringToListConverterViewModel>(nameof(StringToListConverter), "A converter that splits a string by the separator and returns the enumerable sequence of strings as the result."),
	SectionModel.Create<ColorsConverterViewModel>("ColorConverters", "A group of converters that convert a Color to your strings values (RGB, HEX, HSL, etc)"),
	SectionModel.Create<SelectedItemEventArgsConverterViewModel>(nameof(SelectedItemEventArgsConverter), "A converter that allows you to extract the selected item in a ListView from the SelectedItemChangedEventArgs object."),
	SectionModel.Create<CompareConverterViewModel>(nameof(CompareConverter), "A converter that compares two IComparable objects and returns a boolean value or one of two specified objects."),
	SectionModel.Create<ByteArrayToImageSourceConverterViewModel>(nameof(ByteArrayToImageSourceConverter), "A converter that allows the user to convert an incoming value from byte array and returns an object of type ImageSource. This object can then be used as the Source of an Image control.."),
	SectionModel.Create<StateToBooleanConverterViewModel>(nameof(StateToBooleanConverter), "A converter that allows the user to convert a LayoutState enum to a boolean value."),
	SectionModel.Create<IsNotNullConverterViewModel>(nameof(IsNotNullConverter), "A converter that allows users to convert an incoming `object?` to a bool."),
	SectionModel.Create<IsNullConverterViewModel>(nameof(IsNullConverter), "A converter that allows users to convert an incoming `object?` to a bool."),
	SectionModel.Create<IsInRangeConverterViewModel>(nameof(IsInRangeConverter), "A converter that returns a bool if the incoming object is within a defined range.")
]);