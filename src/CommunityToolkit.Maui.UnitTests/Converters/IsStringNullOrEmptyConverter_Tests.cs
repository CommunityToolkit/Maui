using System.Globalization;
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

		var result = (bool)isNullOrEmptyConverter.Convert(value, null, null, null);

		Assert.Equal(expectedResult, result);
	}

	[Theory]
	[InlineData(17)]
	[InlineData(true)]
	[InlineData('c')]
	public void IsNullOrEmptyConverter_InvalidValue(object value)
	{
		var isNotNullOrEmptyConverter = new IsStringNullOrEmptyConverter();

		Assert.Throws<InvalidCastException>(() => isNotNullOrEmptyConverter.Convert(value, null, null, null));
	}
}