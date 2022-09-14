using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class DoubleToIntConverterTests : BaseConverterTest<DoubleToIntConverter>
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
		var convertFromResult = doubleToIntConverter.ConvertFrom(value, ratio);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Fact]
	public void DoubleToIntConverterParameter()
	{
		const int expectedResult = 10;
		const double value = 2;
		var doubleToIntConverter = new DoubleToIntConverter
		{
			Ratio = 5
		};

		var convertResult = (int?)((ICommunityToolkitValueConverter)doubleToIntConverter).Convert(value, typeof(int), null, CultureInfo.CurrentCulture);
		var convertFromResult = doubleToIntConverter.ConvertFrom(value);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[InlineData(2, 2.0)]
	[InlineData(5, 5.0)]
	public void DoubleToIntConverterBack(int value, double expectedResult)
	{
		var doubleToIntConverter = new DoubleToIntConverter();

		var convertBackResult = (double?)((ICommunityToolkitValueConverter)doubleToIntConverter).ConvertBack(value, typeof(double), null, CultureInfo.CurrentCulture);
		var convertBackToResult = doubleToIntConverter.ConvertBackTo(value);

		Assert.Equal(expectedResult, convertBackResult);
		Assert.Equal(expectedResult, convertBackToResult);
	}

	[Theory]
	[InlineData('c')]
	[InlineData(true)]
	[InlineData("abc")]
	public void DoubleToIntInvalidValuesThrowArgumentException(object value)
	{
		var doubleToIntConverter = new DoubleToIntConverter();
		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)doubleToIntConverter).Convert(value, typeof(int), null, CultureInfo.CurrentCulture));
		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)doubleToIntConverter).ConvertBack(value, typeof(double), null, CultureInfo.CurrentCulture));
	}

	[Theory]
	[InlineData('c')]
	[InlineData(true)]
	[InlineData("abc")]
	public void DoubleToIntInvalidParameterThrowArgumentException(object value)
	{
		var doubleToIntConverter = new DoubleToIntConverter();
		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)doubleToIntConverter).Convert(10, typeof(int), value, CultureInfo.CurrentCulture));
		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)doubleToIntConverter).ConvertBack(10, typeof(double), value, CultureInfo.CurrentCulture));
	}

	[Fact]
	public void DoubleToIntInvalidConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new DoubleToIntConverter()).Convert(0.0, null, 0, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new DoubleToIntConverter()).Convert(null, typeof(double), 0, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new DoubleToIntConverter()).ConvertBack(0, null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new DoubleToIntConverter()).ConvertBack(null, typeof(int), null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}