using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests;

public class ColorToColorForTextConverterTests : BaseOneWayConverterTest<ColorToColorForTextConverter>
{
	public static IReadOnlyList<object[]> ColorToColorForTextData { get; } = new[]
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

	[Theory]
	[MemberData(nameof(ColorToColorForTextData))]
	public void ColorToColorForTextConverterValidArgumentsTest(Color initialColor, Color expectedColor)
	{
		var converter = new ColorToColorForTextConverter();

		var convertedColor = ((ICommunityToolkitValueConverter)converter).Convert(initialColor, typeof(Color), null, null);
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

		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)converter).Convert(value, typeof(Color), null, null));
	}

	[Fact]
	public void ColorToColorForTextConverterNullArgumentsTest()
	{
		var converter = new ColorToColorForTextConverter();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)converter).Convert(null, typeof(Color), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)converter).Convert(default, typeof(Color), null, null));
		Assert.Throws<ArgumentNullException>(() => converter.ConvertFrom(null));
		Assert.Throws<ArgumentNullException>(() => converter.ConvertFrom(default));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}