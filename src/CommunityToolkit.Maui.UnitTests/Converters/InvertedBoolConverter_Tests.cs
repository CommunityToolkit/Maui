using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class InvertedBoolConverter_Tests : BaseTest
{
	[Theory]
	[InlineData(true, false)]
	[InlineData(false, true)]
	public void InverterBoolConverter(bool value, bool expectedResult)
	{
		var invertedBoolConverter = new InvertedBoolConverter();

		var convertResult = (bool?)((ICommunityToolkitValueConverter)invertedBoolConverter).Convert(value, typeof(bool), null, CultureInfo.CurrentCulture);
		var convertFromResult = invertedBoolConverter.ConvertFrom(value, typeof(bool), null, CultureInfo.CurrentCulture);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[InlineData(2)]
	[InlineData("")]
	[InlineData(null)]
	public void InvalidConverterValuesThrowArgumenException(object? value)
	{
		var invertedBoolConverter = new InvertedBoolConverter();
		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)invertedBoolConverter).Convert(value, typeof(bool), null, CultureInfo.CurrentCulture));
	}
}