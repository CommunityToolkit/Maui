using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ColorToHexRgbaStringConverter_Tests_Tests : BaseTest
{
	public readonly static IReadOnlyList<object[]> ValidInputData = new[]
	{
		new object[] { int.MinValue, int.MinValue, int.MinValue, int.MinValue, "#00000000" },
		new object[] { int.MinValue, int.MinValue, int.MinValue, int.MaxValue, "#000000FF" },
		new object[] { 0, 0, 0, int.MinValue, "#00000000" },
		new object[] { 0, 0, 0, -0.5, "#00000000" },
		new object[] { 0, 0, 0, 0, "#00000000" },
		new object[] { 0, 0, 0, 0.5, "#00000080" },
		new object[] { 0, 0, 0, 1, "#000000FF" },
		new object[] { 0, 0, 0, int.MaxValue, "#000000FF" },
		new object[] { int.MaxValue, int.MaxValue, int.MaxValue, int.MinValue, "#FFFFFF00" },
		new object[] { int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, "#FFFFFFFF" },
		new object[] { 0, 0, 1, 0, "#0000FF00" },
		new object[] { 0, 0, 1, 1, "#0000FFFF" },
		new object[] { 0, 1, 0, 0, "#00FF0000" },
		new object[] { 0, 1, 0, 1, "#00FF00FF" },
		new object[] { 0, 1, 1, 0, "#00FFFF00" },
		new object[] { 0, 1, 1, 1, "#00FFFFFF" },
		new object[] { 1, 0, 0, 0, "#FF000000" },
		new object[] { 1, 0, 0, 1, "#FF0000FF" },
		new object[] { 1, 0, 1, 0, "#FF00FF00" },
		new object[] { 1, 0, 1, 1, "#FF00FFFF" },
		new object[] { 1, 1, 0, 0, "#FFFF0000" },
		new object[] { 1, 1, 0, 1, "#FFFF00FF" },
		new object[] { 1, 1, 1, 0, "#FFFFFF00" },
		new object[] { 1, 1, 1, 1, "#FFFFFFFF" },
		new object[] { 0.5, 0, 0, 1, "#800000FF" },
		new object[] { 0.5, 0, 0, 0, "#80000000" },
		new object[] { 0, 0.5, 0, 1, "#008000FF" },
		new object[] { 0, 0.5, 0, 0, "#00800000" },
		new object[] { 0, 0, 0.5, 1, "#000080FF" },
		new object[] { 0, 0, 0.5, 0, "#00008000" },
		new object[] { 0.5, 0.5, 0.5, 1, "#808080FF" },
		new object[] { 0.5, 0.5, 0.5, 0, "#80808000" },
		new object[] { 0.25, 0.25, 0.25, 1, "#404040FF" },
		new object[] { 0.25, 0.25, 0.25, 0, "#40404000" },
		new object[] { 0.25, 0.25, 1, 1, "#4040FFFF" },
		new object[] { 0.25, 0.25, 1, 0, "#4040FF00" },
		new object[] { 0.25, 1, 0.25, 1, "#40FF40FF" },
		new object[] { 0.25, 1, 0.25, 0, "#40FF4000" },
		new object[] { 0.75, 1, 0.25, 1, "#BFFF40FF" },
		new object[] { 0.75, 1, 0.25, 0, "#BFFF4000" },
		new object[] { 0.75, 0, 1, 1, "#BF00FFFF" },
		new object[] { 0.75, 0, 1, 0, "#BF00FF00" },
	};

	[Theory]
	[MemberData(nameof(ValidInputData))]
	public void ColorToHexRgbaStringConverterValidInputTest(float red, float green, float blue, float alpha, string expectedResult)
	{
		var converter = new ColorToHexRgbaStringConverter();
		var color = new Color(red, green, blue, alpha);

		var resultConvert = ((ICommunityToolkitValueConverter)converter).Convert(color, typeof(string), null, null);
		var resultConvertFrom = converter.ConvertFrom(color, typeof(string), null, null);

		Assert.Equal(expectedResult, resultConvert);
		Assert.Equal(expectedResult, resultConvertFrom);
	}

	[Fact]
	public void ColorToHexRgbaStringConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => new ColorToHexRgbaStringConverter().ConvertFrom(null, typeof(string), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToHexRgbaStringConverter()).Convert(null, typeof(string), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToHexRgbaStringConverter()).Convert(new Color(), null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}