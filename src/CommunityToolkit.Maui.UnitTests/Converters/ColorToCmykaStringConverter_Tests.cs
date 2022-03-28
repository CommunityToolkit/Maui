using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ColorToCmykaStringConverter_Tests : BaseTest
{
	public readonly static IReadOnlyList<object[]> ValidInputData = new[]
	{
		new object[] { int.MinValue, int.MinValue, int.MinValue, int.MinValue, "CMYKA(0%,0%,0%,100%,0)" },
		new object[] { int.MinValue, int.MinValue, int.MinValue, int.MaxValue, "CMYKA(0%,0%,0%,100%,1)" },
		new object[] { 0, 0, 0, int.MinValue, "CMYKA(0%,0%,0%,100%,0)" },
		new object[] { 0, 0, 0, -0.5, "CMYKA(0%,0%,0%,100%,0)" },
		new object[] { 0, 0, 0, 0, "CMYKA(0%,0%,0%,100%,0)" },
		new object[] { 0, 0, 0, 0.5, "CMYKA(0%,0%,0%,100%,0.5)" },
		new object[] { 0, 0, 0, 1, "CMYKA(0%,0%,0%,100%,1)" },
		new object[] { 0, 0, 0, int.MaxValue, "CMYKA(0%,0%,0%,100%,1)" },
		new object[] { int.MaxValue, int.MaxValue, int.MaxValue, int.MinValue, "CMYKA(0%,0%,0%,0%,0)" },
		new object[] { int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, "CMYKA(0%,0%,0%,0%,1)" },
		new object[] { 0, 0, 1, 0, "CMYKA(100%,100%,0%,0%,0)" },
		new object[] { 0, 0, 1, 1, "CMYKA(100%,100%,0%,0%,1)" },
		new object[] { 0, 1, 0, 0, "CMYKA(100%,0%,100%,0%,0)" },
		new object[] { 0, 1, 0, 1, "CMYKA(100%,0%,100%,0%,1)" },
		new object[] { 0, 1, 1, 0, "CMYKA(100%,0%,0%,0%,0)" },
		new object[] { 0, 1, 1, 1, "CMYKA(100%,0%,0%,0%,1)" },
		new object[] { 1, 0, 0, 0, "CMYKA(0%,100%,100%,0%,0)" },
		new object[] { 1, 0, 0, 1, "CMYKA(0%,100%,100%,0%,1)" },
		new object[] { 1, 0, 1, 0, "CMYKA(0%,100%,0%,0%,0)" },
		new object[] { 1, 0, 1, 1, "CMYKA(0%,100%,0%,0%,1)" },
		new object[] { 1, 1, 0, 0, "CMYKA(0%,0%,100%,0%,0)" },
		new object[] { 1, 1, 0, 1, "CMYKA(0%,0%,100%,0%,1)" },
		new object[] { 1, 1, 1, 0, "CMYKA(0%,0%,0%,0%,0)" },
		new object[] { 1, 1, 1, 1, "CMYKA(0%,0%,0%,0%,1)" },
		new object[] { 0.5, 0, 0, 1, "CMYKA(0%,100%,100%,50%,1)" },
		new object[] { 0.5, 0, 0, 0, "CMYKA(0%,100%,100%,50%,0)" },
		new object[] { 0, 0.5, 0, 1, "CMYKA(100%,0%,100%,50%,1)" },
		new object[] { 0, 0.5, 0, 0, "CMYKA(100%,0%,100%,50%,0)" },
		new object[] { 0, 0, 0.5, 1, "CMYKA(100%,100%,0%,50%,1)" },
		new object[] { 0, 0, 0.5, 0, "CMYKA(100%,100%,0%,50%,0)" },
		new object[] { 0.5, 0.5, 0.5, 1, "CMYKA(0%,0%,0%,50%,1)" },
		new object[] { 0.5, 0.5, 0.5, 0, "CMYKA(0%,0%,0%,50%,0)" },
		new object[] { 0.25, 0.25, 0.25, 1, "CMYKA(0%,0%,0%,75%,1)" },
		new object[] { 0.25, 0.25, 0.25, 0, "CMYKA(0%,0%,0%,75%,0)" },
		new object[] { 0.25, 0.25, 1, 1, "CMYKA(75%,75%,0%,0%,1)" },
		new object[] { 0.25, 0.25, 1, 0, "CMYKA(75%,75%,0%,0%,0)" },
		new object[] { 0.25, 1, 0.25, 1, "CMYKA(75%,0%,75%,0%,1)" },
		new object[] { 0.25, 1, 0.25, 0, "CMYKA(75%,0%,75%,0%,0)" },
		new object[] { 0.75, 1, 0.25, 1, "CMYKA(25%,0%,75%,0%,1)" },
		new object[] { 0.75, 1, 0.25, 0, "CMYKA(25%,0%,75%,0%,0)" },
		new object[] { 0.75, 0, 1, 1, "CMYKA(25%,100%,0%,0%,1)" },
		new object[] { 0.75, 0, 1, 0, "CMYKA(25%,100%,0%,0%,0)" },
	};

	[Theory]
	[MemberData(nameof(ValidInputData))]
	public void ColorToCmykaStringConverterValidInputTest(float red, float green, float blue, float alpha, string expectedResult)
	{
		var converter = new ColorToCmykaStringConverter();
		var color = new Color(red, green, blue, alpha);

		var resultConvert = ((ICommunityToolkitValueConverter)converter).Convert(color, typeof(string), null, null);
		var resultConvertFrom = converter.ConvertFrom(color, typeof(string), null, null);

		Assert.Equal(expectedResult, resultConvert);
		Assert.Equal(expectedResult, resultConvertFrom);
	}

	[Fact]
	public void ColorToCmykaStringConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => new ColorToCmykaStringConverter().ConvertFrom(null, typeof(string), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToCmykaStringConverter()).Convert(null, typeof(string), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToCmykaStringConverter()).Convert(new Color(), null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}