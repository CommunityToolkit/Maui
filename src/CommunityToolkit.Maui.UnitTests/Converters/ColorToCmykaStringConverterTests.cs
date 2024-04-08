using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ColorToCmykaStringConverterTests : BaseOneWayConverterTest<ColorToCmykaStringConverter>
{
	public static readonly IReadOnlyList<object[]> ValidInputData =
	[
		[int.MinValue, int.MinValue, int.MinValue, int.MinValue, "CMYKA(0%,0%,0%,100%,0)"],
		[int.MinValue, int.MinValue, int.MinValue, int.MaxValue, "CMYKA(0%,0%,0%,100%,1)"],
		[0, 0, 0, int.MinValue, "CMYKA(0%,0%,0%,100%,0)"],
		[0, 0, 0, -0.5, "CMYKA(0%,0%,0%,100%,0)"],
		[0, 0, 0, 0, "CMYKA(0%,0%,0%,100%,0)"],
		[0, 0, 0, 0.5, "CMYKA(0%,0%,0%,100%,0.5)"],
		[0, 0, 0, 1, "CMYKA(0%,0%,0%,100%,1)"],
		[0, 0, 0, int.MaxValue, "CMYKA(0%,0%,0%,100%,1)"],
		[int.MaxValue, int.MaxValue, int.MaxValue, int.MinValue, "CMYKA(0%,0%,0%,0%,0)"],
		[int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, "CMYKA(0%,0%,0%,0%,1)"],
		[0, 0, 1, 0, "CMYKA(100%,100%,0%,0%,0)"],
		[0, 0, 1, 1, "CMYKA(100%,100%,0%,0%,1)"],
		[0, 1, 0, 0, "CMYKA(100%,0%,100%,0%,0)"],
		[0, 1, 0, 1, "CMYKA(100%,0%,100%,0%,1)"],
		[0, 1, 1, 0, "CMYKA(100%,0%,0%,0%,0)"],
		[0, 1, 1, 1, "CMYKA(100%,0%,0%,0%,1)"],
		[1, 0, 0, 0, "CMYKA(0%,100%,100%,0%,0)"],
		[1, 0, 0, 1, "CMYKA(0%,100%,100%,0%,1)"],
		[1, 0, 1, 0, "CMYKA(0%,100%,0%,0%,0)"],
		[1, 0, 1, 1, "CMYKA(0%,100%,0%,0%,1)"],
		[1, 1, 0, 0, "CMYKA(0%,0%,100%,0%,0)"],
		[1, 1, 0, 1, "CMYKA(0%,0%,100%,0%,1)"],
		[1, 1, 1, 0, "CMYKA(0%,0%,0%,0%,0)"],
		[1, 1, 1, 1, "CMYKA(0%,0%,0%,0%,1)"],
		[0.5, 0, 0, 1, "CMYKA(0%,100%,100%,50%,1)"],
		[0.5, 0, 0, 0, "CMYKA(0%,100%,100%,50%,0)"],
		[0, 0.5, 0, 1, "CMYKA(100%,0%,100%,50%,1)"],
		[0, 0.5, 0, 0, "CMYKA(100%,0%,100%,50%,0)"],
		[0, 0, 0.5, 1, "CMYKA(100%,100%,0%,50%,1)"],
		[0, 0, 0.5, 0, "CMYKA(100%,100%,0%,50%,0)"],
		[0.5, 0.5, 0.5, 1, "CMYKA(0%,0%,0%,50%,1)"],
		[0.5, 0.5, 0.5, 0, "CMYKA(0%,0%,0%,50%,0)"],
		[0.25, 0.25, 0.25, 1, "CMYKA(0%,0%,0%,75%,1)"],
		[0.25, 0.25, 0.25, 0, "CMYKA(0%,0%,0%,75%,0)"],
		[0.25, 0.25, 1, 1, "CMYKA(75%,75%,0%,0%,1)"],
		[0.25, 0.25, 1, 0, "CMYKA(75%,75%,0%,0%,0)"],
		[0.25, 1, 0.25, 1, "CMYKA(75%,0%,75%,0%,1)"],
		[0.25, 1, 0.25, 0, "CMYKA(75%,0%,75%,0%,0)"],
		[0.75, 1, 0.25, 1, "CMYKA(25%,0%,75%,0%,1)"],
		[0.75, 1, 0.25, 0, "CMYKA(25%,0%,75%,0%,0)"],
		[0.75, 0, 1, 1, "CMYKA(25%,100%,0%,0%,1)"],
		[0.75, 0, 1, 0, "CMYKA(25%,100%,0%,0%,0)"],
	];

	[Theory]
	[MemberData(nameof(ValidInputData))]
	public void ColorToCmykaStringConverterValidInputTest(float red, float green, float blue, float alpha, string expectedResult)
	{
		var converter = new ColorToCmykaStringConverter();
		var color = new Color(red, green, blue, alpha);

		var resultConvert = ((ICommunityToolkitValueConverter)converter).Convert(color, typeof(string), null, new System.Globalization.CultureInfo("en-US"));
		var resultConvertFrom = converter.ConvertFrom(color, new System.Globalization.CultureInfo("en-US"));

		Assert.Equal(expectedResult, resultConvert);
		Assert.Equal(expectedResult, resultConvertFrom);
	}

	[Fact]
	public void ColorToRgbStringConverterCultureTest()
	{
		var expectedResult = "CMYKA(0%,0%,0%,100%,0,5)";
		var converter = new ColorToCmykaStringConverter();
		var color = new Color(0, 0, 0, 0.5f);

		var resultConvert = ((ICommunityToolkitValueConverter)converter).Convert(color, typeof(string), null, new System.Globalization.CultureInfo("uk-UA"));
		var resultConvertFrom = converter.ConvertFrom(color, new System.Globalization.CultureInfo("uk-UA"));

		Assert.Equal(expectedResult, resultConvert);
		Assert.Equal(expectedResult, resultConvertFrom);
	}

	[Fact]
	public void ColorToCmykaStringConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => new ColorToCmykaStringConverter().ConvertFrom(null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToCmykaStringConverter()).Convert(null, typeof(string), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToCmykaStringConverter()).Convert(new Color(), null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}