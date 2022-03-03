using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class IsStringNullOrEmptyConverter_Tests : BaseTest
{
	[Theory]
	[InlineData(null, true)]
	[InlineData("", true)]
	[InlineData("Test", false)]
	[InlineData(" ", false)]
	public void IsNullOrEmptyConverter_ValidStringValue(string? value, bool expectedResult)
	{
		var isNullOrEmptyConverter = new IsStringNullOrEmptyConverter();

		var convertResult = (bool?)isNullOrEmptyConverter.Convert(value, typeof(bool), null, null);
		var convertFromResult = isNullOrEmptyConverter.ConvertFrom(value);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[InlineData(17)]
	[InlineData(true)]
	[InlineData('c')]
	public void IsNullOrEmptyConverter_InvalidValue(object value)
	{
		var isNotNullOrEmptyConverter = new IsStringNullOrEmptyConverter();

		Assert.Throws<ArgumentException>(() => isNotNullOrEmptyConverter.Convert(value, typeof(bool), null, null));
	}
}