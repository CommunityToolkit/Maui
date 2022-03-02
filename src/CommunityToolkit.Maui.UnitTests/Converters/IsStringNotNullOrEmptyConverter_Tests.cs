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

#pragma warning disable CS8605 // Unboxing a possibly null value.
		var baseResult = (bool)isNotNullOrEmptyConverter.Convert(value, typeof(bool), null, null);
#pragma warning restore CS8605 // Unboxing a possibly null value.

		var result = isNotNullOrEmptyConverter.ConvertFrom(value);

		Assert.Equal(expectedResult, baseResult);
		Assert.Equal(expectedResult, result);
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