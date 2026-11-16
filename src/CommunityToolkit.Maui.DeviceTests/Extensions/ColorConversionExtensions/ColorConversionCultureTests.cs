using System.Globalization;
using CommunityToolkit.Maui.Core.Extensions;

namespace CommunityToolkit.Maui.DeviceTests;

// Shared formatting code, but each platform ships its own globalization data (ICU, NLS or hybrid), so the "same output on every machine" promise is proven on every platform.
// The Android test app behaves invariantly for culture, so there this passes without proving the machine-culture half.
[Category(TestCategory.ColorConversionExtensions)]
[Collection(TestCollections.DisableParallelization)]
public class ColorConversionCultureTests : CommunityToolkitHandlerTestBase
{
	static readonly Color halfTransparentRed = Colors.Red.WithAlpha(0.5f);

	[Theory]
	[InlineData("tr-TR")]
	[InlineData("de-DE")]
	public void NoCultureAndNullCultureGiveTheDocumentedFormatInAnyCurrentCulture(string currentCultureName)
	{
		RunWithCurrentCulture(currentCultureName, () =>
		{
			Assert.Equal("RGB(255,0,0)", halfTransparentRed.ToRgbString());
			Assert.Equal("RGBA(255,0,0,0.5)", halfTransparentRed.ToRgbaString());
			Assert.Equal("RGBA(255,0,0,0.5)", halfTransparentRed.ToRgbaString(null));
			Assert.Equal("CMYK(0%,100%,100%,0%)", halfTransparentRed.ToCmykString());
			Assert.Equal("CMYK(0%,100%,100%,0%)", halfTransparentRed.ToCmykString(null));
			Assert.Equal("CMYKA(0%,100%,100%,0%,0.5)", halfTransparentRed.ToCmykaString());
			Assert.Equal("CMYKA(0%,100%,100%,0%,0.5)", halfTransparentRed.ToCmykaString(null));
			Assert.Equal("HSL(360,100%,50%)", halfTransparentRed.ToHslString());
			Assert.Equal("HSL(360,100%,50%)", halfTransparentRed.ToHslString(null));
			Assert.Equal("HSLA(360,100%,50%,0.5)", halfTransparentRed.ToHslaString());
			Assert.Equal("HSLA(360,100%,50%,0.5)", halfTransparentRed.ToHslaString(null));
		});
	}

	[Theory]
	[InlineData("tr-TR")]
	[InlineData("de-DE")]
	public void InvariantCultureGivesTheDocumentedFormatInAnyCurrentCulture(string currentCultureName)
	{
		RunWithCurrentCulture(currentCultureName, () =>
		{
			Assert.Equal("RGBA(255,0,0,0.5)", halfTransparentRed.ToRgbaString(CultureInfo.InvariantCulture));
			Assert.Equal("CMYK(0%,100%,100%,0%)", halfTransparentRed.ToCmykString(CultureInfo.InvariantCulture));
			Assert.Equal("CMYKA(0%,100%,100%,0%,0.5)", halfTransparentRed.ToCmykaString(CultureInfo.InvariantCulture));
			Assert.Equal("HSL(360,100%,50%)", halfTransparentRed.ToHslString(CultureInfo.InvariantCulture));
			Assert.Equal("HSLA(360,100%,50%,0.5)", halfTransparentRed.ToHslaString(CultureInfo.InvariantCulture));
		});
	}

	// The expectations come from each culture's NumberFormatInfo instead of literals, because iOS and Mac Catalyst may use hybrid globalization data.
	[Theory]
	[InlineData("tr-TR")]
	[InlineData("de-DE")]
	public void ExplicitCultureWinsOverTheCurrentCulture(string explicitCultureName)
	{
		RunWithCurrentCulture("en-US", () =>
		{
			var culture = CultureInfo.GetCultureInfo(explicitCultureName);
			var zeroPercent = 0f.ToString("P0", culture);
			var fullPercent = 1f.ToString("P0", culture);
			var halfPercent = 0.5f.ToString("P0", culture);
			var alpha = 0.5f.ToString(culture);

			Assert.Equal($"RGBA(255,0,0,{alpha})", halfTransparentRed.ToRgbaString(culture));
			Assert.Equal($"CMYK({zeroPercent},{fullPercent},{fullPercent},{zeroPercent})", halfTransparentRed.ToCmykString(culture));
			Assert.Equal($"CMYKA({zeroPercent},{fullPercent},{fullPercent},{zeroPercent},{alpha})", halfTransparentRed.ToCmykaString(culture));
			Assert.Equal($"HSL(360,{fullPercent},{halfPercent})", halfTransparentRed.ToHslString(culture));
			Assert.Equal($"HSLA(360,{fullPercent},{halfPercent},{alpha})", halfTransparentRed.ToHslaString(culture));
		});
	}

	static void RunWithCurrentCulture(string cultureName, Action assertions)
	{
		var originalCulture = CultureInfo.CurrentCulture;
		var originalUICulture = CultureInfo.CurrentUICulture;
		try
		{
			var culture = CultureInfo.GetCultureInfo(cultureName);
			CultureInfo.CurrentCulture = culture;
			CultureInfo.CurrentUICulture = culture;

			assertions();
		}
		finally
		{
			CultureInfo.CurrentCulture = originalCulture;
			CultureInfo.CurrentUICulture = originalUICulture;
		}
	}
}