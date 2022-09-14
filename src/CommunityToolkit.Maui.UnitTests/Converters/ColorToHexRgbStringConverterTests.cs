﻿using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ColorToHexRgbStringConverterTestsTests : BaseConverterTest<ColorToHexRgbStringConverter>
{
	public static readonly IReadOnlyList<object[]> ValidInputData = new[]
	{
		new object[] { int.MinValue, int.MinValue, int.MinValue, int.MinValue, "#00000000" },
		new object[] { int.MinValue, int.MinValue, int.MinValue, int.MaxValue, "#000000" },
		new object[] { 0, 0, 0, int.MinValue, "#00000000" },
		new object[] { 0, 0, 0, -0.5, "#00000000" },
		new object[] { 0, 0, 0, 0, "#00000000" },
		new object[] { 0, 0, 0, 0.5, "#0000007F" },
		new object[] { 0, 0, 0, 1, "#000000" },
		new object[] { 0, 0, 0, int.MaxValue, "#000000" },
		new object[] { int.MaxValue, int.MaxValue, int.MaxValue, int.MinValue, "#FFFFFF00" },
		new object[] { int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, "#FFFFFF" },
		new object[] { 0, 0, 1, 0, "#0000FF00" },
		new object[] { 0, 0, 1, 1, "#0000FF" },
		new object[] { 0, 1, 0, 0, "#00FF0000" },
		new object[] { 0, 1, 0, 1, "#00FF00" },
		new object[] { 0, 1, 1, 0, "#00FFFF00" },
		new object[] { 0, 1, 1, 1, "#00FFFF" },
		new object[] { 1, 0, 0, 0, "#FF000000" },
		new object[] { 1, 0, 0, 1, "#FF0000" },
		new object[] { 1, 0, 1, 0, "#FF00FF00" },
		new object[] { 1, 0, 1, 1, "#FF00FF" },
		new object[] { 1, 1, 0, 0, "#FFFF0000" },
		new object[] { 1, 1, 0, 1, "#FFFF00" },
		new object[] { 1, 1, 1, 0, "#FFFFFF00" },
		new object[] { 1, 1, 1, 1, "#FFFFFF" },
		new object[] { 0.5, 0, 0, 1, "#7F0000" },
		new object[] { 0.5, 0, 0, 0, "#7F000000" },
		new object[] { 0, 0.5, 0, 1, "#007F00" },
		new object[] { 0, 0.5, 0, 0, "#007F0000" },
		new object[] { 0, 0, 0.5, 1, "#00007F" },
		new object[] { 0, 0, 0.5, 0, "#00007F00" },
		new object[] { 0.5, 0.5, 0.5, 1, "#7F7F7F" },
		new object[] { 0.5, 0.5, 0.5, 0, "#7F7F7F00" },
		new object[] { 0.25, 0.25, 0.25, 1, "#3F3F3F" },
		new object[] { 0.25, 0.25, 0.25, 0, "#3F3F3F00" },
		new object[] { 0.25, 0.25, 1, 1, "#3F3FFF" },
		new object[] { 0.25, 0.25, 1, 0, "#3F3FFF00" },
		new object[] { 0.25, 1, 0.25, 1, "#3FFF3F" },
		new object[] { 0.25, 1, 0.25, 0, "#3FFF3F00" },
		new object[] { 0.75, 1, 0.25, 1, "#BFFF3F" },
		new object[] { 0.75, 1, 0.25, 0, "#BFFF3F00" },
		new object[] { 0.75, 0, 1, 1, "#BF00FF" },
		new object[] { 0.75, 0, 1, 0, "#BF00FF00" },
	};

	[Theory]
	[MemberData(nameof(ValidInputData))]
	public void ColorToHexRgbStringConverterValidInputTest(float red, float green, float blue, float alpha, string expectedResult)
	{
		var converter = new ColorToHexRgbStringConverter();
		var color = new Color(red, green, blue, alpha);

		var resultConvert = ((ICommunityToolkitValueConverter)converter).Convert(color, typeof(string), null, null);
		var resultConvertFrom = converter.ConvertFrom(color);

		Assert.Equal(expectedResult, resultConvert);
		Assert.Equal(expectedResult, resultConvertFrom);
	}

	[Theory]
	[MemberData(nameof(ValidInputData))]
	public void ColorToHexRgbStringConverterConvertBackValidInputTest(float red, float green, float blue, float alpha, string expectedResult)
	{
		var converter = new ColorToHexRgbStringConverter();
		var color = new Color(red, green, blue, alpha);

		var resultConvert = ((ICommunityToolkitValueConverter)converter).ConvertBack(expectedResult, typeof(Color), null, null);
		var resultConvertTo = converter.ConvertBackTo(expectedResult);

		Assert.Equal(color, resultConvert);
		Assert.Equal(color, resultConvertTo);
	}

	[Fact]
	public void ColorToHexRgbStringConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => new ColorToHexRgbStringConverter().ConvertFrom(null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToHexRgbStringConverter()).Convert(null, typeof(string), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToHexRgbStringConverter()).Convert(new Color(), null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}