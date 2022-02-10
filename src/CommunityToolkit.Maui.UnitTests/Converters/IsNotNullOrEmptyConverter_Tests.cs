using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class IsNotNullOrEmptyConverter_Tests : BaseTest
{
	[Theory]
	[InlineData("Test", true)]
	[InlineData(typeof(IsStringNotNullOrEmptyConverter), true)]
	[InlineData(null, false)]
	[InlineData("", false)]
	public void IsNotNullOrEmptyConverter(object value, bool expectedResult)
	{
		var isNotNullOrEmptyConverter = new IsStringNotNullOrEmptyConverter();

		var result = (bool)isNotNullOrEmptyConverter.Convert(value, typeof(IsNotNullOrEmptyConverter_Tests), null, CultureInfo.CurrentCulture);

		Assert.Equal(result, expectedResult);
	}
}