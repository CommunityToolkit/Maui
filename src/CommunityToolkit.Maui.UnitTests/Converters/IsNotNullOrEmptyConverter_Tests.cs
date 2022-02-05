using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class IsNotNullOrEmptyConverter_Tests : BaseTest
{
	[Theory]
	[InlineData(null, null, false)]
	[InlineData(null, true, false)]
	[InlineData(null, false, false)]
	[InlineData("", null, false)]
	[InlineData("", true, false)]
	[InlineData("", false, false)]
	[InlineData(" ", null, true)]
	[InlineData(" ", true, false)]
	[InlineData(" ", false, true)]
	[InlineData("Test", null, true)]
	[InlineData("Test", true, true)]
	[InlineData("Test", false, true)]
	[InlineData(typeof(IsNotNullOrEmptyConverter), null, true)]
	[InlineData(typeof(IsNotNullOrEmptyConverter), true, true)]
	[InlineData(typeof(IsNotNullOrEmptyConverter), false, true)]
	public void IsNotNullOrEmptyConverter(object value, object parameter, bool expectedResult)
	{
		var isNotNullOrEmptyConverter = new IsNotNullOrEmptyConverter();

		var result = (bool)isNotNullOrEmptyConverter.Convert(value, typeof(IsNotNullOrEmptyConverter_Tests), parameter, CultureInfo.CurrentCulture);

		Assert.Equal(result, expectedResult);
	}
}