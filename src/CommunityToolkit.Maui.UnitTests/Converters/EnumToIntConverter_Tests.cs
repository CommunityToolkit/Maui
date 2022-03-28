using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class EnumToIntConverter_Tests : BaseTest
{
	[Theory]
	[InlineData(TestEnum.None, 0)]
	[InlineData(TestEnum.One, 1)]
	[InlineData(TestEnum.FortyTwo, 42)]
	public void EnumToIntConvert_Validation(TestEnum value, int expectedResult)
	{
		var enumToIntConverter = new EnumToIntConverter();

		var convertResult = (int?)((ICommunityToolkitValueConverter)enumToIntConverter).Convert(value, typeof(int), null, null);
		var convertFromResult = enumToIntConverter.ConvertFrom(value, typeof(int), null, null);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(5.5)]
	[InlineData('c')]
	[InlineData("abc")]
	[InlineData(true)]
	public void EnumToIntConvert_ValueNotEnum_ThrowsArgumentException(object value)
	{
		var enumToIntConverter = new EnumToIntConverter();
		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)enumToIntConverter).Convert(value, typeof(int), null, null));
	}

	[Fact]
	public void EnumToIntConvert_ValueNull_ThrowsArgumentNullException()
	{
		var enumToIntConverter = new EnumToIntConverter();
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)enumToIntConverter).Convert(null, typeof(int), null, null));
	}

	[Theory]
	[InlineData(0, TestEnum.None)]
	[InlineData(1, TestEnum.One)]
	[InlineData(42, TestEnum.FortyTwo)]
	public void EnumToIntConvertBack_Validation(int value, TestEnum expectedResult)
	{
		var enumToIntConverter = new EnumToIntConverter();

		var convertBackResult = ((ICommunityToolkitValueConverter)enumToIntConverter).ConvertBack(value, typeof(TestEnum), null, null);
		var convertBackToResult = enumToIntConverter.ConvertBackTo(value, typeof(TestEnum), null, null);

		Assert.Equal(expectedResult, convertBackResult);
		Assert.Equal(expectedResult, convertBackToResult);
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(3)]
	[InlineData("a string")]
	public void EnumToIntConvertBack_ValueNotInEnum_ThrowsArgumentException(object value)
	{
		var enumToIntConverter = new EnumToIntConverter();
		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)enumToIntConverter).ConvertBack(value, typeof(TestEnum), null, null));
	}

	[Fact]
	public void EnumToIntConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new EnumToIntConverter()).Convert(TestEnum.FortyTwo, null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new EnumToIntConverter()).Convert(null, typeof(int), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new EnumToIntConverter()).ConvertBack(1, null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new EnumToIntConverter()).ConvertBack(null, typeof(TestEnum), null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	public enum TestEnum
	{
		None = 0,
		One = 1,
		FortyTwo = 42,
	}
}