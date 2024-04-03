using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ColorToDegreeHueConverterTests : BaseOneWayConverterTest<ColorToDegreeHueConverter>
{
	public static readonly IReadOnlyList<object[]> ValidInputData =
	[
		[0, 0, 0, 0, 0],
		[0, 0, 0, 1, 0],
		[0, 0, 1, 0, 240],
		[0, 0, 1, 1, 240],
		[0, 1, 0, 0, 120],
		[0, 1, 0, 1, 120],
		[0, 1, 1, 0, 180],
		[0, 1, 1, 1, 180],
		[1, 0, 0, 0, 360],
		[1, 0, 0, 1, 360],
		[1, 0, 1, 0, 300],
		[1, 0, 1, 1, 300],
		[1, 1, 0, 0, 60],
		[1, 1, 0, 1, 60],
		[1, 1, 1, 0, 0],
		[1, 1, 1, 1, 0],
		[0.5, 0, 0, 1, 360],
		[0.5, 0, 0, 0, 360],
		[0, 0.5, 0, 1, 120],
		[0, 0.5, 0, 0, 120],
		[0.5, 0.5, 0.5, 1, 0],
		[0.5, 0.5, 0.5, 0, 0],
		[0.25, 0.25, 0.25, 1, 0],
		[0.25, 0.25, 0.25, 0, 0],
		[0.25, 0.25, 1, 1, 240],
		[0.25, 0.25, 1, 0, 240],
		[0.25, 1, 0.25, 1, 120],
		[0.25, 1, 0.25, 0, 120],
		[0.75, 1, 0.25, 1, 80],
		[0.75, 1, 0.25, 0, 80],
		[0.75, 0, 1, 1, 285],
		[0.75, 0, 1, 0, 285],
	];

	[Theory]
	[MemberData(nameof(ValidInputData))]
	public void ColorToDegreeHueConverterValidInputTest(float red, float green, float blue, float alpha, double expectedResult)
	{
		var converter = new ColorToDegreeHueConverter();
		var color = new Color(red, green, blue, alpha);

		var resultConvertFrom = converter.ConvertFrom(color);
		var resultConvert = ((ICommunityToolkitValueConverter)converter).Convert(color, typeof(double), null, null);

		Assert.Equal(expectedResult, resultConvertFrom);
		Assert.Equal(expectedResult, resultConvert);
	}

	[Fact]
	public void ColorToDegreeHueConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => new ColorToDegreeHueConverter().ConvertFrom(null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToDegreeHueConverter()).Convert(null, typeof(double), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToDegreeHueConverter()).Convert(new Color(), null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}