using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class IsNotEqualConverterTests : BaseTest
{
	[Theory]
	[InlineData(true, true, false)]
	[InlineData(int.MaxValue, int.MinValue, true)]
	[InlineData("Test", true, true)]
	[InlineData(null, null, false)]
	[InlineData(null, true, true)]
	public void IsNotEqualConverterValidInputTest(object? value, object? comparedValue, bool expectedResult)
	{
		var notEqualConverter = new IsNotEqualConverter();

		var convertResult = (bool?)((ICommunityToolkitValueConverter)notEqualConverter).Convert(value, typeof(bool), comparedValue, CultureInfo.CurrentCulture);
		var convertFromResult = notEqualConverter.ConvertFrom(value, comparedValue);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Fact]
	public void IsNotEqualConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new IsNotEqualConverter()).Convert(true, null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new IsNotEqualConverter()).ConvertBack(true, null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}