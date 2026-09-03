using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Core.Views;
using Xunit;

namespace CommunityToolkit.Maui.DeviceTests.Tests.Additional;

public class ColorConversionWithMethodsTests
{
	[Fact]
	public void WithRed_Double_ReturnsModifiedColor()
	{
		var color = new Color(0f, 0.5f, 0.5f);

		var result = color.WithRed(1.0);

		Assert.Equal(1.0, result.Red, 0.01);
		Assert.Equal(0.5, result.Green, 0.01);
		Assert.Equal(0.5, result.Blue, 0.01);
	}

	[Fact]
	public void WithRed_Double_OutOfRange_Throws()
	{
		var color = Colors.Red;
		var thrown1 = false;
		var thrown2 = false;

		try
		{
			color.WithRed(1.5);
		}
		catch (ArgumentOutOfRangeException)
		{
			thrown1 = true;
		}

		try
		{
			color.WithRed(-0.1);
		}
		catch (ArgumentOutOfRangeException)
		{
			thrown2 = true;
		}

		Assert.True(thrown1);
		Assert.True(thrown2);
	}

	[Fact]
	public void WithGreen_Double_ReturnsModifiedColor()
	{
		var color = new Color(0.5f, 0f, 0.5f);

		var result = color.WithGreen(1.0);

		Assert.Equal(0.5, result.Red, 0.01);
		Assert.Equal(1.0, result.Green, 0.01);
		Assert.Equal(0.5, result.Blue, 0.01);
	}

	[Fact]
	public void WithGreen_Double_OutOfRange_Throws()
	{
		var color = Colors.Green;
		var thrown1 = false;
		var thrown2 = false;

		try
		{
			color.WithGreen(2.0);
		}
		catch (ArgumentOutOfRangeException)
		{
			thrown1 = true;
		}

		try
		{
			color.WithGreen(-1.0);
		}
		catch (ArgumentOutOfRangeException)
		{
			thrown2 = true;
		}

		Assert.True(thrown1);
		Assert.True(thrown2);
	}

	[Fact]
	public void WithBlue_Double_ReturnsModifiedColor()
	{
		var color = new Color(0.5f, 0.5f, 0f);

		var result = color.WithBlue(1.0);

		Assert.Equal(0.5, result.Red, 0.01);
		Assert.Equal(0.5, result.Green, 0.01);
		Assert.Equal(1.0, result.Blue, 0.01);
	}

	[Fact]
	public void WithBlue_Double_OutOfRange_Throws()
	{
		var color = Colors.Blue;
		var thrown1 = false;
		var thrown2 = false;

		try
		{
			color.WithBlue(1.01);
		}
		catch (ArgumentOutOfRangeException)
		{
			thrown1 = true;
		}

		try
		{
			color.WithBlue(-0.01);
		}
		catch (ArgumentOutOfRangeException)
		{
			thrown2 = true;
		}

		Assert.True(thrown1);
		Assert.True(thrown2);
	}

	[Fact]
	public void WithRed_Byte_ReturnsModifiedColor()
	{
		var color = new Color(0f, 0f, 0f);

		var result = color.WithRed((byte)255);

		Assert.Equal(1.0, result.Red, 0.01);
	}

	[Fact]
	public void WithGreen_Byte_ReturnsModifiedColor()
	{
		var color = new Color(0f, 0f, 0f);

		var result = color.WithGreen((byte)128);

		Assert.True(result.Green > 0.49 && result.Green < 0.51);
	}

	[Fact]
	public void WithBlue_Byte_ReturnsModifiedColor()
	{
		var color = new Color(0f, 0f, 0f);

		var result = color.WithBlue((byte)255);

		Assert.Equal(1.0, result.Blue, 0.01);
	}

	[Fact]
	public void WithCyan_ReturnsModifiedColor()
	{
		var color = Colors.Red;

		var result = color.WithCyan(0.5);

		Assert.NotNull(result);
	}

	[Fact]
	public void WithMagenta_ReturnsModifiedColor()
	{
		var color = Colors.Green;

		var result = color.WithMagenta(0.5);

		Assert.NotNull(result);
	}

	[Fact]
	public void WithYellow_ReturnsModifiedColor()
	{
		var color = Colors.Blue;

		var result = color.WithYellow(0.5);

		Assert.NotNull(result);
	}

	[Fact]
	public void WithBlackKey_ReturnsModifiedColor()
	{
		var color = Colors.White;

		var result = color.WithBlackKey(0.5);

		Assert.NotNull(result);
	}
}

public class ColorConversionGetMethodsTests
{
	[Fact]
	public void GetByteRed_ReturnsCorrectValue()
	{
		var color = new Color(1f, 0f, 0f);

		Assert.Equal(255, color.GetByteRed());
	}

	[Fact]
	public void GetByteGreen_ReturnsCorrectValue()
	{
		var color = new Color(0f, 1f, 0f);

		Assert.Equal(255, color.GetByteGreen());
	}

