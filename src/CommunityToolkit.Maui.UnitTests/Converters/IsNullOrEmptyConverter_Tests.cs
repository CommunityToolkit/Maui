using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class IsNullOrEmptyConverter_Tests : BaseTest
{
	[Theory]
	[InlineData(null, true)]
	[InlineData("", true)]
	[InlineData("Test", false)]
	[InlineData(typeof(IsStringNullOrEmptyConverter), false)]
	public void IsNullOrEmptyConverter(object value, bool expectedResult)
	{
		var isNullOrEmptyConverter = new IsStringNullOrEmptyConverter();

		var result = (bool)isNullOrEmptyConverter.Convert(value, typeof(IsNotNullOrEmptyConverter_Tests), null, CultureInfo.CurrentCulture);

		Assert.Equal(result, expectedResult);
	}
}