using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.UnitTests.TestData;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions;

public class ColorConversionExtensions_Tests
{
	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void ToRgbString(ColorTestDefinition testDef)
	{
		var result = testDef.Color.ToRgbString();

		Assert.Equal(testDef.ExpectedRGB, result);
	}

	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void ToRgbaString(ColorTestDefinition testDef)
	{
		var result = testDef.Color.ToRgbaString();

		Assert.Equal(testDef.ExpectedRGBA, result);
	}

	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void ToHexRgbString(ColorTestDefinition testDef)
	{
		var result = testDef.Color.ToHexRgbString();

		Assert.Equal(testDef.ExpectedHEXRGB, result);
	}

	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void ToHexRgbaString(ColorTestDefinition testDef)
	{
		var result = testDef.Color.ToHexRgbaString();

		Assert.Equal(testDef.ExpectedHEXRGBA, result);
	}

	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void ToHexArgbString(ColorTestDefinition testDef)
	{
		var result = testDef.Color.ToHexArgbString();

		Assert.Equal(testDef.ExpectedHEXARGB, result);
	}

	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void ToCmykString(ColorTestDefinition testDef)
	{
		var result = testDef.Color.ToCmykString();

		Assert.Equal(testDef.ExpectedCMYK, result);
	}

	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void ToCmykaString(ColorTestDefinition testDef)
	{
		var result = testDef.Color.ToCmykaString();

		Assert.Equal(testDef.ExpectedCMYKA, result);
	}

	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void ToHslString(ColorTestDefinition testDef)
	{
		var result = testDef.Color.ToHslString();

		Assert.Equal(testDef.ExpectedHslString, result);
	}

	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void ToHslaString(ColorTestDefinition testDef)
	{
		var result = testDef.Color.ToHslaString();

		Assert.Equal(testDef.ExpectedHslaString, result);
	}

	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void GetByteRed(ColorTestDefinition testDef)
	{
		var result = testDef.Color.GetByteRed();

		Assert.Equal(testDef.ExpectedByteR, result);
	}

	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void GetByteGreen(ColorTestDefinition testDef)
	{
		var result = testDef.Color.GetByteGreen();

		Assert.Equal(testDef.ExpectedByteG, result);
	}

	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void GetByteBlue(ColorTestDefinition testDef)
	{
		var result = testDef.Color.GetByteBlue();

		Assert.Equal(testDef.ExpectedByteB, result);
	}

	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void GetByteAlpha(ColorTestDefinition testDef)
	{
		var result = testDef.Color.GetByteAlpha();

		Assert.Equal(testDef.ExpectedByteA, result);
	}

	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void GetPctBlackKey(ColorTestDefinition def)
	{
		var result = def.Color.GetPercentBlackKey();

		Assert.Equal(def.ExpectedPctBlack, result);
	}
	
	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void GetDegreeHue(ColorTestDefinition def)
	{
		var result = def.Color.GetDegreeHue();

		Assert.Equal(def.ExpectedDegreeHue, result);
	}

	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void GetPctCyan(ColorTestDefinition testDef)
	{
		var result = testDef.Color.GetPercentCyan();

		Assert.Equal(testDef.ExpectedPctCyan, result);
	}

	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void GetPctMagenta(ColorTestDefinition testDef)
	{
		var result = testDef.Color.GetPercentMagenta();

		Assert.Equal(testDef.ExpectedPctMagenta, result);
	}

	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void GetPctYellow(ColorTestDefinition testDef)
	{
		var result = testDef.Color.GetPercentYellow();

		Assert.Equal(testDef.ExpectedPctYellow, result);
	}

	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void ToInverseColor(ColorTestDefinition testDef)
	{
		var result = testDef.Color.ToInverseColor();

		Assert.Equal(testDef.ExpectedInverse, result);
	}

	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void ToGrayScale(ColorTestDefinition testDef)
	{
		var result = testDef.Color.ToGrayScale();

		Assert.Equal(testDef.ExpectedGreyScale, result);
	}

	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void IsDark(ColorTestDefinition testDef)
	{
		var result = testDef.Color.IsDark();

		Assert.Equal(testDef.ExpectedIsDark, result);
	}

	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void IsDarkForTheEye(ColorTestDefinition testDef)
	{
		var result = testDef.Color.IsDarkForTheEye();

		Assert.Equal(testDef.ExpectedIsDarkForEye, result);
	}

