using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class IntToBoolConverter_Tests : BaseTest
{
	[Theory]
	[InlineData(1, true)]
	[InlineData(0, false)]
	public void IndexToArrayConverter(int value, bool expectedResult)
	{
		var intToBoolConverter = new IntToBoolConverter();

		var result = intToBoolConverter.Convert(value, typeof(bool), null, CultureInfo.CurrentCulture);
		var typedResult = intToBoolConverter.ConvertFrom(value);

		Assert.Equal(result, expectedResult);
		Assert.Equal(typedResult, expectedResult);
	}

	[Theory]
	[InlineData(true, 1)]
	[InlineData(false, 0)]
	public void IndexToArrayConverterBack(bool value, int expectedResult)
	{
		var intToBoolConverter = new IntToBoolConverter();

		var result = intToBoolConverter.ConvertBack(value, typeof(bool), null, CultureInfo.CurrentCulture);
		var typedResult = intToBoolConverter.ConvertBackTo(value);

		Assert.Equal(result, expectedResult);
		Assert.Equal(typedResult, expectedResult);
	}

	[Theory]
	[InlineData(2.5)]
	[InlineData("")]
	public void InValidConverterValuesThrowArgumentException(object value)
	{
		var intToBoolConverter = new IntToBoolConverter();
		Assert.Throws<ArgumentException>(() => intToBoolConverter.Convert(value, typeof(bool), null, CultureInfo.CurrentCulture));
	}

	[Theory]
	[InlineData(2.5)]
	[InlineData("")]
	[InlineData(null)]
	public void InValidConverterBackValuesThrowArgumentException(object value)
	{
		var intToBoolConverter = new IntToBoolConverter();
		Assert.Throws<ArgumentException>(() => intToBoolConverter.ConvertBack(value, typeof(bool), null, CultureInfo.CurrentCulture));
	}

	[Fact]
	public void NullConverterValueThrowsArgumentNullException()
	{
		var intToBoolConverter = new IntToBoolConverter();
		Assert.Throws<ArgumentNullException>(() => intToBoolConverter.Convert(null, typeof(bool), null, CultureInfo.CurrentCulture));
	}
}