	[Fact]
	public void GetByteBlue_ReturnsCorrectValue()
	{
		var color = new Color(0f, 0f, 1f);

		Assert.Equal(255, color.GetByteBlue());
	}

	[Fact]
	public void GetByteAlpha_ReturnsCorrectValue()
	{
		var color = new Color(0f, 0f, 0f, 1f);

		Assert.Equal(255, color.GetByteAlpha());
	}

	[Fact]
	public void GetByteAlpha_HalfTransparent()
	{
		var color = new Color(0f, 0f, 0f, 0.5f);

		Assert.Equal(128, color.GetByteAlpha());
	}

	[Fact]
	public void GetDegreeHue_ScalesHueTo360()
	{
		var color = Colors.Red;

		// GetDegreeHue is defined as GetHue() * 360; verify the contract rather than a specific color value
		Assert.Equal(color.GetHue() * 360, color.GetDegreeHue(), 3);
	}

	[Fact]
	public void GetPercentBlackKey_White_IsZero()
	{
		var color = Colors.White;

		Assert.Equal(0f, color.GetPercentBlackKey(), 0.01f);
	}

	[Fact]
	public void GetPercentBlackKey_Black_IsOne()
	{
		var color = Colors.Black;

		Assert.Equal(1f, color.GetPercentBlackKey(), 0.01f);
	}

	[Fact]
	public void GetPercentCyan_Red_IsZero()
	{
		var color = Colors.Red;

		Assert.Equal(0f, color.GetPercentCyan(), 0.01f);
	}

	[Fact]
	public void GetPercentMagenta_Green_IsZero()
	{
		var color = Colors.Green;

		Assert.Equal(0f, color.GetPercentMagenta(), 0.01f);
	}

	[Fact]
	public void GetPercentYellow_Blue_IsZero()
	{
		var color = Colors.Blue;

		Assert.Equal(0f, color.GetPercentYellow(), 0.01f);
	}
}

public class ColorConversionToMethodsTests
{
	[Fact]
	public void ToInverseColor_Red_ReturnsCyan()
	{
		var color = Colors.Red;

		var result = color.ToInverseColor();

		Assert.Equal(0f, result.Red, 0.01f);
		Assert.Equal(1f, result.Green, 0.01f);
		Assert.Equal(1f, result.Blue, 0.01f);
	}

	[Fact]
	public void ToInverseColor_Black_ReturnsWhite()
	{
		var color = Colors.Black;

		var result = color.ToInverseColor();

		Assert.Equal(1f, result.Red, 0.01f);
		Assert.Equal(1f, result.Green, 0.01f);
		Assert.Equal(1f, result.Blue, 0.01f);
	}

	[Fact]
	public void ToBlackOrWhite_LightColor_ReturnsWhite()
	{
		var color = Colors.White;

		var result = color.ToBlackOrWhite();

		Assert.Equal(Colors.White, result);
	}

	[Fact]
	public void ToBlackOrWhite_DarkColor_ReturnsBlack()
	{
		var color = Colors.Black;

		var result = color.ToBlackOrWhite();

		Assert.Equal(Colors.Black, result);
	}

	[Fact]
	public void ToGrayScale_ReturnsGray()
	{
		var color = Colors.Red;

		var result = color.ToGrayScale();

		// Grayscale should have equal R, G, B components
		Assert.Equal(result.Red, result.Green, 0.01f);
		Assert.Equal(result.Green, result.Blue, 0.01f);
	}
}

public class AppThemeObjectTests
{
	[Fact]
	public void AppThemeColor_DefaultProperties_AreNull()
	{
		var appThemeColor = new AppThemeColor();

		Assert.Null(appThemeColor.Light);
		Assert.Null(appThemeColor.Dark);
		Assert.Null(appThemeColor.Default);
	}

	[Fact]
	public void AppThemeColor_CanSetLightDarkDefault()
	{
		var appThemeColor = new AppThemeColor
		{
			Light = Colors.White,
			Dark = Colors.Black,
			Default = Colors.Gray
		};

		Assert.Equal(Colors.White, appThemeColor.Light);
		Assert.Equal(Colors.Black, appThemeColor.Dark);
		Assert.Equal(Colors.Gray, appThemeColor.Default);
	}

	[Fact]
	public void AppThemeObject_DefaultProperties_AreNull()
	{
		var appThemeObject = new AppThemeObject();

		Assert.Null(appThemeObject.Light);
		Assert.Null(appThemeObject.Dark);
		Assert.Null(appThemeObject.Default);
	}

	[Fact]
	public void AppThemeObject_CanSetLightDarkDefault()
	{
		var appThemeObject = new AppThemeObject
		{
			Light = "LightValue",
			Dark = "DarkValue",
			Default = "DefaultValue"
		};

		Assert.Equal("LightValue", appThemeObject.Light);
		Assert.Equal("DarkValue", appThemeObject.Dark);
		Assert.Equal("DefaultValue", appThemeObject.Default);
	}

