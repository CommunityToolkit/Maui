using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ColorToRgbaStringConverter_Tests : BaseTest
{
	public readonly static IReadOnlyList<object[]> ValidInputData = new[]
	{
		new object[] { int.MinValue, int.MinValue, int.MinValue, int.MinValue, "RGBA(0,0,0,0)" },
		new object[] { int.MinValue, int.MinValue, int.MinValue, int.MaxValue, "RGBA(0,0,0,1)" },
		new object[] { 0, 0, 0, int.MinValue, "RGBA(0,0,0,0)" },
		new object[] { 0, 0, 0, -0.5, "RGBA(0,0,0,0)" },
		new object[] { 0, 0, 0, 0, "RGBA(0,0,0,0)" },
		new object[] { 0, 0, 0, 0.5, "RGBA(0,0,0,0.5)" },
		new object[] { 0, 0, 0, 1, "RGBA(0,0,0,1)" },
		new object[] { 0, 0, 0, int.MaxValue, "RGBA(0,0,0,1)" },
		new object[] { int.MaxValue, int.MaxValue, int.MaxValue, int.MinValue, "RGBA(255,255,255,0)" },
		new object[] { int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, "RGBA(255,255,255,1)" },
		new object[] { 0, 0, 1, 0, "RGBA(0,0,255,0)" },
		new object[] { 0, 0, 1, 1, "RGBA(0,0,255,1)" },
		new object[] { 0, 1, 0, 0, "RGBA(0,255,0,0)" },
		new object[] { 0, 1, 0, 1, "RGBA(0,255,0,1)" },
		new object[] { 0, 1, 1, 0, "RGBA(0,255,255,0)" },
		new object[] { 0, 1, 1, 1, "RGBA(0,255,255,1)" },
		new object[] { 1, 0, 0, 0, "RGBA(255,0,0,0)" },
		new object[] { 1, 0, 0, 1, "RGBA(255,0,0,1)" },
		new object[] { 1, 0, 1, 0, "RGBA(255,0,255,0)" },
		new object[] { 1, 0, 1, 1, "RGBA(255,0,255,1)" },
		new object[] { 1, 1, 0, 0, "RGBA(255,255,0,0)" },
		new object[] { 1, 1, 0, 1, "RGBA(255,255,0,1)" },
		new object[] { 1, 1, 1, 0, "RGBA(255,255,255,0)" },
		new object[] { 1, 1, 1, 1, "RGBA(255,255,255,1)" },
		new object[] { 0.5, 0, 0, 1, "RGBA(128,0,0,1)" },
		new object[] { 0.5, 0, 0, 0, "RGBA(128,0,0,0)" },
		new object[] { 0, 0.5, 0, 1, "RGBA(0,128,0,1)" },
		new object[] { 0, 0.5, 0, 0, "RGBA(0,128,0,0)" },
		new object[] { 0, 0, 0.5, 1, "RGBA(0,0,128,1)" },
		new object[] { 0, 0, 0.5, 0, "RGBA(0,0,128,0)" },
		new object[] { 0.5, 0.5, 0.5, 1, "RGBA(128,128,128,1)" },
		new object[] { 0.5, 0.5, 0.5, 0, "RGBA(128,128,128,0)" },
		new object[] { 0.25, 0.25, 0.25, 1, "RGBA(64,64,64,1)" },
		new object[] { 0.25, 0.25, 0.25, 0, "RGBA(64,64,64,0)" },
		new object[] { 0.25, 0.25, 1, 1, "RGBA(64,64,255,1)" },
		new object[] { 0.25, 0.25, 1, 0, "RGBA(64,64,255,0)" },
		new object[] { 0.25, 1, 0.25, 1, "RGBA(64,255,64,1)" },
		new object[] { 0.25, 1, 0.25, 0, "RGBA(64,255,64,0)" },
		new object[] { 0.75, 1, 0.25, 1, "RGBA(191,255,64,1)" },
		new object[] { 0.75, 1, 0.25, 0, "RGBA(191,255,64,0)" },
		new object[] { 0.75, 0, 1, 1, "RGBA(191,0,255,1)" },
		new object[] { 0.75, 0, 1, 0, "RGBA(191,0,255,0)" },
	};

	[Theory]
	[MemberData(nameof(ValidInputData))]
	public void ColorToRgbStringConverterValidInputTest(float red, float green, float blue, float alpha, string expectedResult)
	{
		var converter = new ColorToRgbaStringConverter();
		var color = new Color(red, green, blue, alpha);

		var resultConvert = ((ICommunityToolkitValueConverter)converter).Convert(color, typeof(string), null, null);
		var resultConvertFrom = converter.ConvertFrom(color, typeof(string), null, null);

		Assert.Equal(expectedResult, resultConvert);
		Assert.Equal(expectedResult, resultConvertFrom);
	}

	[Fact]
	public void ColorToRgbStringConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => new ColorToRgbaStringConverter().ConvertFrom(null, typeof(string), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToRgbaStringConverter()).Convert(null, typeof(string), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToRgbaStringConverter()).Convert(new Color(), null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}