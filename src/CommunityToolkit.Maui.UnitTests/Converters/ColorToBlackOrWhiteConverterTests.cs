using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests;

public class ColorToBlackOrWhiteConverterTests : BaseOneWayConverterTest<ColorToBlackOrWhiteConverter>
{
	public static IReadOnlyList<object[]> ColorToBlackOrWhiteData { get; } = new[]
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