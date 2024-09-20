using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ColorToRgbStringConverterTests : BaseConverterTest<ColorToRgbStringConverter>
{
	public static readonly IReadOnlyList<object[]> ValidInputData =
	[
		[int.MinValue, int.MinValue, int.MinValue, int.MinValue, "RGB(0,0,0)"],
		[0, 0, 0, int.MinValue, "RGB(0,0,0)"],
		[0, 0, 0, -0.5, "RGB(0,0,0)"],
		[0, 0, 0, 0, "RGB(0,0,0)"],
		[0, 0, 0, 0.5, "RGB(0,0,0)"],
		[0, 0, 0, int.MaxValue, "RGB(0,0,0)"],
		[int.MaxValue, int.MaxValue, int.MaxValue, int.MinValue, "RGB(255,255,255)"],
		[int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, "RGB(255,255,255)"],
		[0, 0, 0, 1, "RGB(0,0,0)"],
		[0, 0, 1, 0, "RGB(0,0,255)"],
		[0, 0, 1, 1, "RGB(0,0,255)"],
		[0, 1, 0, 0, "RGB(0,255,0)"],
		[0, 1, 0, 1, "RGB(0,255,0)"],
		[0, 1, 1, 0, "RGB(0,255,255)"],
		[0, 1, 1, 1, "RGB(0,255,255)"],
		[1, 0, 0, 0, "RGB(255,0,0)"],
		[1, 0, 0, 1, "RGB(255,0,0)"],
		[1, 0, 1, 0, "RGB(255,0,255)"],
		[1, 0, 1, 1, "RGB(255,0,255)"],
		[1, 1, 0, 0, "RGB(255,255,0)"],
		[1, 1, 0, 1, "RGB(255,255,0)"],
		[1, 1, 1, 0, "RGB(255,255,255)"],
		[1, 1, 1, 1, "RGB(255,255,255)"],
		[0.5, 0, 0, 1, "RGB(128,0,0)"],
		[0.5, 0, 0, 0, "RGB(128,0,0)"],
		[0, 0.5, 0, 1, "RGB(0,128,0)"],
		[0, 0.5, 0, 0, "RGB(0,128,0)"],
		[0, 0, 0.5, 1, "RGB(0,0,128)"],
		[0, 0, 0.5, 0, "RGB(0,0,128)"],
		[0.5, 0.5, 0.5, 1, "RGB(128,128,128)"],
		[0.5, 0.5, 0.5, 0, "RGB(128,128,128)"],
		[0.25, 0.25, 0.25, 1, "RGB(64,64,64)"],
		[0.25, 0.25, 0.25, 0, "RGB(64,64,64)"],
		[0.25, 0.25, 1, 1, "RGB(64,64,255)"],
		[0.25, 0.25, 1, 0, "RGB(64,64,255)"],
		[0.25, 1, 0.25, 1, "RGB(64,255,64)"],
		[0.25, 1, 0.25, 0, "RGB(64,255,64)"],
		[0.75, 1, 0.25, 1, "RGB(191,255,64)"],
		[0.75, 1, 0.25, 0, "RGB(191,255,64)"],
		[0.75, 0, 1, 1, "RGB(191,0,255)"],
		[0.75, 0, 1, 0, "RGB(191,0,255)"],
	];

	[Theory]
	[MemberData(nameof(ValidInputData))]
	public void ColorToRgbStringConverterValidInputTest(float red, float green, float blue, float alpha, string expectedResult)
	{
		var converter = new ColorToRgbStringConverter();
		var color = new Color(red, green, blue, alpha);

		var resultConvert = ((ICommunityToolkitValueConverter)converter).Convert(color, typeof(string), null, null);
		var resultConvertFrom = converter.ConvertFrom(color);

		Assert.Equal(expectedResult, resultConvert);
		Assert.Equal(expectedResult, resultConvertFrom);
	}

	[Theory]
	[MemberData(nameof(ValidInputData))]
	public void ColorToRgbStringConverterConvertBackValidInputTest(float red, float green, float blue, float alpha, string colorString)
	{
		var converter = new ColorToRgbStringConverter();
		var expectedResult = new Color(red, green, blue, alpha);

		var resultConvertBack = ((ICommunityToolkitValueConverter)converter).ConvertBack(colorString, typeof(Color), null, null);
		var resultConvertBackTo = converter.ConvertBackTo(colorString, CultureInfo.InvariantCulture);

		AssertColorComparison(expectedResult, resultConvertBack as Color);
		AssertColorComparison(expectedResult, resultConvertBackTo);
	}

	static void AssertColorComparison(Color expectedResult, Color? result)
	{
		const int precision = 2;

		Assert.NotNull(result);
		Assert.Equal(expectedResult.Red, result.Red, precision);
		Assert.Equal(expectedResult.Green, result.Green, precision);
		Assert.Equal(expectedResult.Blue, result.Blue, precision);
	}

	[Fact]
	public void ColorToRgbStringConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => new ColorToPercentYellowConverter().ConvertFrom(null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToPercentYellowConverter()).Convert(null, typeof(string), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToPercentYellowConverter()).Convert(new Color(), null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}