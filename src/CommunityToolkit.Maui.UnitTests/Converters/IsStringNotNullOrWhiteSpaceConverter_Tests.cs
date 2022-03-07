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

		var convertResult = (bool?)isNotNullOrWhiteSpaceConverter.Convert(value, typeof(bool), null, null);
		var convertFromResult = isNotNullOrWhiteSpaceConverter.ConvertFrom(value);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[InlineData(17)]
	[InlineData(true)]
	[InlineData('c')]
	public void IsNotNullOrWhiteSpaceConverter_InvalidValue(object value)
	{
		var isNotNullOrWhiteSpaceConverter = new IsStringNotNullOrWhiteSpaceConverter();

		Assert.Throws<ArgumentException>(() => isNotNullOrWhiteSpaceConverter.Convert(value, typeof(bool), null, null));
	}
}