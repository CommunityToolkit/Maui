using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests;

public class ColorToGrayScaleColorConverterTests : BaseOneWayConverterTest<ColorToGrayScaleColorConverter>
{
	public static IReadOnlyList<object[]> ColorToGrayScaleColorData { get; } = new[]
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

	[Theory]
	[MemberData(nameof(ColorToGrayScaleColorData))]
	public void ColorToGrayScaleColorConverterValidArgumentsTest(Color initialColor, Color expectedColor)
	{
		var converter = new ColorToGrayScaleColorConverter();

		var convertedColor = ((ICommunityToolkitValueConverter)converter).Convert(initialColor, typeof(Color), null, null);
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
		Assert.Throws<ArgumentNullException>(() => converter.ConvertFrom(null));
		Assert.Throws<ArgumentNullException>(() => converter.ConvertFrom(default));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}