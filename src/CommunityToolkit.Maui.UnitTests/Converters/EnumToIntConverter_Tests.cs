using CommunityToolkit.Maui.Converters;
using NUnit.Framework;

namespace CommunityToolkit.Maui.UnitTests.Converters
{
    public enum TestEnumForEnumToIntConverter
	{
		None,
		One,
		FortyTwo = 42,
	}

	public class EnumToIntConverter_Tests
	{
		[TestCase(TestEnumForEnumToIntConverter.None, 0)]
		[TestCase(TestEnumForEnumToIntConverter.One, 1)]
		[TestCase(TestEnumForEnumToIntConverter.FortyTwo, 42)]
		public void EnumToIntConvert_Validation(object? value, int expectedResult)
		{
			var enumToIntConverter = new EnumToIntConverter();
			var result = enumToIntConverter.Convert(value, targetType: null, parameter: null, culture: null);
			Assert.AreEqual(expectedResult, result);
		}

		[TestCase(null)]
		[TestCase("a string")]
		public void EnumToIntConvert_ValueNotEnum_ThrowsArgumentException(object value)
		{
			var enumToIntConverter = new EnumToIntConverter();
			Assert.Throws<ArgumentException>(() => enumToIntConverter.Convert(value, targetType: null, parameter: null, culture: null));
		}

		[TestCase(0, TestEnumForEnumToIntConverter.None)]
		[TestCase(1, TestEnumForEnumToIntConverter.One)]
		[TestCase(42, TestEnumForEnumToIntConverter.FortyTwo)]
		public void EnumToIntConvertBack_Validation(object? value, object expectedResult)
		{
			var enumToIntConverter = new EnumToIntConverter();
			var result = enumToIntConverter.ConvertBack(value, typeof(TestEnumForEnumToIntConverter), parameter: null, culture: null);
			Assert.AreEqual(expectedResult, result);
		}

		[TestCase(-1)]
		[TestCase(3)]
		[TestCase("a string")]
		public void EnumToIntConvertBack_ValueNotInEnum_ThrowsArgumentException(object value)
		{
			var enumToIntConverter = new EnumToIntConverter();
			Assert.Throws<ArgumentException>(() => enumToIntConverter.ConvertBack(value, typeof(TestEnumForEnumToIntConverter), parameter: null, culture: null));
		}
	}
}