using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ColorToHexRgbStringConverter_Tests_Tests : BaseTest
{
	public readonly static IReadOnlyList<object[]> ValidInputData = new[]
	{
		new object[] { int.MinValue, int.MinValue, int.MinValue, int.MinValue, "#000000" },
		new object[] { int.MinValue, int.MinValue, int.MinValue, int.MaxValue, "#000000" },
		new object[] { 0, 0, 0, int.MinValue, "#000000" },
		new object[] { 0, 0, 0, -0.5, "#000000" },
		new object[] { 0, 0, 0, 0, "#000000" },
		new object[] { 0, 0, 0, 0.5, "#000000" },
		new object[] { 0, 0, 0, 1, "#000000" },
		new object[] { 0, 0, 0, int.MaxValue, "#000000" },
		new object[] { int.MaxValue, int.MaxValue, int.MaxValue, int.MinValue, "#FFFFFF" },
		new object[] { int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, "#FFFFFF" },
		new object[] { 0, 0, 1, 0, "#0000FF" },
		new object[] { 0, 0, 1, 1, "#0000FF" },
		new object[] { 0, 1, 0, 0, "#00FF00" },
		new object[] { 0, 1, 0, 1, "#00FF00" },
		new object[] { 0, 1, 1, 0, "#00FFFF" },
		new object[] { 0, 1, 1, 1, "#00FFFF" },
		new object[] { 1, 0, 0, 0, "#FF0000" },
		new object[] { 1, 0, 0, 1, "#FF0000" },
		new object[] { 1, 0, 1, 0, "#FF00FF" },
		new object[] { 1, 0, 1, 1, "#FF00FF" },
		new object[] { 1, 1, 0, 0, "#FFFF00" },
		new object[] { 1, 1, 0, 1, "#FFFF00" },
		new object[] { 1, 1, 1, 0, "#FFFFFF" },
		new object[] { 1, 1, 1, 1, "#FFFFFF" },
		new object[] { 0.5, 0, 0, 1, "#800000" },
		new object[] { 0.5, 0, 0, 0, "#800000" },
		new object[] { 0, 0.5, 0, 1, "#008000" },
		new object[] { 0, 0.5, 0, 0, "#008000" },
		new object[] { 0, 0, 0.5, 1, "#000080" },
		new object[] { 0, 0, 0.5, 0, "#000080" },
		new object[] { 0.5, 0.5, 0.5, 1, "#808080" },
		new object[] { 0.5, 0.5, 0.5, 0, "#808080" },
		new object[] { 0.25, 0.25, 0.25, 1, "#404040" },
		new object[] { 0.25, 0.25, 0.25, 0, "#404040" },
		new object[] { 0.25, 0.25, 1, 1, "#4040FF" },
		new object[] { 0.25, 0.25, 1, 0, "#4040FF" },
		new object[] { 0.25, 1, 0.25, 1, "#40FF40" },
		new object[] { 0.25, 1, 0.25, 0, "#40FF40" },
		new object[] { 0.75, 1, 0.25, 1, "#BFFF40" },
		new object[] { 0.75, 1, 0.25, 0, "#BFFF40" },
		new object[] { 0.75, 0, 1, 1, "#BF00FF" },
		new object[] { 0.75, 0, 1, 0, "#BF00FF" },
	};

	[Theory]
	[MemberData(nameof(ValidInputData))]
	public void ColorToHexRgbStringConverterValidInputTest(float red, float green, float blue, float alpha, string expectedResult)
	{
		var converter = new ColorToHexRgbStringConverter();
		var color = new Color(red, green, blue, alpha);

		var resultConvert = ((ICommunityToolkitValueConverter)converter).Convert(color, typeof(string), null, null);
		var resultConvertFrom = converter.ConvertFrom(color, typeof(string), null, null);

		Assert.Equal(expectedResult, resultConvert);
		Assert.Equal(expectedResult, resultConvertFrom);
	}

	[Fact]
	public void ColorToHexRgbStringConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => new ColorToHexRgbStringConverter().ConvertFrom(null, typeof(string), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToHexRgbStringConverter()).Convert(null, typeof(string), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToHexRgbStringConverter()).Convert(new Color(), null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}