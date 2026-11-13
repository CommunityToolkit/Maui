using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ColorToHexArgbStringConverterTests : BaseConverterTest<ColorToHexArgbStringConverter>
{
	public static readonly TheoryData<float, float, float, float, string> ValidInputData = new()
	{
		{
			int.MinValue, int.MinValue, int.MinValue, int.MinValue, "#00000000"
		},
		{
			int.MaxValue, int.MinValue, int.MinValue, int.MinValue, "#FF000000"
		},
		{
			int.MinValue, 0, 0, 0, "#00000000"
		},
		{
			-0.5f, 0, 0, 0, "#00000000"
		},
		{
			0, 0, 0, 0, "#00000000"
		},
		{
			0.5f, 0, 0, 0, "#7F000000"
		},
		{
			1, 0, 0, 0, "#FF000000"
		},
		{
			int.MaxValue, 0, 0, 0, "#FF000000"
		},
		{
			int.MinValue, int.MaxValue, int.MaxValue, int.MaxValue, "#00FFFFFF"
		},
		{
			int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, "#FFFFFFFF"
		},
		{
			0, 1, 0, 0, "#00FF0000"
		},
		{
			1, 1, 0, 0, "#FFFF0000"
		},
		{
			0, 1, 1, 0, "#00FFFF00"
		},
		{
			1, 1, 1, 0, "#FFFFFF00"
		},
		{
			0, 0, 1, 0, "#0000FF00"
		},
		{
			1, 0, 1, 0, "#FF00FF00"
		},
		{
			0, 0, 1, 1, "#0000FFFF"
		},
		{
			1, 0, 1, 1, "#FF00FFFF"
		},
		{
			0, 1, 0, 1, "#00FF00FF"
		},
		{
			1, 1, 0, 1, "#FFFF00FF"
		},
		{
			0, 0, 0.5f, 1, "#00007FFF"
		},
		{
			0, 0, 0.5f, 0, "#00007F00"
		},
		{
			0.5f, 0.5f, 0.5f, 1, "#7F7F7FFF"
		},
		{
			0.5f, 0.5f, 0.5f, 0, "#7F7F7F00"
		},
		{
			0.25f, 0.25f, 0.25f, 1, "#3F3F3FFF"
		},
		{
			0.25f, 0.25f, 0.25f, 0, "#3F3F3F00"
		},
		{
			0.25f, 1, 0.25f, 1, "#3FFF3FFF"
		},
		{
			0.25f, 1, 0.25f, 0, "#3FFF3F00"
		},
		{
			0.75f, 1, 0.25f, 1, "#BFFF3FFF"
		},
		{
			0.75f, 1, 0.25f, 0, "#BFFF3F00"
		},
		{
			0.75f, 0, 1, 1, "#BF00FFFF"
		},
		{
			0.75f, 0, 1, 0, "#BF00FF00"
		}
	};

	[Theory]
	[MemberData(nameof(ValidInputData))]
	public void ColorToHexArgbStringConverterValidInputTest(float alpha, float red, float green, float blue, string expectedResult)
	{
		var converter = new ColorToHexArgbStringConverter();
		var color = new Color(red, green, blue, alpha);

		var resultConvert = ((ICommunityToolkitValueConverter)converter).Convert(color, typeof(string), null, null);
		var resultConvertFrom = converter.ConvertFrom(color);

		Assert.Equal(expectedResult, resultConvert);
		Assert.Equal(expectedResult, resultConvertFrom);
	}

	[Theory]
	[MemberData(nameof(ValidInputData))]
	public void ColorToHexArgbStringConverterConvertBackValidInputTest(float alpha, float red, float green, float blue, string expectedResult)
	{
		var converter = new ColorToHexArgbStringConverter();
		var color = new Color(red, green, blue, alpha);

		var resultConvert = ((ICommunityToolkitValueConverter)converter).ConvertBack(expectedResult, typeof(Color), null, null);
		var resultConvertTo = converter.ConvertBackTo(expectedResult);

		Assert.Equal(color, resultConvert);
		Assert.Equal(color, resultConvertTo);
	}

	[Fact]
	public void ColorToHexArgbStringConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => new ColorToHexArgbStringConverter().ConvertFrom(null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToHexArgbStringConverter()).Convert(null, typeof(string), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToHexArgbStringConverter()).Convert(new Color(), null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}