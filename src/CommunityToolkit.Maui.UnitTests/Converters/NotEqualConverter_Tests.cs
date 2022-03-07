using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class NotEqualConverter_Tests : BaseTest
{
	[Theory]
	[InlineData(true, true, false)]
	[InlineData(int.MaxValue, int.MinValue, true)]
	[InlineData("Test", true, true)]
	[InlineData(null, null, false)]
	public void NotEqualConverter(object value, object comparedValue, bool expectedResult)
	{
		var notEqualConverter = new NotEqualConverter();

		var result = (bool)notEqualConverter.Convert(value, typeof(bool), comparedValue, CultureInfo.CurrentCulture);

		Assert.Equal(result, expectedResult);
	}
}