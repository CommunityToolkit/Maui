using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class IsStringNotNullOrEmptyConverter_Tests : BaseTest
{
	[Theory]
	[InlineData("Test", true)]
	[InlineData(null, false)]
	[InlineData("", false)]
	[InlineData(" ", true)]
	public void IsStringNotNullOrEmptyConverter_ValidStringValue(string? value, bool expectedResult)
	{
		var isNotNullOrEmptyConverter = new IsStringNotNullOrEmptyConverter();

		var convertResult = (bool?)isNotNullOrEmptyConverter.Convert(value, typeof(bool), null, null);
		var convertFromResult = isNotNullOrEmptyConverter.ConvertFrom(value);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[InlineData(17)]
	[InlineData(true)]
	[InlineData('c')]
	public void IsStringNotNullOrEmptyConverter_InvalidValue(object value)
	{
		var isNotNullOrEmptyConverter = new IsStringNotNullOrEmptyConverter();

		Assert.Throws<ArgumentException>(() => isNotNullOrEmptyConverter.Convert(value, typeof(bool), null, null));
	}
}