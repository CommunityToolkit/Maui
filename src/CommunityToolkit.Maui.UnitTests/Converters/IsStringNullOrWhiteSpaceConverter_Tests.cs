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

		var result = (bool)isNullOrWhiteSpaceConverter.Convert(value, null, null, null);

		Assert.Equal(expectedResult, result);
	}

	[Theory]
	[InlineData(17)]
	[InlineData(true)]
	[InlineData('c')]
	public void IsNullOrWhiteSpaceConverter_InvalidValue(object value)
	{
		var isNotNullOrWhiteSpaceConverter = new IsStringNullOrWhiteSpaceConverter();

		Assert.Throws<InvalidCastException>(() => isNotNullOrWhiteSpaceConverter.Convert(value, null, null, null));
	}
}