	[Fact]
	public void GetPctCyanShouldNotCrashOnBlackColor()
	{
		var black = Colors.Black;
		black.GetPercentCyan();
		Assert.True(true);
	}

	[Fact]
	public void GetPctMagentaShouldNotCrashOnBlackColor()
	{
		var black = Colors.Black;
		black.GetPercentMagenta();
		Assert.True(true);
	}

	[Fact]
	public void GetPctYellowShouldNotCrashOnBlackColor()
	{
		var black = Colors.Black;
		black.GetPercentYellow();
		Assert.True(true);
	}

	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void ToBlackOrWhite(ColorTestDefinition testDef)
	{
		var result = testDef.Color.ToBlackOrWhite();

		Assert.Equal(testDef.ExpectedToBlackOrWhite, result);
	}

	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void ToBlackOrWhiteForText(ColorTestDefinition testDef)
	{
		var result = testDef.Color.ToBlackOrWhiteForText();

		Assert.Equal(testDef.ExpectedToBlackOrWhiteForText, result);
	}

	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void WithRed_Double(ColorTestDefinition testDef)
	{
		var red = new Random().NextDouble();
		var result = testDef.Color.WithRed(red);

		Assert.Equal((float)red, result.Red);
	}

	[Fact]
	public void WithRed_Double_RedGeaterThan1ShouldThrowAgumentOutOfRangeException()
	{
		Color c = new ();
		var red = new Random().Next(2, int.MaxValue);

		Assert.Throws<ArgumentOutOfRangeException>(() =>
		{
			c.WithRed(red);
		});
	}

	[Fact]
	public void WithRed_Double_RedNegativeShouldThrowAgumentOutOfRangeException()
	{
		Color c = new ();
		var red = -new Random().NextDouble();

		Assert.Throws<ArgumentOutOfRangeException>(() =>
		{
			c.WithRed(red);
		});
	}

	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void WithGreen_Double(ColorTestDefinition testDef)
	{
		var green = new Random().NextDouble();
		var result = testDef.Color.WithGreen(green);

		Assert.Equal((float)green, result.Green);
	}

	[Fact]
	public void WithGreen_Double_GreenGeaterThan1ShouldThrowAgumentOutOfRangeException()
	{
		Color c = new ();
		var green = new Random().Next(2, int.MaxValue);

		Assert.Throws<ArgumentOutOfRangeException>(() =>
		{
			c.WithGreen(green);
		});
	}

	[Fact]
	public void WithGreen_Double_GreenNegativeShouldThrowAgumentOutOfRangeException()
	{
		Color c = new ();
		var green = -new Random().NextDouble();

		Assert.Throws<ArgumentOutOfRangeException>(() =>
		{
			c.WithGreen(green);
		});
	}

	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void WithBlue_Double(ColorTestDefinition testDef)
	{
		var blue = new Random().NextDouble();
		var result = testDef.Color.WithBlue(blue);

		Assert.Equal((float)blue, result.Blue);
	}

	[Fact]
	public void WithBlue_Double_BlueGeaterThan1ShouldThrowAgumentOutOfRangeException()
	{
		Color c = new ();

		var blue = new Random().Next(2, int.MaxValue);

		Assert.Throws<ArgumentOutOfRangeException>(() =>
		{
			c.WithBlue(blue);
		});
	}

