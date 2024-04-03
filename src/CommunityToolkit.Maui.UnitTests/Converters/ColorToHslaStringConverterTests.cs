using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ColorToHslaStringConverterTests : BaseOneWayConverterTest<ColorToHslaStringConverter>
{
	public static readonly IReadOnlyList<object[]> ValidInputData =
	[
		[int.MinValue, int.MinValue, int.MinValue, int.MinValue, "HSLA(0,0%,0%,0)"],
		[0, 0, 0, int.MinValue, "HSLA(0,0%,0%,0)"],
		[0, 0, 0, -0.5, "HSLA(0,0%,0%,0)"],
		[0, 0, 0, 0, "HSLA(0,0%,0%,0)"],
		[0, 0, 0, 0.5, "HSLA(0,0%,0%,0.5)"],
		[0, 0, 0, int.MaxValue, "HSLA(0,0%,0%,1)"],
		[int.MaxValue, int.MaxValue, int.MaxValue, int.MinValue, "HSLA(0,0%,100%,0)"],
		[int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, "HSLA(0,0%,100%,1)"],
		[0, 0, 0, 1, "HSLA(0,0%,0%,1)"],
		[0, 0, 1, 0, "HSLA(240,100%,50%,0)"],
		[0, 0, 1, 1, "HSLA(240,100%,50%,1)"],
		[0, 1, 0, 0, "HSLA(120,100%,50%,0)"],
		[0, 1, 0, 1, "HSLA(120,100%,50%,1)"],
		[0, 1, 1, 0, "HSLA(180,100%,50%,0)"],
		[0, 1, 1, 1, "HSLA(180,100%,50%,1)"],
		[1, 0, 0, 0, "HSLA(360,100%,50%,0)"],
		[1, 0, 0, 1, "HSLA(360,100%,50%,1)"],
		[1, 0, 1, 0, "HSLA(300,100%,50%,0)"],
		[1, 0, 1, 1, "HSLA(300,100%,50%,1)"],
		[1, 1, 0, 0, "HSLA(60,100%,50%,0)"],
		[1, 1, 0, 1, "HSLA(60,100%,50%,1)"],
		[1, 1, 1, 0, "HSLA(0,0%,100%,0)"],
		[1, 1, 1, 1, "HSLA(0,0%,100%,1)"],
		[0.5, 0, 0, 1, "HSLA(360,100%,25%,1)"],
		[0.5, 0, 0, 0, "HSLA(360,100%,25%,0)"],
		[0, 0.5, 0, 1, "HSLA(120,100%,25%,1)"],
		[0, 0.5, 0, 0, "HSLA(120,100%,25%,0)"],
		[0, 0, 0.5, 1, "HSLA(240,100%,25%,1)"],
		[0, 0, 0.5, 0, "HSLA(240,100%,25%,0)"],
		[0.5, 0.5, 0.5, 1, "HSLA(0,0%,50%,1)"],
		[0.5, 0.5, 0.5, 0, "HSLA(0,0%,50%,0)"],
		[0.25, 0.25, 0.25, 1, "HSLA(0,0%,25%,1)"],
		[0.25, 0.25, 0.25, 0, "HSLA(0,0%,25%,0)"],
		[0.25, 0.25, 1, 1, "HSLA(240,100%,62%,1)"],
		[0.25, 0.25, 1, 0, "HSLA(240,100%,62%,0)"],
		[0.25, 1, 0.25, 1, "HSLA(120,100%,62%,1)"],
		[0.25, 1, 0.25, 0, "HSLA(120,100%,62%,0)"],
		[0.75, 1, 0.25, 1, "HSLA(80,100%,62%,1)"],
		[0.75, 1, 0.25, 0, "HSLA(80,100%,62%,0)"],
		[0.75, 0, 1, 1, "HSLA(285,100%,50%,1)"],
		[0.75, 0, 1, 0, "HSLA(285,100%,50%,0)"],
	];

	[Theory]
	[MemberData(nameof(ValidInputData))]
	public void ColorToRgbStringConverterValidInputTest(float red, float green, float blue, float alpha, string expectedResult)
	{
		var converter = new ColorToHslaStringConverter();
		var color = new Color(red, green, blue, alpha);

		var resultConvert = ((ICommunityToolkitValueConverter)converter).Convert(color, typeof(string), null, new System.Globalization.CultureInfo("en-US"));
		var resultConvertFrom = converter.ConvertFrom(color, new System.Globalization.CultureInfo("en-US"));

		Assert.Equal(expectedResult, resultConvert);
		Assert.Equal(expectedResult, resultConvertFrom);
	}

	[Fact]
	public void ColorToRgbStringConverterCultureTest()
	{
		var expectedResult = "HSLA(0,0%,0%,0,5)";
		var converter = new ColorToHslaStringConverter();
		var color = new Color(0, 0, 0, 0.5f);

		var resultConvert = ((ICommunityToolkitValueConverter)converter).Convert(color, typeof(string), null, new System.Globalization.CultureInfo("uk-UA"));
		var resultConvertFrom = converter.ConvertFrom(color, new System.Globalization.CultureInfo("uk-UA"));

		Assert.Equal(expectedResult, resultConvert);
		Assert.Equal(expectedResult, resultConvertFrom);
	}

	[Fact]
	public void ColorToRgbStringConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => new ColorToHslaStringConverter().ConvertFrom(null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToHslaStringConverter()).Convert(null, typeof(string), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToHslaStringConverter()).Convert(new Color(), null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}