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

		var result = (bool)intToBoolConverter.Convert(value, typeof(bool), null, CultureInfo.CurrentCulture);

		Assert.Equal(result, expectedResult);
	}

	[Theory]
	[InlineData(true, 1)]
	[InlineData(false, 0)]
	public void IndexToArrayConverterBack(bool value, int expectedResult)
	{
		var intToBoolConverter = new IntToBoolConverter();

		var result = (int)intToBoolConverter.ConvertBack(value, typeof(bool), null, CultureInfo.CurrentCulture);

		Assert.Equal(result, expectedResult);
	}

	[Theory]
	[InlineData(2.5)]
	[InlineData("")]
	[InlineData(null)]
	public void InValidConverterValuesThrowArgumenException(object value)
	{
		var intToBoolConverter = new IntToBoolConverter();
		Assert.Throws<ArgumentException>(() => intToBoolConverter.Convert(value, typeof(bool), null, CultureInfo.CurrentCulture));
	}

	[Theory]
	[InlineData(2.5)]
	[InlineData("")]
	[InlineData(null)]
	public void InValidConverterBackValuesThrowArgumenException(object value)
	{
		var intToBoolConverter = new IntToBoolConverter();
		Assert.Throws<ArgumentException>(() => intToBoolConverter.ConvertBack(value, typeof(bool), null, CultureInfo.CurrentCulture));
	}
}