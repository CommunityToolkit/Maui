using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ColorToRgbStringConverter_Tests : BaseTest
{
	public readonly static IReadOnlyList<object[]> ValidInputData = new[]
	{
		new object[] { 0, 0, 0, 0, "RGB(0,0,0)" },
		new object[] { 0, 0, 0, 1, "RGB(0,0,0)" },
		new object[] { 0, 0, 1, 0, "RGB(0,0,255)" },
		new object[] { 0, 0, 1, 1, "RGB(0,0,255)" },
		new object[] { 0, 1, 0, 0, "RGB(0,255,0)" },
		new object[] { 0, 1, 0, 1, "RGB(0,255,0)" },
		new object[] { 0, 1, 1, 0, "RGB(0,255,255)" },
		new object[] { 0, 1, 1, 1, "RGB(0,255,255)" },
		new object[] { 1, 0, 0, 0, "RGB(255,0,0)" },
		new object[] { 1, 0, 0, 1, "RGB(255,0,0)" },
		new object[] { 1, 0, 1, 0, "RGB(255,0,255)" },
		new object[] { 1, 0, 1, 1, "RGB(255,0,255)" },
		new object[] { 1, 1, 0, 0, "RGB(255,255,0)" },
		new object[] { 1, 1, 0, 1, "RGB(255,255,0)" },
		new object[] { 1, 1, 1, 0, "RGB(255,255,255)" },
		new object[] { 1, 1, 1, 1, "RGB(255,255,255)" },
		new object[] { 0.5, 0, 0, 1, "RGB(127,0,0)" },
		//new object[] { 0.5, 0, 0, 0, 1 },
		//new object[] { 0, 0.5, 0, 1, 1 },
		//new object[] { 0, 0.5, 0, 0, 1 },
		//new object[] { 0, 0, 0.5, 1, 0 },
		//new object[] { 0, 0, 0.5, 0, 0 },
		//new object[] { 0.5, 0.5, 0.5, 1, 0 },
		//new object[] { 0.5, 0.5, 0.5, 0, 0 },
		//new object[] { 0.25, 0.25, 0.25, 1, 0 },
		//new object[] { 0.25, 0.25, 0.25, 0, 0 },
		//new object[] { 0.25, 0.25, 1, 1, 0 },
		//new object[] { 0.25, 0.25, 1, 0, 0 },
		//new object[] { 0.25, 1, 0.25, 1, 0.75 },
		//new object[] { 0.25, 1, 0.25, 0, 0.75 },
		//new object[] { 0.75, 1, 0.25, 1, 0.75 },
		//new object[] { 0.75, 1, 0.25, 0, 0.75 },
		//new object[] { 0.75, 0, 1, 1, 0 },
		//new object[] { 0.75, 0, 1, 0, 0 },
	};

	[Theory]
	[MemberData(nameof(ValidInputData))]
	public void ColorToRgbStringConverterValidInputTest(float red, float green, float blue, float alpha, string expectedResult)
	{
		var converter = new ColorToRgbStringConverter();
		var color = new Color(red, green, blue, alpha);

		var resultConvertFrom = converter.ConvertFrom(color);
		var resultConvert = converter.Convert(color, typeof(string), null, null);

		Assert.Equal(expectedResult, resultConvertFrom);
		Assert.Equal(expectedResult, resultConvert);
	}

	[Fact]
	public void ColorToRgbStringConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => new ColorToPercentYellowConverter().ConvertFrom(null));
		Assert.Throws<ArgumentNullException>(() => new ColorToPercentYellowConverter().Convert(null, typeof(string), null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}