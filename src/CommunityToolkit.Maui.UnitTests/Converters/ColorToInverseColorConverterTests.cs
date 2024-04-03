using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ColorToInverseColorConverterTests : BaseOneWayConverterTest<ColorToInverseColorConverter>
{
	public static IReadOnlyList<object[]> ColorToInverseColorConverterData { get; } =
	[
		[Colors.White, Colors.Black],
		[new Color(0f, 0f, 0f), new Color(1f, 1f, 1f)],
		[new Color(0f, 0f, 1f), new Color(1f, 1f, 0f)],
		[new Color(0f, 1f, 0f), new Color(1f, 0f, 1f)],
		[new Color(0f, 1f, 1f), new Color(1f, 0f, 0f)],
		[new Color(1f, 0f, 0f), new Color(0f, 1f, 1f)],
		[new Color(1f, 0f, 1f), new Color(0f, 1f, 0f)],
		[new Color(1f, 1f, 0f), new Color(0f, 0f, 1f)],
		[new Color(1f, 1f, 1f), new Color(0f, 0f, 0f)],
		[Colors.Black, Colors.White],
	];

	[Theory]
	[MemberData(nameof(ColorToInverseColorConverterData))]
	public void ColorToInverseColorConverterConverterValidArgumentsTest(Color initialColor, Color expectedColor)
	{
		var converter = new ColorToInverseColorConverter();

		var convertedColor = ((ICommunityToolkitValueConverter)converter).Convert(initialColor, typeof(Color), null, null);
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
		Assert.Throws<ArgumentNullException>(() => converter.ConvertFrom(null));
		Assert.Throws<ArgumentNullException>(() => converter.ConvertFrom(default));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}