using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ColorToColorConverter_Tests : BaseTest
{
	public static IReadOnlyList<object?[]> ColorToBlackOrWhiteData { get; } = new[]
	{
		new object[] { Colors.Black, Colors.Black },
		new object[] { Colors.DarkBlue, Colors.Black },
		new object[] { Colors.DarkCyan, Colors.Black },
		new object[] { Colors.Brown, Colors.Black },
		new object[] { Colors.DarkGreen, Colors.Black },
		new object[] { Colors.DarkSlateGray, Colors.Black },
		new object[] { Colors.Transparent, Colors.Black},
		new object[] { Colors.White, Colors.White},
		new object[] { Colors.DarkSalmon, Colors.White },
		new object[] { Colors.DarkOrchid, Colors.White },
		new object[] { Colors.DarkGrey, Colors.White },
		new object[] { Colors.Yellow, Colors.White },
		new object[] { Colors.Pink, Colors.White },
		new object[] { Colors.LightBlue, Colors.White },
		new object[] { Colors.Wheat, Colors.White }
	};

	public static IReadOnlyList<object?[]> ColorToColorForTextData { get; } = new[]
	{
		new object[] { Colors.White, Colors.Black},
		new object[] { Colors.Yellow, Colors.Black },
		new object[] { Colors.Pink, Colors.Black },
		new object[] { Colors.LightBlue, Colors.Black },
		new object[] { Colors.Wheat, Colors.Black },
		new object[] { Colors.Black, Colors.White },
		new object[] { Colors.DarkBlue, Colors.White },
		new object[] { Colors.DarkCyan, Colors.White },
		new object[] { Colors.Brown, Colors.White },
		new object[] { Colors.DarkGreen, Colors.White },
		new object[] { Colors.DarkSlateGray, Colors.White },
		new object[] { Colors.Transparent, Colors.White},
		new object[] { Colors.DarkSalmon, Colors.White },
		new object[] { Colors.DarkOrchid, Colors.White },
		new object[] { Colors.DarkGrey, Colors.White }
	};

	public static IReadOnlyList<object?[]> ColorToGrayScaleColorData { get; } = new[]
	{
		new object[] { Colors.White, Colors.White},
		new object[] { Colors.Yellow, new Color(2f/3f, 2f/3f, 2f/3f, 1) },
		new object[] { Colors.Pink, new Color(0.8496732f, 0.8496732f, 0.8496732f, 1) },
		new object[] { Colors.LightBlue, new Color(0.8091503f, 0.8091503f, 0.8091503f, 1) },
		new object[] { Colors.Wheat, new Color(0.84444445f, 0.84444445f, 0.84444445f, 1) },
		new object[] { Colors.Black, Colors.Black },
		new object[] { Colors.DarkBlue, new Color(0.18169935f, 0.18169935f, 0.18169935f, 1) },
		new object[] { Colors.DarkCyan, new Color(0.3633987f, 0.3633987f, 0.3633987f, 1) },
		new object[] { Colors.Brown, new Color(0.3254902f, 0.3254902f, 0.3254902f, 1) },
		new object[] { Colors.DarkGreen, new Color(0.13071896f, 0.13071896f, 0.13071896f, 1) },
		new object[] { Colors.DarkSlateGray, new Color(0.26797387f, 0.26797387f, 0.26797387f, 1) },
		new object[] { Colors.Transparent, Colors.Black},
		new object[] { Colors.DarkSalmon, new Color(0.66013074f, 0.66013074f, 0.66013074f, 1) },
		new object[] { Colors.DarkOrchid, new Color(0.5320262f, 0.5320262f, 0.5320262f, 1) },
		new object[] { Colors.DarkGrey, new Color(0.6627451f, 0.6627451f, 0.6627451f, 1) }
	};

	public static IReadOnlyList<object?[]> ColorToInverseColorConverterData { get; } = new[]
	{
		new object[] { Colors.White, Colors.Black},
		new object[] { new Color(0,0,0), new Color(1,1,1) },
		new object[] { new Color(0,0,1), new Color(1,1,0) },
		new object[] { new Color(0,1,0), new Color(1,0,1) },
		new object[] { new Color(0,1,1), new Color(1,0,0) },
		new object[] { new Color(1,0,0), new Color(0,1,1) },
		new object[] { new Color(1,0,1), new Color(0,1,0) },
		new object[] { new Color(1,1,0), new Color(0,0,1) },
		new object[] { new Color(1,1,1), new Color(0,0,0) },
		new object[] { Colors.Black, Colors.White },
	};

	[Theory]
	[MemberData(nameof(ColorToBlackOrWhiteData))]
	public void ColorToBlackOrWhiteConverterValidArgumentsTest(Color initialColor, Color expectedColor)
	{
		var converter = new ColorToBlackOrWhiteConverter();

		var convertedColor = converter.Convert(initialColor, typeof(Color), null, null);
		var convertedColorFrom = converter.ConvertFrom(initialColor);

		Assert.Equal(expectedColor, convertedColor);
		Assert.Equal(expectedColor, convertedColorFrom);
	}

	[Theory]
	[InlineData(2)]
	[InlineData('c')]
	[InlineData(true)]
	public void ColorToBlackOrWhiteConverterConvertInvalidArgumentsTest(object value)
	{
		var converter = new ColorToBlackOrWhiteConverter();

		Assert.Throws<ArgumentException>(() => converter.Convert(value, typeof(Color), null, null));
	}

	[Fact]
	public void ColorToBlackOrWhiteConverterNullArgumentsTest()
	{
		var converter = new ColorToBlackOrWhiteConverter();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => converter.Convert(null, typeof(Color), null, null));
		Assert.Throws<ArgumentNullException>(() => converter.Convert(default, typeof(Color), null, null));
		Assert.Throws<ArgumentNullException>(() => converter.ConvertFrom(null));
		Assert.Throws<ArgumentNullException>(() => converter.ConvertFrom(default));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[MemberData(nameof(ColorToColorForTextData))]
	public void ColorToColorForTextConverterValidArgumentsTest(Color initialColor, Color expectedColor)
	{
		var converter = new ColorToColorForTextConverter();

		var convertedColor = converter.Convert(initialColor, typeof(Color), null, null);
		var convertedColorFrom = converter.ConvertFrom(initialColor);

		Assert.Equal(expectedColor, convertedColor);
		Assert.Equal(expectedColor, convertedColorFrom);
	}

	[Theory]
	[InlineData(2)]
	[InlineData('c')]
	[InlineData(true)]
	public void ColorToColorForTextConverterConvertInvalidArgumentsTest(object value)
	{
		var converter = new ColorToColorForTextConverter();

		Assert.Throws<ArgumentException>(() => converter.Convert(value, typeof(Color), null, null));
	}

	[Fact]
	public void ColorToColorForTextConverterNullArgumentsTest()
	{
		var converter = new ColorToColorForTextConverter();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => converter.Convert(null, typeof(Color), null, null));
		Assert.Throws<ArgumentNullException>(() => converter.Convert(default, typeof(Color), null, null));
		Assert.Throws<ArgumentNullException>(() => converter.ConvertFrom(null));
		Assert.Throws<ArgumentNullException>(() => converter.ConvertFrom(default));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[MemberData(nameof(ColorToGrayScaleColorData))]
	public void ColorToGrayScaleColorConverterValidArgumentsTest(Color initialColor, Color expectedColor)
	{
		var converter = new ColorToGrayScaleColorConverter();

		var convertedColor = converter.Convert(initialColor, typeof(Color), null, null);
		var convertedColorFrom = converter.ConvertFrom(initialColor);

		Assert.Equal(expectedColor, convertedColor);
		Assert.Equal(expectedColor, convertedColorFrom);
	}

	[Theory]
	[InlineData(2)]
	[InlineData('c')]
	[InlineData(true)]
	public void ColorToGrayScaleColorConverterConvertInvalidArgumentsTest(object value)
	{
		var converter = new ColorToGrayScaleColorConverter();

		Assert.Throws<ArgumentException>(() => converter.Convert(value, typeof(Color), null, null));
	}

	[Fact]
	public void ColorToGrayScaleColorConverterNullInvalidArgumentsTest()
	{
		var converter = new ColorToGrayScaleColorConverter();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => converter.Convert(null, typeof(Color), null, null));
		Assert.Throws<ArgumentNullException>(() => converter.Convert(default, typeof(Color), null, null));
		Assert.Throws<ArgumentNullException>(() => converter.ConvertFrom(null));
		Assert.Throws<ArgumentNullException>(() => converter.ConvertFrom(default));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[MemberData(nameof(ColorToInverseColorConverterData))]
	public void ColorToInverseColorConverterConverterValidArgumentsTest(Color initialColor, Color expectedColor)
	{
		var converter = new ColorToInverseColorConverter();

		var convertedColor = converter.Convert(initialColor, typeof(Color), null, null);
		var convertedColorFrom = converter.ConvertFrom(initialColor);

		Assert.Equal(expectedColor, convertedColor);
		Assert.Equal(expectedColor, convertedColorFrom);
	}

	[Theory]
	[InlineData(2)]
	[InlineData('c')]
	[InlineData(true)]
	public void ColorToInverseColorConverterConvertInvalidArgumentsTest(object value)
	{
		var converter = new ColorToInverseColorConverter();

		Assert.Throws<ArgumentException>(() => converter.Convert(value, typeof(Color), null, null));
	}

	[Fact]
	public void ColorToInverseColorConverteNullArgumentsTest()
	{
		var converter = new ColorToInverseColorConverter();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => converter.Convert(null, typeof(Color), null, null));
		Assert.Throws<ArgumentNullException>(() => converter.Convert(default, typeof(Color), null, null));
		Assert.Throws<ArgumentNullException>(() => converter.ConvertFrom(null));
		Assert.Throws<ArgumentNullException>(() => converter.ConvertFrom(default));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}