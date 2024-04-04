using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ColorToPercentMagentaConverterTests : BaseOneWayConverterTest<ColorToPercentMagentaConverter>
{
	public static readonly IReadOnlyList<object[]> ValidInputData =
	[
		[0, 0, 0, 0, 0],
		[0, 0, 0, 1, 0],
		[0, 0, 1, 0, 1],
		[0, 0, 1, 1, 1],
		[0, 1, 0, 0, 0],
		[0, 1, 0, 1, 0],
		[0, 1, 1, 0, 0],
		[0, 1, 1, 1, 0],
		[1, 0, 0, 0, 1],
		[1, 0, 0, 1, 1],
		[1, 0, 1, 0, 1],
		[1, 0, 1, 1, 1],
		[1, 1, 0, 0, 0],
		[1, 1, 0, 1, 0],
		[1, 1, 1, 0, 0],
		[1, 1, 1, 1, 0],
		[0.5, 0, 0, 1, 1],
		[0.5, 0, 0, 0, 1],
		[0, 0.5, 0, 1, 0],
		[0, 0.5, 0, 0, 0],
		[0.5, 0.5, 0.5, 1, 0],
		[0.5, 0.5, 0.5, 0, 0],
		[0.25, 0.25, 0.25, 1, 0],
		[0.25, 0.25, 0.25, 0, 0],
		[0.25, 0.25, 1, 1, 0.75],
		[0.25, 0.25, 1, 0, 0.75],
		[0.25, 1, 0.25, 1, 0],
		[0.25, 1, 0.25, 0, 0],
		[0.75, 1, 0.25, 1, 0],
		[0.75, 1, 0.25, 0, 0],
		[0.75, 0, 1, 1, 1],
		[0.75, 0, 1, 0, 1],
	];

	[Theory]
	[MemberData(nameof(ValidInputData))]
	public void ColorToPercentMagentaConverterValidInputTest(float red, float green, float blue, float alpha, double expectedResult)
	{
		var converter = new ColorToPercentMagentaConverter();
		var color = new Color(red, green, blue, alpha);

		var resultConvertFrom = converter.ConvertFrom(color);
		var resultConvert = ((ICommunityToolkitValueConverter)converter).Convert(color, typeof(double), null, null);

		Assert.Equal(expectedResult, resultConvertFrom);
		Assert.Equal(expectedResult, resultConvert);
	}

	[Fact]
	public void ColorToPercentMagentaConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => new ColorToPercentMagentaConverter().ConvertFrom(null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToPercentMagentaConverter()).Convert(null, typeof(double), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToPercentMagentaConverter()).Convert(new Color(), null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}