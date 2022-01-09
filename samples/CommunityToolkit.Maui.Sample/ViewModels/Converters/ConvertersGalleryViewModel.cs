using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.Pages.Converters;

namespace CommunityToolkit.Maui.Sample.ViewModels.Converters;

public class ConvertersGalleryViewModel : BaseGalleryViewModel
{
	readonly EqualConverterPage _equalConverterPage;
	readonly MultiConverterPage _multiConverterPage;
	readonly ColorsConverterPage _colorsConverterPage;
	readonly NotEqualConverterPage _notEqualConverterPage;
	readonly TextCaseConverterPage _textCaseConverterPage;
	readonly EnumToIntConverterPage _enumToIntConverterPage;
	readonly IntToBoolConverterPage _intToBoolConverterPage;
	readonly EnumToBoolConverterPage _enumToBoolConverterPage;
	readonly ItemTappedEventArgsPage _itemTappedEventArgsPage;
	readonly DoubleToIntConverterPage _doubleToIntConverterPage;
	readonly BoolToObjectConverterPage _boolToObjectConverterPage;
	readonly InvertedBoolConverterPage _invertedBoolConverterPage;
	readonly ListToStringConverterPage _listToStringConverterPage;
	readonly StringToListConverterPage _stringToListConverterPage;
	readonly ImageResourceConverterPage _imageResourceConverterPage;
	readonly IsNullOrEmptyConverterPage _isNullOrEmptyConverterPage;
	readonly DateTimeOffsetConverterPage _dateTimeOffsetConverterPage;
	readonly MathExpressionConverterPage _mathExpressionConverterPage;
	readonly IsNotNullOrEmptyConverterPage _isNotNullOrEmptyConverterPage;
	readonly IndexToArrayItemConverterPage _indexToArrayItemConverterPage;
	readonly ListIsNullOrEmptyConverterPage _listIsNullOrEmptyConverterPage;
	readonly VariableMultiValueConverterPage _variableMultiValueConverterPage;
	readonly ListIsNotNullOrEmptyConverterPage _listIsNotNullOrEmptyConverterPage;

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
	{
		_equalConverterPage = equalConverterPage;
		_multiConverterPage = multiConverterPage;
		_colorsConverterPage = colorsConverterPage;
		_notEqualConverterPage = notEqualConverterPage;
		_textCaseConverterPage = textCaseConverterPage;
		_enumToIntConverterPage = enumToIntConverterPage;
		_intToBoolConverterPage = intToBoolConverterPage;
		_enumToBoolConverterPage = enumToBoolConverterPage;
		_itemTappedEventArgsPage = itemTappedEventArgsPage;
		_doubleToIntConverterPage = doubleToIntConverterPage;
		_boolToObjectConverterPage = boolToObjectConverterPage;
		_invertedBoolConverterPage = invertedBoolConverterPage;
		_listToStringConverterPage = listToStringConverterPage;
		_stringToListConverterPage = stringToListConverterPage;
		_imageResourceConverterPage = imageResourceConverterPage;
		_isNullOrEmptyConverterPage = isNullOrEmptyConverterPage;
		_dateTimeOffsetConverterPage = dateTimeOffsetConverterPage;
		_mathExpressionConverterPage = mathExpressionConverterPage;
		_isNotNullOrEmptyConverterPage = isNotNullOrEmptyConverterPage;
		_indexToArrayItemConverterPage = indexToArrayItemConverterPage;
		_listIsNullOrEmptyConverterPage = listIsNullOrEmptyConverterPage;
		_variableMultiValueConverterPage = variableMultiValueConverterPage;
		_listIsNotNullOrEmptyConverterPage = listIsNotNullOrEmptyConverterPage;
	}

