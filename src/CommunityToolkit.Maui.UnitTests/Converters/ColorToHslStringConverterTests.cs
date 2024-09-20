using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ColorToHslStringConverterTests : BaseOneWayConverterTest<ColorToHslStringConverter>
{
	public static readonly IReadOnlyList<object[]> ValidInputData =
	[
		[int.MinValue, int.MinValue, int.MinValue, int.MinValue, "HSL(0,0%,0%)"],
		[0, 0, 0, int.MinValue, "HSL(0,0%,0%)"],
		[0, 0, 0, -0.5, "HSL(0,0%,0%)"],
		[0, 0, 0, 0, "HSL(0,0%,0%)"],
		[0, 0, 0, 0.5, "HSL(0,0%,0%)"],
		[0, 0, 0, int.MaxValue, "HSL(0,0%,0%)"],
		[int.MaxValue, int.MaxValue, int.MaxValue, int.MinValue, "HSL(0,0%,100%)"],
		[int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, "HSL(0,0%,100%)"],
		[0, 0, 0, 1, "HSL(0,0%,0%)"],
		[0, 0, 1, 0, "HSL(240,100%,50%)"],
		[0, 0, 1, 1, "HSL(240,100%,50%)"],
		[0, 1, 0, 0, "HSL(120,100%,50%)"],
		[0, 1, 0, 1, "HSL(120,100%,50%)"],
		[0, 1, 1, 0, "HSL(180,100%,50%)"],
		[0, 1, 1, 1, "HSL(180,100%,50%)"],
		[1, 0, 0, 0, "HSL(360,100%,50%)"],
		[1, 0, 0, 1, "HSL(360,100%,50%)"],
		[1, 0, 1, 0, "HSL(300,100%,50%)"],
		[1, 0, 1, 1, "HSL(300,100%,50%)"],
		[1, 1, 0, 0, "HSL(60,100%,50%)"],
		[1, 1, 0, 1, "HSL(60,100%,50%)"],
		[1, 1, 1, 0, "HSL(0,0%,100%)"],
		[1, 1, 1, 1, "HSL(0,0%,100%)"],
		[0.5, 0, 0, 1, "HSL(360,100%,25%)"],
		[0.5, 0, 0, 0, "HSL(360,100%,25%)"],
		[0, 0.5, 0, 1, "HSL(120,100%,25%)"],
		[0, 0.5, 0, 0, "HSL(120,100%,25%)"],
		[0, 0, 0.5, 1, "HSL(240,100%,25%)"],
		[0, 0, 0.5, 0, "HSL(240,100%,25%)"],
		[0.5, 0.5, 0.5, 1, "HSL(0,0%,50%)"],
		[0.5, 0.5, 0.5, 0, "HSL(0,0%,50%)"],
		[0.25, 0.25, 0.25, 1, "HSL(0,0%,25%)"],
		[0.25, 0.25, 0.25, 0, "HSL(0,0%,25%)"],
		[0.25, 0.25, 1, 1, "HSL(240,100%,62%)"],
		[0.25, 0.25, 1, 0, "HSL(240,100%,62%)"],
		[0.25, 1, 0.25, 1, "HSL(120,100%,62%)"],
		[0.25, 1, 0.25, 0, "HSL(120,100%,62%)"],
		[0.75, 1, 0.25, 1, "HSL(80,100%,62%)"],
		[0.75, 1, 0.25, 0, "HSL(80,100%,62%)"],
		[0.75, 0, 1, 1, "HSL(285,100%,50%)"],
		[0.75, 0, 1, 0, "HSL(285,100%,50%)"],
	];

	[Theory]
	[MemberData(nameof(ValidInputData))]
	public void ColorToHslStringConverterValidInputTest(float red, float green, float blue, float alpha, string expectedResult)
	{
		var converter = new ColorToHslStringConverter();
		var color = new Color(red, green, blue, alpha);

		var resultConvert = ((ICommunityToolkitValueConverter)converter).Convert(color, typeof(string), null, null);
		var resultConvertFrom = converter.ConvertFrom(color);

		Assert.Equal(expectedResult, resultConvert);
		Assert.Equal(expectedResult, resultConvertFrom);
	}

	[Fact]
	public void ColorToHslStringConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => new ColorToPercentYellowConverter().ConvertFrom(null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToPercentYellowConverter()).Convert(null, typeof(string), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToPercentYellowConverter()).Convert(new Color(), null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}