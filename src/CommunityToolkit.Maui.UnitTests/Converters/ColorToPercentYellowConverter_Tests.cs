using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ColorToPercentYellowConverter_Tests : BaseTest
{
	public readonly static IReadOnlyList<object[]> ValidInputData = new[]
	{
		new object[] { 0, 0, 0, 0, 0 },
		new object[] { 0, 0, 0, 1, 0 },
		new object[] { 0, 0, 1, 0, 0 },
		new object[] { 0, 0, 1, 1, 0 },
		new object[] { 0, 1, 0, 0, 1 },
		new object[] { 0, 1, 0, 1, 1 },
		new object[] { 0, 1, 1, 0, 0 },
		new object[] { 0, 1, 1, 1, 0 },
		new object[] { 1, 0, 0, 0, 1 },
		new object[] { 1, 0, 0, 1, 1 },
		new object[] { 1, 0, 1, 0, 0 },
		new object[] { 1, 0, 1, 1, 0 },
		new object[] { 1, 1, 0, 0, 1 },
		new object[] { 1, 1, 0, 1, 1 },
		new object[] { 1, 1, 1, 0, 0 },
		new object[] { 1, 1, 1, 1, 0 },
		new object[] { 0.5, 0, 0, 1, 1 },
		new object[] { 0.5, 0, 0, 0, 1 },
		new object[] { 0, 0.5, 0, 1, 1 },
		new object[] { 0, 0.5, 0, 0, 1 },
		new object[] { 0, 0, 0.5, 1, 0 },
		new object[] { 0, 0, 0.5, 0, 0 },
		new object[] { 0.5, 0.5, 0.5, 1, 0 },
		new object[] { 0.5, 0.5, 0.5, 0, 0 },
		new object[] { 0.25, 0.25, 0.25, 1, 0 },
		new object[] { 0.25, 0.25, 0.25, 0, 0 },
		new object[] { 0.25, 0.25, 1, 1, 0 },
		new object[] { 0.25, 0.25, 1, 0, 0 },
		new object[] { 0.25, 1, 0.25, 1, 0.75 },
		new object[] { 0.25, 1, 0.25, 0, 0.75 },
		new object[] { 0.75, 1, 0.25, 1, 0.75 },
		new object[] { 0.75, 1, 0.25, 0, 0.75 },
		new object[] { 0.75, 0, 1, 1, 0 },
		new object[] { 0.75, 0, 1, 0, 0 },
	};

	[Theory]
	[MemberData(nameof(ValidInputData))]
	public void ColorToPercentYellowConverterValidInputTest(float red, float green, float blue, float alpha, double expectedResult)
	{
		var converter = new ColorToPercentYellowConverter();
		var color = new Color(red, green, blue, alpha);

		var resultConvertFrom = converter.ConvertFrom(color, typeof(double), null, null);
		var resultConvert = ((ICommunityToolkitValueConverter)converter).Convert(color, typeof(double), null, null);

		Assert.Equal(expectedResult, resultConvertFrom);
		Assert.Equal(expectedResult, resultConvert);
	}

	[Fact]
	public void ColorToPercentYellowCyanConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => new ColorToPercentYellowConverter().ConvertFrom(null, typeof(double), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToPercentYellowConverter()).Convert(null, typeof(double), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToPercentYellowConverter()).Convert(new Color(), null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}