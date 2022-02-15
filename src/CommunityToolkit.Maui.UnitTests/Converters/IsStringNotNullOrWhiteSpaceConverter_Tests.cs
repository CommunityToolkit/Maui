using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class IsStringNotNullOrWhiteSpaceConverter_Tests : BaseTest
{
	[Theory]
	[InlineData("Test", true)]
	[InlineData(null, false)]
	[InlineData("", false)]
	[InlineData(" ", false)]
	[InlineData("         ", false)]
	public void IsNotNullOrWhiteSpaceConverter_ValidStringValue(string? value, bool expectedResult)
	{
		var isNotNullOrWhiteSpaceConverter = new IsStringNotNullOrWhiteSpaceConverter();

		var result = (bool)isNotNullOrWhiteSpaceConverter.Convert(value, null, null, null);

		Assert.Equal(expectedResult, result);
	}

	[Theory]
	[InlineData(17)]
	[InlineData(true)]
	[InlineData('c')]
	public void IsNotNullOrWhiteSpaceConverter_InvalidValue(object value)
	{
		var isNotNullOrWhiteSpaceConverter = new IsStringNotNullOrWhiteSpaceConverter();

		Assert.Throws<InvalidCastException>(() => isNotNullOrWhiteSpaceConverter.Convert(value, null, null, null));
	}
}