	[Fact]
	public void WithBlue_Double_BlueNegativeShouldThrowAgumentOutOfRangeException()
	{
		Color c = new ();

		var blue = -new Random().NextDouble();

		Assert.Throws<ArgumentOutOfRangeException>(() =>
		{
			c.WithBlue(blue);
		});
	}

	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void WithRed_Byte(ColorTestDefinition testDef)
	{
		var red = (byte)new Random().Next(0, 256);
		var result = testDef.Color.WithRed(red);

		Assert.Equal(red, result.GetByteRed());
	}

	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void WithGreen_Byte(ColorTestDefinition testDef)
	{
		var green = (byte)new Random().Next(0, 256);
		var result = testDef.Color.WithGreen(green);

		Assert.Equal(green, result.GetByteGreen());
	}

	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void WithBlue_Byte(ColorTestDefinition testDef)
	{
		var blue = (byte)new Random().Next(0, 256);
		var result = testDef.Color.WithBlue(blue);

		Assert.Equal(blue, result.GetByteBlue());
	}

	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void WithCyan(ColorTestDefinition testDef)
	{
		var pctCyan = new Random().NextDouble();
		var newColor = testDef.Color.WithCyan(pctCyan);

		var expectedRed = (1 - pctCyan) * (1 - testDef.ExpectedPctBlack);

		Assert.Equal(Math.Round(expectedRed, 4), Math.Round(newColor.Red, 4));
		Assert.Equal(Math.Round(testDef.G, 4), Math.Round(newColor.Green, 4));
		Assert.Equal(Math.Round(testDef.B, 4), Math.Round(newColor.Blue, 4));
		Assert.Equal(Math.Round(testDef.A, 4), Math.Round(newColor.Alpha, 4));
	}

	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void WithMagenta(ColorTestDefinition testDef)
	{
		var pctMagenta = new Random().NextDouble();
		var newColor = testDef.Color.WithMagenta(pctMagenta);

		var expectedGreen = (1 - pctMagenta) * (1 - testDef.ExpectedPctBlack);

		Assert.Equal(Math.Round(expectedGreen, 4), Math.Round(newColor.Green, 4));
		Assert.Equal(Math.Round(testDef.R, 4), Math.Round(newColor.Red, 4));
		Assert.Equal(Math.Round(testDef.B, 4), Math.Round(newColor.Blue, 4));
		Assert.Equal(Math.Round(testDef.A, 4), Math.Round(newColor.Alpha, 4));
	}

	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void WithYellow(ColorTestDefinition testDef)
	{
		var pctYellow = new Random().NextDouble();
		var newColor = testDef.Color.WithYellow(pctYellow);

		var expectedBlue = (1 - pctYellow) * (1 - testDef.ExpectedPctBlack);

		Assert.Equal(Math.Round(expectedBlue, 4), Math.Round(newColor.Blue, 4));
		Assert.Equal(Math.Round(testDef.R, 4), Math.Round(newColor.Red, 4));
		Assert.Equal(Math.Round(testDef.G, 4), Math.Round(newColor.Green, 4));
		Assert.Equal(Math.Round(testDef.A, 4), Math.Round(newColor.Alpha, 4));
	}

	[Theory]
	[ClassData(typeof(ColorTestData))]
	public void WithBlackKey(ColorTestDefinition testDef)
	{
		var pctBlack = new Random().NextDouble();
		var newColor = testDef.Color.WithBlackKey(pctBlack);

		var expectedRed = (1 - testDef.ExpectedPctCyan) * (1 - pctBlack);
		var expectedGreen = (1 - testDef.ExpectedPctMagenta) * (1 - pctBlack);
		var expectedBlue = (1 - testDef.ExpectedPctYellow) * (1 - pctBlack);

		Assert.Equal(Math.Round(expectedRed, 4), Math.Round(newColor.Red, 4));
		Assert.Equal(Math.Round(expectedGreen, 4), Math.Round(newColor.Green, 4));
		Assert.Equal(Math.Round(expectedBlue, 4), Math.Round(newColor.Blue, 4));
		Assert.Equal(testDef.A, newColor.Alpha);
	}
}