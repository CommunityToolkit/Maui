using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ColorToColorForTextConverterTests : BaseOneWayConverterTest<ColorToColorForTextConverter>
{
	public static IReadOnlyList<object[]> ColorToColorForTextData { get; } =
	[
		[Colors.White, Colors.Black],
		[Colors.Yellow, Colors.Black],
		[Colors.Pink, Colors.Black],
		[Colors.LightBlue, Colors.Black],
		[Colors.Wheat, Colors.Black],
		[Colors.Black, Colors.White],
		[Colors.DarkBlue, Colors.White],
		[Colors.DarkCyan, Colors.White],
		[Colors.Brown, Colors.White],
		[Colors.DarkGreen, Colors.White],
		[Colors.DarkSlateGray, Colors.White],
		[Colors.Transparent, Colors.White],
		[Colors.DarkSalmon, Colors.White],
		[Colors.DarkOrchid, Colors.White],
		[Colors.DarkGrey, Colors.White]
	];

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