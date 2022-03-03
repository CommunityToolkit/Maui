using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class IsStringNullOrWhiteSpaceConverter_Tests : BaseTest
{
	[Theory]
	[InlineData(null, true)]
	[InlineData("", true)]
	[InlineData("Test", false)]
	[InlineData(" ", true)]
	[InlineData("         ", true)]
	public void IsNullOrWhiteSpaceConverter_ValidStringValue(string? value, bool expectedResult)
	{
		var isNullOrWhiteSpaceConverter = new IsStringNullOrWhiteSpaceConverter();

		var convertResult = (bool?)isNullOrWhiteSpaceConverter.Convert(value, typeof(bool), null, null);
		var convertFromResult = isNullOrWhiteSpaceConverter.ConvertFrom(value);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[InlineData(17)]
	[InlineData(true)]
	[InlineData('c')]
	public void IsNullOrWhiteSpaceConverter_InvalidValue(object value)
	{
		var isNotNullOrWhiteSpaceConverter = new IsStringNullOrWhiteSpaceConverter();

		Assert.Throws<ArgumentException>(() => isNotNullOrWhiteSpaceConverter.Convert(value, typeof(bool), null, null));
	}
}