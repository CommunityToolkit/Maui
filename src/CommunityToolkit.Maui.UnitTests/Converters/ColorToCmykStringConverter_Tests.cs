using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ColorToCmykStringConverter_Tests : BaseTest
{
	public readonly static IReadOnlyList<object[]> ValidInputData = new[]
	{
		new object[] { int.MinValue, int.MinValue, int.MinValue, int.MinValue, "CMYK(0%,0%,0%,100%)" },
		new object[] { int.MinValue, int.MinValue, int.MinValue, int.MaxValue, "CMYK(0%,0%,0%,100%)" },
		new object[] { 0, 0, 0, int.MinValue, "CMYK(0%,0%,0%,100%)" },
		new object[] { 0, 0, 0, -0.5, "CMYK(0%,0%,0%,100%)" },
		new object[] { 0, 0, 0, 0, "CMYK(0%,0%,0%,100%)" },
		new object[] { 0, 0, 0, 0.5, "CMYK(0%,0%,0%,100%)" },
		new object[] { 0, 0, 0, 1, "CMYK(0%,0%,0%,100%)" },
		new object[] { 0, 0, 0, int.MaxValue, "CMYK(0%,0%,0%,100%)" },
		new object[] { int.MaxValue, int.MaxValue, int.MaxValue, int.MinValue, "CMYK(0%,0%,0%,0%)" },
		new object[] { int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, "CMYK(0%,0%,0%,0%)" },
		new object[] { 0, 0, 1, 0, "CMYK(100%,100%,0%,0%)" },
		new object[] { 0, 0, 1, 1, "CMYK(100%,100%,0%,0%)" },
		new object[] { 0, 1, 0, 0, "CMYK(100%,0%,100%,0%)" },
		new object[] { 0, 1, 0, 1, "CMYK(100%,0%,100%,0%)" },
		new object[] { 0, 1, 1, 0, "CMYK(100%,0%,0%,0%)" },
		new object[] { 0, 1, 1, 1, "CMYK(100%,0%,0%,0%)" },
		new object[] { 1, 0, 0, 0, "CMYK(0%,100%,100%,0%)" },
		new object[] { 1, 0, 0, 1, "CMYK(0%,100%,100%,0%)" },
		new object[] { 1, 0, 1, 0, "CMYK(0%,100%,0%,0%)" },
		new object[] { 1, 0, 1, 1, "CMYK(0%,100%,0%,0%)" },
		new object[] { 1, 1, 0, 0, "CMYK(0%,0%,100%,0%)" },
		new object[] { 1, 1, 0, 1, "CMYK(0%,0%,100%,0%)" },
		new object[] { 1, 1, 1, 0, "CMYK(0%,0%,0%,0%)" },
		new object[] { 1, 1, 1, 1, "CMYK(0%,0%,0%,0%)" },
		new object[] { 0.5, 0, 0, 1, "CMYK(0%,100%,100%,50%)" },
		new object[] { 0.5, 0, 0, 0, "CMYK(0%,100%,100%,50%)" },
		new object[] { 0, 0.5, 0, 1, "CMYK(100%,0%,100%,50%)" },
		new object[] { 0, 0.5, 0, 0, "CMYK(100%,0%,100%,50%)" },
		new object[] { 0, 0, 0.5, 1, "CMYK(100%,100%,0%,50%)" },
		new object[] { 0, 0, 0.5, 0, "CMYK(100%,100%,0%,50%)" },
		new object[] { 0.5, 0.5, 0.5, 1, "CMYK(0%,0%,0%,50%)" },
		new object[] { 0.5, 0.5, 0.5, 0, "CMYK(0%,0%,0%,50%)" },
		new object[] { 0.25, 0.25, 0.25, 1, "CMYK(0%,0%,0%,75%)" },
		new object[] { 0.25, 0.25, 0.25, 0, "CMYK(0%,0%,0%,75%)" },
		new object[] { 0.25, 0.25, 1, 1, "CMYK(75%,75%,0%,0%)" },
		new object[] { 0.25, 0.25, 1, 0, "CMYK(75%,75%,0%,0%)" },
		new object[] { 0.25, 1, 0.25, 1, "CMYK(75%,0%,75%,0%)" },
		new object[] { 0.25, 1, 0.25, 0, "CMYK(75%,0%,75%,0%)" },
		new object[] { 0.75, 1, 0.25, 1, "CMYK(25%,0%,75%,0%)" },
		new object[] { 0.75, 1, 0.25, 0, "CMYK(25%,0%,75%,0%)" },
		new object[] { 0.75, 0, 1, 1, "CMYK(25%,100%,0%,0%)" },
		new object[] { 0.75, 0, 1, 0, "CMYK(25%,100%,0%,0%)" },
	};

	[Theory]
	[MemberData(nameof(ValidInputData))]
	public void ColorToCmykStringConverterValidInputTest(float red, float green, float blue, float alpha, string expectedResult)
	{
		var converter = new ColorToCmykStringConverter();
		var color = new Color(red, green, blue, alpha);

		var resultConvert = ((ICommunityToolkitValueConverter)converter).Convert(color, typeof(string), null, null);
		var resultConvertFrom = converter.ConvertFrom(color, typeof(string), null, null);

		Assert.Equal(expectedResult, resultConvert);
		Assert.Equal(expectedResult, resultConvertFrom);
	}

	[Fact]
	public void ColorToCmykStringConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => new ColorToCmykStringConverter().ConvertFrom(null, typeof(string), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToCmykStringConverter()).Convert(null, typeof(string), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToCmykStringConverter()).Convert(new Color(), null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}