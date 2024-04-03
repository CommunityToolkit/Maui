using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ColorToGrayScaleColorConverterTests : BaseOneWayConverterTest<ColorToGrayScaleColorConverter>
{
	public static IReadOnlyList<object[]> ColorToGrayScaleColorData { get; } =
	[
		[Colors.White, Colors.White],
		[Colors.Yellow, new Color(2f / 3f, 2f / 3f, 2f / 3f, 1)],
		[Colors.Pink, new Color(0.8496732f, 0.8496732f, 0.8496732f, 1)],
		[Colors.LightBlue, new Color(0.8091503f, 0.8091503f, 0.8091503f, 1)],
		[Colors.Wheat, new Color(0.84444445f, 0.84444445f, 0.84444445f, 1)],
		[Colors.Black, Colors.Black],
		[Colors.DarkBlue, new Color(0.18169935f, 0.18169935f, 0.18169935f, 1)],
		[Colors.DarkCyan, new Color(0.3633987f, 0.3633987f, 0.3633987f, 1)],
		[Colors.Brown, new Color(0.3254902f, 0.3254902f, 0.3254902f, 1)],
		[Colors.DarkGreen, new Color(0.13071896f, 0.13071896f, 0.13071896f, 1)],
		[Colors.DarkSlateGray, new Color(0.26797387f, 0.26797387f, 0.26797387f, 1)],
		[Colors.Transparent, Colors.Black],
		[Colors.DarkSalmon, new Color(0.66013074f, 0.66013074f, 0.66013074f, 1)],
		[Colors.DarkOrchid, new Color(0.5320262f, 0.5320262f, 0.5320262f, 1)],
		[Colors.DarkGrey, new Color(0.6627451f, 0.6627451f, 0.6627451f, 1)]
	];

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