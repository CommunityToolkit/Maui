using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ColorToColorConverter_Tests : BaseTest
{
	public static IReadOnlyList<Color[]> ColorToBlackOrWhiteData { get; } = new[]
	{
		new[] { Colors.Black, Colors.Black },
		new[] { Colors.DarkBlue, Colors.Black },
		new[] { Colors.DarkCyan, Colors.Black },
		new[] { Colors.Brown, Colors.Black },
		new[] { Colors.DarkGreen, Colors.Black },
		new[] { Colors.DarkSlateGray, Colors.Black },
		new[] { Colors.Transparent, Colors.Black},
		new[] { Colors.White, Colors.White},
		new[] { Colors.DarkSalmon, Colors.White },
		new[] { Colors.DarkOrchid, Colors.White },
		new[] { Colors.DarkGrey, Colors.White },
		new[] { Colors.Yellow, Colors.White },
		new[] { Colors.Pink, Colors.White },
		new[] { Colors.LightBlue, Colors.White },
		new[] { Colors.Wheat, Colors.White }
	};

	public static IReadOnlyList<Color[]> ColorToColorForTextData { get; } = new[]
	{
		new[] { Colors.White, Colors.Black},
		new[] { Colors.Yellow, Colors.Black },
		new[] { Colors.Pink, Colors.Black },
		new[] { Colors.LightBlue, Colors.Black },
		new[] { Colors.Wheat, Colors.Black },
		new[] { Colors.Black, Colors.White },
		new[] { Colors.DarkBlue, Colors.White },
		new[] { Colors.DarkCyan, Colors.White },
		new[] { Colors.Brown, Colors.White },
		new[] { Colors.DarkGreen, Colors.White },
		new[] { Colors.DarkSlateGray, Colors.White },
		new[] { Colors.Transparent, Colors.White},
		new[] { Colors.DarkSalmon, Colors.White },
		new[] { Colors.DarkOrchid, Colors.White },
		new[] { Colors.DarkGrey, Colors.White }
	};

	public static IReadOnlyList<Color[]> ColorToGrayScaleColorData { get; } = new[]
	{
		new[] { Colors.White, Colors.White},
		new[] { Colors.Yellow, new Color(2f/3f, 2f/3f, 2f/3f, 1) },
		new[] { Colors.Pink, new Color(0.8496732f, 0.8496732f, 0.8496732f, 1) },
		new[] { Colors.LightBlue, new Color(0.8091503f, 0.8091503f, 0.8091503f, 1) },
		new[] { Colors.Wheat, new Color(0.84444445f, 0.84444445f, 0.84444445f, 1) },
		new[] { Colors.Black, Colors.Black },
		new[] { Colors.DarkBlue, new Color(0.18169935f, 0.18169935f, 0.18169935f, 1) },
		new[] { Colors.DarkCyan, new Color(0.3633987f, 0.3633987f, 0.3633987f, 1) },
		new[] { Colors.Brown, new Color(0.3254902f, 0.3254902f, 0.3254902f, 1) },
		new[] { Colors.DarkGreen, new Color(0.13071896f, 0.13071896f, 0.13071896f, 1) },
		new[] { Colors.DarkSlateGray, new Color(0.26797387f, 0.26797387f, 0.26797387f, 1) },
		new[] { Colors.Transparent, Colors.Black},
		new[] { Colors.DarkSalmon, new Color(0.66013074f, 0.66013074f, 0.66013074f, 1) },
		new[] { Colors.DarkOrchid, new Color(0.5320262f, 0.5320262f, 0.5320262f, 1) },
		new[] { Colors.DarkGrey, new Color(0.6627451f, 0.6627451f, 0.6627451f, 1) }
	};

	public static IReadOnlyList<Color[]> ColorToInverseColorConverterData { get; } = new[]
	{
		new[] { Colors.White, Colors.Black},
		new[] { new Color(0f,0f,0f), new Color(1f,1f,1f) },
		new[] { new Color(0f,0f,1f), new Color(1f,1f,0f) },
		new[] { new Color(0f,1f,0f), new Color(1f,0f,1f) },
		new[] { new Color(0f,1f,1f), new Color(1f,0f,0f) },
		new[] { new Color(1f,0f,0f), new Color(0f,1f,1f) },
		new[] { new Color(1f,0f,1f), new Color(0f,1f,0f) },
		new[] { new Color(1f,1f,0f), new Color(0f,0f,1f) },
		new[] { new Color(1f,1f,1f), new Color(0f,0f,0f) },
		new[] { Colors.Black, Colors.White },
	};

	[Theory]
	[MemberData(nameof(ColorToBlackOrWhiteData))]
	public void ColorToBlackOrWhiteConverterValidArgumentsTest(Color initialColor, Color expectedColor)
	{
		var converter = new ColorToBlackOrWhiteConverter();

		var convertedColor = ((ICommunityToolkitValueConverter)converter).Convert(initialColor, typeof(Color), null, null);
		var convertedColorFrom = converter.ConvertFrom(initialColor, typeof(Color), null, null);

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

		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)converter).Convert(value, typeof(Color), null, null));
	}

	[Fact]
	public void ColorToBlackOrWhiteConverterNullArgumentsTest()
	{
		var converter = new ColorToBlackOrWhiteConverter();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)converter).Convert(null, typeof(Color), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)converter).Convert(default, typeof(Color), null, null));
		Assert.Throws<ArgumentNullException>(() => converter.ConvertFrom(null, typeof(Color), null, null));
		Assert.Throws<ArgumentNullException>(() => converter.ConvertFrom(default, typeof(Color), null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[MemberData(nameof(ColorToColorForTextData))]
	public void ColorToColorForTextConverterValidArgumentsTest(Color initialColor, Color expectedColor)
	{
		var converter = new ColorToColorForTextConverter();

		var convertedColor = ((ICommunityToolkitValueConverter)converter).Convert(initialColor, typeof(Color), null, null);
		var convertedColorFrom = converter.ConvertFrom(initialColor, typeof(Color), null, null);

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

		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)converter).Convert(value, typeof(Color), null, null));
	}

	[Fact]
	public void ColorToColorForTextConverterNullArgumentsTest()
	{
		var converter = new ColorToColorForTextConverter();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)converter).Convert(null, typeof(Color), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)converter).Convert(default, typeof(Color), null, null));
		Assert.Throws<ArgumentNullException>(() => converter.ConvertFrom(null, typeof(Color), null, null));
		Assert.Throws<ArgumentNullException>(() => converter.ConvertFrom(default, typeof(Color), null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[MemberData(nameof(ColorToGrayScaleColorData))]
	public void ColorToGrayScaleColorConverterValidArgumentsTest(Color initialColor, Color expectedColor)
	{
		var converter = new ColorToGrayScaleColorConverter();

		var convertedColor = ((ICommunityToolkitValueConverter)converter).Convert(initialColor, typeof(Color), null, null);
		var convertedColorFrom = converter.ConvertFrom(initialColor, typeof(Color), null, null);

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

		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)converter).Convert(value, typeof(Color), null, null));
	}

	[Fact]
	public void ColorToGrayScaleColorConverterNullInvalidArgumentsTest()
	{
		var converter = new ColorToGrayScaleColorConverter();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)converter).Convert(null, typeof(Color), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)converter).Convert(default, typeof(Color), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)converter).Convert(new Color(), null, null, null));
		Assert.Throws<ArgumentNullException>(() => converter.ConvertFrom(null, typeof(Color), null, null));
		Assert.Throws<ArgumentNullException>(() => converter.ConvertFrom(default, typeof(Color), null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Theory]
	[MemberData(nameof(ColorToInverseColorConverterData))]
	public void ColorToInverseColorConverterConverterValidArgumentsTest(Color initialColor, Color expectedColor)
	{
		var converter = new ColorToInverseColorConverter();

		var convertedColor = ((ICommunityToolkitValueConverter)converter).Convert(initialColor, typeof(Color), null, null);
		var convertedColorFrom = converter.ConvertFrom(initialColor, typeof(Color), null, null);

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

		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)converter).Convert(value, typeof(Color), null, null));
	}

	[Fact]
	public void ColorToInverseColorConverteNullArgumentsTest()
	{
		var converter = new ColorToInverseColorConverter();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)converter).Convert(null, typeof(Color), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)converter).Convert(default, typeof(Color), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)converter).Convert(new Color(), null, null, null));
		Assert.Throws<ArgumentNullException>(() => converter.ConvertFrom(null, typeof(Color), null, null));
		Assert.Throws<ArgumentNullException>(() => converter.ConvertFrom(default, typeof(Color), null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}