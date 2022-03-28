using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class DoubleToIntConverter_Tests : BaseTest
{
	[Theory]
	[InlineData(2.5, 2)]
	[InlineData(2.55, 3)]
	[InlineData(2.555, 3)]
	[InlineData(2.555, 652, 255)]
	public void DoubleToIntConverter(double value, int expectedResult, object? ratio = null)
	{
		var doubleToIntConverter = new DoubleToIntConverter();

		var convertResult = (int?)((ICommunityToolkitValueConverter)doubleToIntConverter).Convert(value, typeof(int), ratio, CultureInfo.CurrentCulture);
		var convertFromResult = doubleToIntConverter.ConvertFrom(value, typeof(int), ratio, CultureInfo.CurrentCulture);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[InlineData(2, 2.0)]
	[InlineData(5, 5.0)]
	public void DoubleToIntConverterBack(int value, double expectedResult, object? ratio = null)
	{
		var doubleToIntConverter = new DoubleToIntConverter();

		var convertBackResult = (double?)((ICommunityToolkitValueConverter)doubleToIntConverter).ConvertBack(value, typeof(double), ratio, CultureInfo.CurrentCulture);
		var convertBackToResult = doubleToIntConverter.ConvertBackTo(value, typeof(double), ratio, CultureInfo.CurrentCulture);

		Assert.Equal(expectedResult, convertBackResult);
		Assert.Equal(expectedResult, convertBackToResult);
	}

	[Theory]
	[InlineData(5.5)]
	[InlineData('c')]
	[InlineData(true)]
	[InlineData("abc")]
	public void DoubleToIntInvalidValuesThrowArgumentException(object value)
	{
		var doubleToIntConverter = new DoubleToIntConverter();
		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)doubleToIntConverter).Convert(value, typeof(BoolToObjectConverter_Tests), null, CultureInfo.CurrentCulture));
		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)doubleToIntConverter).ConvertBack(value, typeof(BoolToObjectConverter_Tests), null, CultureInfo.CurrentCulture));
	}

	[Fact]
	public void DoubleToIntInvalidConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new DoubleToIntConverter()).Convert(0.0, null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new DoubleToIntConverter()).Convert(null, typeof(double), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new DoubleToIntConverter()).ConvertBack(0, null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new DoubleToIntConverter()).ConvertBack(null, typeof(int), null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}