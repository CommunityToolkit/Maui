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
	public void IsNotNullOrEmptyConverter_ValidStringValue(string? value, bool expectedResult)
	{
		var isNotNullOrEmptyConverter = new IsStringNotNullOrEmptyConverter();

		var result = (bool)isNotNullOrEmptyConverter.Convert(value, null, null, null);

		Assert.Equal(expectedResult, result);
	}

	[Theory]
	[InlineData(17)]
	[InlineData(true)]
	[InlineData('c')]
	public void IsNotNullOrEmptyConverter_InvalidValue(object value)
	{
		var isNotNullOrEmptyConverter = new IsStringNotNullOrEmptyConverter();

		Assert.Throws<InvalidCastException>(() => isNotNullOrEmptyConverter.Convert(value, null, null, null));
	}
}