using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ColorToRgbaStringConverterTests : BaseConverterTest<ColorToRgbaStringConverter>
{
	public static readonly TheoryData<float, float, float, float, string> ValidInputData = new()
	{
		{
			int.MinValue, int.MinValue, int.MinValue, int.MinValue, "RGBA(0,0,0,0)"
		},
		{
			int.MinValue, int.MinValue, int.MinValue, int.MaxValue, "RGBA(0,0,0,1)"
		},
		{
			0, 0, 0, int.MinValue, "RGBA(0,0,0,0)"
		},
		{
			0, 0, 0, -0.5f, "RGBA(0,0,0,0)"
		},
		{
			0, 0, 0, 0, "RGBA(0,0,0,0)"
		},
		{
			0, 0, 0, 0.5f, "RGBA(0,0,0,0.5)"
		},
		{
			0, 0, 0, 1, "RGBA(0,0,0,1)"
		},
		{
			0, 0, 0, int.MaxValue, "RGBA(0,0,0,1)"
		},
		{
			int.MaxValue, int.MaxValue, int.MaxValue, int.MinValue, "RGBA(255,255,255,0)"
		},
		{
			int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue, "RGBA(255,255,255,1)"
		},
		{
			0, 0, 1, 0, "RGBA(0,0,255,0)"
		},
		{
			0, 0, 1, 1, "RGBA(0,0,255,1)"
		},
		{
			0, 1, 0, 0, "RGBA(0,255,0,0)"
		},
		{
			0, 1, 0, 1, "RGBA(0,255,0,1)"
		},
		{
			0, 1, 1, 0, "RGBA(0,255,255,0)"
		},
		{
			0, 1, 1, 1, "RGBA(0,255,255,1)"
		},
		{
			1, 0, 0, 0, "RGBA(255,0,0,0)"
		},
		{
			1, 0, 0, 1, "RGBA(255,0,0,1)"
		},
		{
			1, 0, 1, 0, "RGBA(255,0,255,0)"
		},
		{
			1, 0, 1, 1, "RGBA(255,0,255,1)"
		},
		{
			1, 1, 0, 0, "RGBA(255,255,0,0)"
		},
		{
			1, 1, 0, 1, "RGBA(255,255,0,1)"
		},
		{
			1, 1, 1, 0, "RGBA(255,255,255,0)"
		},
		{
			1, 1, 1, 1, "RGBA(255,255,255,1)"
		},
		{
			0.5f, 0, 0, 1, "RGBA(128,0,0,1)"
		},
		{
			0.5f, 0, 0, 0, "RGBA(128,0,0,0)"
		},
		{
			0, 0.5f, 0, 1, "RGBA(0,128,0,1)"
		},
		{
			0, 0.5f, 0, 0, "RGBA(0,128,0,0)"
		},
		{
			0, 0, 0.5f, 1, "RGBA(0,0,128,1)"
		},
		{
			0, 0, 0.5f, 0, "RGBA(0,0,128,0)"
		},
		{
			0.5f, 0.5f, 0.5f, 1, "RGBA(128,128,128,1)"
		},
		{
			0.5f, 0.5f, 0.5f, 0, "RGBA(128,128,128,0)"
		},
		{
			0.25f, 0.25f, 0.25f, 1, "RGBA(64,64,64,1)"
		},
		{
			0.25f, 0.25f, 0.25f, 0, "RGBA(64,64,64,0)"
		},
		{
			0.25f, 0.25f, 1, 1, "RGBA(64,64,255,1)"
		},
		{
			0.25f, 0.25f, 1, 0, "RGBA(64,64,255,0)"
		},
		{
			0.25f, 1, 0.25f, 1, "RGBA(64,255,64,1)"
		},
		{
			0.25f, 1, 0.25f, 0, "RGBA(64,255,64,0)"
		},
		{
			0.75f, 1, 0.25f, 1, "RGBA(191,255,64,1)"
		},
		{
			0.75f, 1, 0.25f, 0, "RGBA(191,255,64,0)"
		},
		{
			0.75f, 0, 1, 1, "RGBA(191,0,255,1)"
		},
		{
			0.75f, 0, 1, 0, "RGBA(191,0,255,0)"
		},
	};

	[Theory]
	[MemberData(nameof(ValidInputData))]
	public void ColorToRgbStringConverterValidInputTest(float red, float green, float blue, float alpha, string expectedResult)
	{
		var converter = new ColorToRgbaStringConverter();
		var color = new Color(red, green, blue, alpha);

		var resultConvert = ((ICommunityToolkitValueConverter)converter).Convert(color, typeof(string), null, new System.Globalization.CultureInfo("en-US"));
		var resultConvertFrom = converter.ConvertFrom(color, new System.Globalization.CultureInfo("en-US"));

		Assert.Equal(expectedResult, resultConvert);
		Assert.Equal(expectedResult, resultConvertFrom);
	}

	[Theory]
	[MemberData(nameof(ValidInputData))]
	public void ColorToRgbStringConverterConvertBackValidInputTest(float red, float green, float blue, float alpha, string colorString)
	{
		var converter = new ColorToRgbaStringConverter();
		var expectedResult = new Color(red, green, blue, alpha);

		var resultConvertBack = ((ICommunityToolkitValueConverter)converter).ConvertBack(colorString, typeof(Color), null, null);
		var resultConvertBackTo = converter.ConvertBackTo(colorString, System.Globalization.CultureInfo.InvariantCulture);

		AssertColorComparison(expectedResult, resultConvertBack as Color);
		AssertColorComparison(expectedResult, resultConvertBackTo);
	}

	[Theory]
	[InlineData("uk-UA", "RGBA(0,0,0,0,5)")]
	[InlineData("tr-TR", "RGBA(0,0,0,0,5)")]
	public void ColorToRgbStringConverterCultureTest(string cultureName, string expectedResult)
	{
		var culture = new System.Globalization.CultureInfo(cultureName);
		var converter = new ColorToRgbaStringConverter();
		var color = new Color(0, 0, 0, 0.5f);

		var resultConvert = ((ICommunityToolkitValueConverter)converter).Convert(color, typeof(string), null, culture);
		var resultConvertFrom = converter.ConvertFrom(color, culture);

		Assert.Equal(expectedResult, resultConvert);
		Assert.Equal(expectedResult, resultConvertFrom);
	}

	[Theory]
	[InlineData("de-DE")]
	[InlineData("fr-FR")]
	[InlineData("en-US")]
	[InlineData("uk-UA")]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	public void ColorToRgbaStringConverter_RoundTripsWithCulture(string cultureName)
	{
		var culture = new System.Globalization.CultureInfo(cultureName);
		var converter = new ColorToRgbaStringConverter();
		var color = new Color(1f, 0f, 0f, 0.5f);

		var colorString = converter.ConvertFrom(color, culture);
		var result = converter.ConvertBackTo(colorString, culture);

		AssertColorComparison(color, result);
		Assert.Equal(color.Alpha, result.Alpha, 2);
	}

	[Theory]
	[InlineData("de-DE", "RGBA(255,0,0,0,5)", 1f, 0f, 0f, 0.5f)]
	[InlineData("tr-TR", "RGBA(255,0,0,0,5)", 1f, 0f, 0f, 0.5f)]
	[InlineData("de-DE", "RGBA(255, 0, 0, 0,5)", 1f, 0f, 0f, 0.5f)]
	[InlineData("tr-TR", "RGBA(255, 0, 0, 0,5)", 1f, 0f, 0f, 0.5f)]
	[InlineData("de-DE", "RGBA(255,0,0,1,5)", 1f, 0f, 0f, 1f)]
	[InlineData("tr-TR", "RGBA(255,0,0,1,5)", 1f, 0f, 0f, 1f)]
	[InlineData("de-DE", "RGBA(255,0,0,-0,5)", 1f, 0f, 0f, 0f)]
	[InlineData("tr-TR", "RGBA(255,0,0,-0,5)", 1f, 0f, 0f, 0f)]
	public void ColorToRgbaStringConverter_ConvertBack_CommaDecimalInput_ParsesAndClampsAlpha(string cultureName, string colorString, float red, float green, float blue, float alpha)
	{
		var result = new ColorToRgbaStringConverter().ConvertBackTo(colorString, new System.Globalization.CultureInfo(cultureName));

		Assert.Equal(red, result.Red, 2);
		Assert.Equal(green, result.Green, 2);
		Assert.Equal(blue, result.Blue, 2);
		Assert.Equal(alpha, result.Alpha, 2);
	}

	[Theory]
	[InlineData("de-DE", "RGBA(256,0,0,0,5)")]
	[InlineData("tr-TR", "RGBA(256,0,0,0,5)")]
	[InlineData("de-DE", "RGBA(255,0,0,abc)")]
	[InlineData("tr-TR", "RGBA(255,0,0,abc)")]
	[InlineData("de-DE", "RGBA(255,0,0)")]
	[InlineData("tr-TR", "RGBA(255,0,0)")]
	[InlineData("de-DE", "not a color")]
	[InlineData("tr-TR", "not a color")]
	public void ColorToRgbaStringConverter_ConvertBack_InvalidCultureAwareInput_ReturnsDefault(string cultureName, string colorString)
	{
		var converter = new ColorToRgbaStringConverter();

		var result = converter.ConvertBackTo(colorString, new System.Globalization.CultureInfo(cultureName));

		Assert.Equal(converter.DefaultConvertBackReturnValue, result);
	}

	[Fact]
	public void ColorToRgbStringConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => new ColorToRgbaStringConverter().ConvertFrom(null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToRgbaStringConverter()).Convert(null, typeof(string), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToRgbaStringConverter()).Convert(new Color(), null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[InlineData("tr-TR")]
	[InlineData("de-DE")]
	[InlineData("en-US")]
	[InlineData("az-Latn-AZ")]
	[InlineData("sv-SE")]
	[InlineData("ar-SA")]
	public void ColorToRgbaStringConverter_NullCulture_WritesInvariantAndRoundTrips(string cultureName)
	{
		var currentCulture = System.Globalization.CultureInfo.GetCultureInfo(cultureName);
		System.Globalization.CultureInfo.CurrentCulture = currentCulture;
		System.Globalization.CultureInfo.CurrentUICulture = currentCulture;
		var converter = new ColorToRgbaStringConverter();
		var color = new Color(1f, 0f, 0f, 0.5f);

		var colorString = converter.ConvertFrom(color, null);
		var result = converter.ConvertBackTo(colorString, null);

		Assert.Equal("RGBA(255,0,0,0.5)", colorString);
		AssertColorComparison(color, result);
		Assert.Equal(color.Alpha, result.Alpha, 2);
	}

	[Theory]
	[InlineData("de-DE")]
	[InlineData("tr-TR")]
	public void ColorToRgbaStringConverter_NullCulture_ConvertsBackCommaDecimalFromCurrentCulture(string cultureName)
	{
		var currentCulture = System.Globalization.CultureInfo.GetCultureInfo(cultureName);
		System.Globalization.CultureInfo.CurrentCulture = currentCulture;
		System.Globalization.CultureInfo.CurrentUICulture = currentCulture;
		var converter = new ColorToRgbaStringConverter();
		const string stringWrittenByV17 = "RGBA(255,0,0,0,5)";

		var resultConvertBackTo = converter.ConvertBackTo(stringWrittenByV17, null);
		var resultConvertBack = ((ICommunityToolkitValueConverter)converter).ConvertBack(stringWrittenByV17, typeof(Color), null, null) as Color;

		AssertColorComparison(new Color(1f, 0f, 0f), resultConvertBackTo);
		Assert.Equal(0.5f, resultConvertBackTo.Alpha, 2);
		AssertColorComparison(new Color(1f, 0f, 0f), resultConvertBack);
		Assert.Equal(0.5f, resultConvertBack?.Alpha ?? float.NaN, 2);
	}

	static void AssertColorComparison(Color expectedResult, Color? result)
	{
		const int precision = 2;

		Assert.NotNull(result);
		Assert.Equal(expectedResult.Red, result.Red, precision);
		Assert.Equal(expectedResult.Green, result.Green, precision);
		Assert.Equal(expectedResult.Blue, result.Blue, precision);
	}

	[CulturedTheory(cultures: ["en-US", "de-DE", "tr-TR", "ar-SA"])]
	[MemberData(nameof(ValidInputData))]
	public void ColorToRgbaStringConverter_ValidInput_NullCulture_IgnoresCurrentCulture(float red, float green, float blue, float alpha, string expectedResult)
	{
		var converter = new ColorToRgbaStringConverter();
		var color = new Color(red, green, blue, alpha);

		var resultConvert = ((ICommunityToolkitValueConverter)converter).Convert(color, typeof(string), null, null);

		Assert.Equal(expectedResult, resultConvert);
		Assert.Equal(expectedResult, converter.ConvertFrom(color));
		Assert.Equal(expectedResult, converter.ConvertFrom(color, null));
	}

	[Theory]
	[InlineData("en-US")]
	[InlineData("tr-TR")]
	[InlineData("de-DE")]
	public void ColorToRgbaStringConverter_InvariantCulture_UsesDocumentedFormat(string currentCultureName)
	{
		var currentCulture = System.Globalization.CultureInfo.GetCultureInfo(currentCultureName);
		System.Globalization.CultureInfo.CurrentCulture = currentCulture;
		System.Globalization.CultureInfo.CurrentUICulture = currentCulture;
		var converter = new ColorToRgbaStringConverter();
		var color = Colors.Red.WithAlpha(0.5f);

		var resultConvert = ((ICommunityToolkitValueConverter)converter).Convert(color, typeof(string), null, System.Globalization.CultureInfo.InvariantCulture);

		Assert.Equal("RGBA(255,0,0,0.5)", resultConvert);
		Assert.Equal("RGBA(255,0,0,0.5)", converter.ConvertFrom(color, System.Globalization.CultureInfo.InvariantCulture));
	}
}