	[Fact]
	public void AppThemeColor_GetBinding_ReturnsBinding()
	{
		var appThemeColor = new AppThemeColor
		{
			Light = Colors.White,
			Dark = Colors.Black
		};

		var binding = appThemeColor.GetBinding();

		Assert.NotNull(binding);
	}

	[Fact]
	public void AppThemeObject_GetBinding_ReturnsBinding()
	{
		var appThemeObject = new AppThemeObject
		{
			Light = 42,
			Dark = 24
		};

		var binding = appThemeObject.GetBinding();

		Assert.NotNull(binding);
	}
}

public class SnackbarOptionsTests
{
	[Fact]
	public void SnackbarOptions_DefaultProperties()
	{
		var options = new SnackbarOptions();

		Assert.Equal(0.0, options.CharacterSpacing);
		Assert.Equal(Colors.Black, options.TextColor);
		Assert.Equal(Colors.Black, options.ActionButtonTextColor);
		Assert.Equal(Colors.LightGray, options.BackgroundColor);
		Assert.Equal(new CornerRadius(4, 4, 4, 4), options.CornerRadius);
	}

	[Fact]
	public void SnackbarOptions_CanSetProperties()
	{
		var options = new SnackbarOptions
		{
			CharacterSpacing = 2.0,
			TextColor = Colors.Red,
			ActionButtonTextColor = Colors.Blue,
			BackgroundColor = Colors.Yellow,
			CornerRadius = new CornerRadius(8)
		};

		Assert.Equal(2.0, options.CharacterSpacing);
		Assert.Equal(Colors.Red, options.TextColor);
		Assert.Equal(Colors.Blue, options.ActionButtonTextColor);
		Assert.Equal(Colors.Yellow, options.BackgroundColor);
		Assert.Equal(new CornerRadius(8), options.CornerRadius);
	}

	[Fact]
	public void SnackbarOptions_CanSetFont()
	{
		var font = Microsoft.Maui.Font.SystemFontOfSize(20);
		var options = new SnackbarOptions
		{
			Font = font,
			ActionButtonFont = font
		};

		Assert.Equal(font, options.Font);
		Assert.Equal(font, options.ActionButtonFont);
	}
}

public class FolderRecordTests
{
	[Fact]
	public void Folder_StoresPathAndName()
	{
		var folder = new Folder("/tmp/test", "test");

		Assert.Equal("/tmp/test", folder.Path);
		Assert.Equal("test", folder.Name);
	}

	[Fact]
	public void Folder_Equality_SameValues()
	{
		var folder1 = new Folder("/tmp/test", "test");
		var folder2 = new Folder("/tmp/test", "test");

		Assert.Equal(folder1, folder2);
	}

	[Fact]
	public void Folder_Inequality_DifferentValues()
	{
		var folder1 = new Folder("/tmp/test1", "test1");
		var folder2 = new Folder("/tmp/test2", "test2");

		Assert.NotEqual(folder1, folder2);
	}

	[Fact]
	public void Folder_WithExpression_CreatesCopy()
	{
		var folder = new Folder("/tmp/test", "test");
		var modified = folder with { Name = "modified" };

		Assert.Equal("/tmp/test", modified.Path);
		Assert.Equal("modified", modified.Name);
		Assert.Equal("test", folder.Name);
	}

	[Fact]
	public void Folder_ToString_ContainsValues()
	{
		var folder = new Folder("/tmp/test", "test");
		var str = folder.ToString();

		Assert.Contains("/tmp/test", str);
		Assert.Contains("test", str);
	}
}

public class CoreEventArgsAdditionalTests
{
	[Fact]
	public void TouchGestureCompletedEventArgs_CarriesParameter()
	{
		var args = new TouchGestureCompletedEventArgs("param");

		Assert.Equal("param", args.TouchCommandParameter);
	}

	[Fact]
	public void TouchGestureCompletedEventArgs_NullParameter()
	{
		var args = new TouchGestureCompletedEventArgs(null);

		Assert.Null(args.TouchCommandParameter);
	}

	[Fact]
	public void LongPressCompletedEventArgs_NullParameter()
	{
		var args = new LongPressCompletedEventArgs(null);

		Assert.Null(args.LongPressCommandParameter);
	}

	[Fact]
	public void DrawingLineCompletedEventArgs_CarriesLine()
	{
		var line = new DrawingLine();
		var args = new DrawingLineCompletedEventArgs(line);

		Assert.Same(line, args.LastDrawingLine);
	}

	[Fact]
	public void DrawingLineStartedEventArgs_CarriesPoint()
	{
		var point = new PointF(5f, 10f);
		var args = new DrawingLineStartedEventArgs(point);

		Assert.Equal(point, args.Point);
	}
}