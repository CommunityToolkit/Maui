using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ColorToBlackOrWhiteConverterTests : BaseOneWayConverterTest<ColorToBlackOrWhiteConverter>
{
	public static IReadOnlyList<object[]> ColorToBlackOrWhiteData { get; } =
	[
		[Colors.Black, Colors.Black],
		[Colors.DarkBlue, Colors.Black],
		[Colors.DarkCyan, Colors.Black],
		[Colors.Brown, Colors.Black],
		[Colors.DarkGreen, Colors.Black],
		[Colors.DarkSlateGray, Colors.Black],
		[Colors.Transparent, Colors.Black],
		[Colors.White, Colors.White],
		[Colors.DarkSalmon, Colors.White],
		[Colors.DarkOrchid, Colors.White],
		[Colors.DarkGrey, Colors.White],
		[Colors.Yellow, Colors.White],
		[Colors.Pink, Colors.White],
		[Colors.LightBlue, Colors.White],
		[Colors.Wheat, Colors.White]
	];

	[Theory]
	[MemberData(nameof(ColorToBlackOrWhiteData))]
	public void ColorToBlackOrWhiteConverterValidArgumentsTest(Color initialColor, Color expectedColor)
	{
		var converter = new ColorToBlackOrWhiteConverter();

		var convertedColor = ((ICommunityToolkitValueConverter)converter).Convert(initialColor, typeof(Color), null, null);
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

		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)converter).Convert(value, typeof(Color), null, null));
	}

	[Fact]
	public void ColorToBlackOrWhiteConverterNullArgumentsTest()
	{
		var converter = new ColorToBlackOrWhiteConverter();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)converter).Convert(null, typeof(Color), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)converter).Convert(default, typeof(Color), null, null));
		Assert.Throws<ArgumentNullException>(() => converter.ConvertFrom(null));
		Assert.Throws<ArgumentNullException>(() => converter.ConvertFrom(default));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}