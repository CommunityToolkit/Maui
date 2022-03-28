using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class IsEqualConverter_Tests : BaseTest
{
	public const string TestValue = nameof(TestValue);

	[Theory]
	[InlineData(true, true, true)]
	[InlineData(int.MaxValue, int.MinValue, false)]
	[InlineData("Test", true, false)]
	[InlineData(null, null, true)]
	[InlineData(null, true, false)]
	public void IsEqualConverterValidInputTest(object? value, object? comparedValue, bool expectedResult)
	{
		var isEqualConverter = new IsEqualConverter();

		var convertResult = (bool?)((ICommunityToolkitValueConverter)isEqualConverter).Convert(value, typeof(bool), comparedValue, CultureInfo.CurrentCulture);
		var convertFromResult = isEqualConverter.ConvertFrom(value, typeof(bool), comparedValue, CultureInfo.CurrentCulture);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Fact]
	public void IsEqualConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new IsEqualConverter()).Convert(true, null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new IsEqualConverter()).ConvertBack(true, null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}