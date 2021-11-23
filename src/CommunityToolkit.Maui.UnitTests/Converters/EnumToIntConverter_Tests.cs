using System;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class EnumToIntConverter_Tests : BaseTest
{
	enum TestEnumForEnumToIntConverter
	{
		None,
		One,
		FortyTwo = 42,
	}

	[Theory]
	[InlineData(TestEnumForEnumToIntConverter.None, 0)]
	[InlineData(TestEnumForEnumToIntConverter.One, 1)]
	[InlineData(TestEnumForEnumToIntConverter.FortyTwo, 42)]
	public void EnumToIntConvert_Validation(object? value, int expectedResult)
	{
		var enumToIntConverter = new EnumToIntConverter();
		var result = (int)enumToIntConverter.Convert(value, targetType: null, parameter: null, culture: null);
		Assert.Equal(expectedResult, result);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("a string")]
	public void EnumToIntConvert_ValueNotEnum_ThrowsArgumentException(object value)
	{
		var enumToIntConverter = new EnumToIntConverter();
		Assert.Throws<ArgumentException>(() => enumToIntConverter.Convert(value, targetType: null, parameter: null, culture: null));
	}

	[Theory]
	[InlineData(0, TestEnumForEnumToIntConverter.None)]
	[InlineData(1, TestEnumForEnumToIntConverter.One)]
	[InlineData(42, TestEnumForEnumToIntConverter.FortyTwo)]
	public void EnumToIntConvertBack_Validation(object? value, object expectedResult)
	{
		var enumToIntConverter = new EnumToIntConverter();
		var result = (object)enumToIntConverter.ConvertBack(value, typeof(TestEnumForEnumToIntConverter), parameter: null, culture: null);
		Assert.Equal(expectedResult, result);
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(3)]
	[InlineData("a string")]
	public void EnumToIntConvertBack_ValueNotInEnum_ThrowsArgumentException(object value)
	{
		var enumToIntConverter = new EnumToIntConverter();
		Assert.Throws<ArgumentException>(() => enumToIntConverter.ConvertBack(value, typeof(TestEnumForEnumToIntConverter), parameter: null, culture: null));
	}
}