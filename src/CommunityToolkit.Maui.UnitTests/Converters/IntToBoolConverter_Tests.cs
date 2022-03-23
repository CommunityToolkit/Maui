using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class IntToBoolConverter_Tests : BaseTest
{
	[Theory]
	[InlineData(int.MaxValue, true)]
	[InlineData(2, true)]
	[InlineData(1, true)]
	[InlineData(0, false)]
	[InlineData(-1, true)]
	[InlineData(int.MinValue, true)]
	public void IntToBoolConverter(int value, bool expectedResult)
	{
		var intToBoolConverter = new IntToBoolConverter();

		var result = ((ICommunityToolkitValueConverter)intToBoolConverter).Convert(value, typeof(bool), null, CultureInfo.CurrentCulture);
		var typedResult = intToBoolConverter.ConvertFrom(value, typeof(bool), null, CultureInfo.CurrentCulture);

		Assert.Equal(expectedResult, result);
		Assert.Equal(expectedResult, typedResult);
	}

	[Theory]
	[InlineData(true, 1)]
	[InlineData(false, 0)]
	public void IntToBoolConverterBack(bool value, int expectedResult)
	{
		var intToBoolConverter = new IntToBoolConverter();

		var result = ((ICommunityToolkitValueConverter)intToBoolConverter).ConvertBack(value, typeof(int), null, CultureInfo.CurrentCulture);
		var typedResult = intToBoolConverter.ConvertBackTo(value, typeof(bool), null, CultureInfo.CurrentCulture);

		Assert.Equal(expectedResult, result);
		Assert.Equal(expectedResult, typedResult);
	}

	[Theory]
	[InlineData(2.5)]
	[InlineData("")]
	[InlineData('c')]
	public void InvalidConverterValuesThrowArgumentException(object value)
	{
		var intToBoolConverter = new IntToBoolConverter();
		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)intToBoolConverter).Convert(value, typeof(bool), null, CultureInfo.CurrentCulture));
	}

	[Fact]
	public void NullConverterValueThrowsArgumentNullException()
	{
		var intToBoolConverter = new IntToBoolConverter();
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)intToBoolConverter).Convert(null, typeof(bool), null, CultureInfo.CurrentCulture));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)intToBoolConverter).ConvertBack(null, typeof(bool), null, CultureInfo.CurrentCulture)); ;
	}

	[Theory]
	[InlineData(2.5)]
	[InlineData("")]
	[InlineData('c')]
	public void InvalidConverterBackValuesThrowArgumentException(object value)
	{
		var intToBoolConverter = new IntToBoolConverter();
		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)intToBoolConverter).ConvertBack(value, typeof(int), null, CultureInfo.CurrentCulture));
	}
}