	protected override IEnumerable<SectionModel> CreateItems() => new[]
	{
		new SectionModel(
			_boolToObjectConverterPage,
			nameof(BoolToObjectConverter),
			"A converter that allows users to convert a bool value binding to a specific object."),
		new SectionModel(
			_isNullOrEmptyConverterPage,
			nameof(IsNullOrEmptyConverter),
			"A converter that allows users to convert an incoming binding to a bool value. This value represents if the incoming binding value is null or empty."),
		new SectionModel(
			_isNotNullOrEmptyConverterPage,
			nameof(IsNotNullOrEmptyConverter),
			"A converter that allows users to convert an incoming binding to a bool value. This value represents if the incoming binding value is Not null or empty."),
		new SectionModel(
			_invertedBoolConverterPage,
			nameof(InvertedBoolConverter),
			"A converter that allows users to convert a bool value binding to its inverted value.."),
		new SectionModel(
			_equalConverterPage,
			nameof(EqualConverter),
			"A converter that allows users to convert any value binding to a bool depending on whether or not it is equal to a different value. "),
		new SectionModel(
			_notEqualConverterPage,
			nameof(NotEqualConverter),
			"A converter that allows users to convert any value binding to a bool depending on whether or not it is not equal to a different value. "),
		new SectionModel(
			_doubleToIntConverterPage,
			nameof(DoubleToIntConverter),
			"A converter that allows users to convert an incoming double value to an int."),
		new SectionModel(
			_indexToArrayItemConverterPage,
			nameof(IndexToArrayItemConverter),
			"A converter that allows users to convert a int value binding to an item in an array."),
		new SectionModel(
			_intToBoolConverterPage,
			nameof(IntToBoolConverter),
			"A converter that allows users to convert an incoming int value to a bool."),
		new SectionModel(
			_itemTappedEventArgsPage,
			nameof(ItemTappedEventArgsConverter),
			"A converter that allows you to extract the value from ItemTappedEventArgs that can be used in combination with EventToCommandBehavior"),
		new SectionModel(
			_textCaseConverterPage,
			nameof(TextCaseConverter),
			"A converter that allows users to convert the casing of an incoming string type binding. The Type property is used to define what kind of casing will be applied to the string."),
		new SectionModel(
			_multiConverterPage,
			nameof(MultiConverter),
			"This sample demonstrates how to use Multibinding Converter"),
		new SectionModel(
			_dateTimeOffsetConverterPage,
			nameof(DateTimeOffsetConverter),
			"A converter that allows to convert from a DateTimeOffset type to a DateTime type"),
		new SectionModel(
			_variableMultiValueConverterPage,
			nameof(VariableMultiValueConverter),
			"A converter that allows you to combine multiple boolean bindings into a single binding."),
		new SectionModel(
			_listIsNullOrEmptyConverterPage,
			nameof(ListIsNullOrEmptyConverter),
			"A converter that allows you to check if collection is null or empty"),
		new SectionModel(
			_listIsNotNullOrEmptyConverterPage,
			nameof(ListIsNotNullOrEmptyConverter),
			"A converter that allows you to check if collection is not null or empty"),
		new SectionModel(
			_listToStringConverterPage,
			nameof(ListToStringConverter),
			"A converter that allows users to convert an incoming binding that implements IEnumerable to a single string value. The Separator property is used to join the items in the IEnumerable."),
		new SectionModel(
			_enumToBoolConverterPage,
			nameof(EnumToBoolConverter),
			"A converter that allows you to convert an Enum to boolean value"),
		new SectionModel(
			_enumToIntConverterPage,
			nameof(EnumToIntConverter),
			"A converter that allows you to convert an Enum to its underlying int value"),
		new SectionModel(
			_imageResourceConverterPage,
			nameof(ImageResourceConverter),
			"A converter that allows you to convert embeded ressource image id to an ImageSource"),
		new SectionModel(
			_mathExpressionConverterPage,
			nameof(MathExpressionConverter),
			"A converter that allows users to calculate an expression at runtime."),
		new SectionModel(
			_stringToListConverterPage,
			nameof(StringToListConverter),
			"A converter that splits a string by the separator and returns the enumerable sequence of strings as the result."),
		new SectionModel(
			_imageResourceConverterPage,
			nameof(ImageResourceConverter),
			"A converter that allows you to convert embeded ressource image id to an ImageSource"),
		new SectionModel(
			_colorsConverterPage,
			"ColorConverters",
			"A group of converters that convert a Color to your strings values (RGB, HEX, HSL, etc)"),
	};
}