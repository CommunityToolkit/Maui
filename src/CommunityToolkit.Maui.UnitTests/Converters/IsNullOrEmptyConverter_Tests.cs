using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class IsNullOrEmptyConverter_Tests : BaseTest
{
	[Theory]
	[InlineData(null, null, true)]
	[InlineData(null, true, true)]
	[InlineData(null, false, true)]
	[InlineData("", null, true)]
	[InlineData("", true, true)]
	[InlineData("", false, true)]
	[InlineData(" ", null, false)]
	[InlineData(" ", true, true)]
	[InlineData(" ", false, false)]
	[InlineData("Test", null, false)]
	[InlineData("Test", true, false)]
	[InlineData("Test", false, false)]
	[InlineData(typeof(IsNullOrEmptyConverter), null, false)]
	[InlineData(typeof(IsNullOrEmptyConverter), true, false)]
	[InlineData(typeof(IsNullOrEmptyConverter), false, false)]
	public void IsNullOrEmptyConverter(object value, object parameter, bool expectedResult)
	{
		var isNullOrEmptyConverter = new IsNullOrEmptyConverter();

		var result = (bool)isNullOrEmptyConverter.Convert(value, typeof(IsNotNullOrEmptyConverter_Tests), parameter, CultureInfo.CurrentCulture);

		Assert.Equal(result, expectedResult);
	}
}