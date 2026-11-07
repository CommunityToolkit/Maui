using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ColorToGrayScaleColorConverterTests : BaseOneWayConverterTest<ColorToGrayScaleColorConverter>
{
	public static TheoryData<int, int> ColorToGrayScaleColorData { get; } = new()
	{
		{
			Colors.White.ToInt(), Colors.White.ToInt()
		},
		{
			Colors.Yellow.ToInt(), new Color(2f / 3f, 2f / 3f, 2f / 3f, 1).ToInt()
		},
		{
			Colors.Pink.ToInt(), new Color(0.8496732f, 0.8496732f, 0.8496732f, 1).ToInt()
		},
		{
			Colors.LightBlue.ToInt(), new Color(0.8091503f, 0.8091503f, 0.8091503f, 1).ToInt()
		},
		{
			Colors.Wheat.ToInt(), new Color(0.84444445f, 0.84444445f, 0.84444445f, 1).ToInt()
		},
		{
			Colors.Black.ToInt(), Colors.Black.ToInt()
		},
		{
			Colors.DarkBlue.ToInt(), new Color(0.18169935f, 0.18169935f, 0.18169935f, 1).ToInt()
		},
		{
			Colors.DarkCyan.ToInt(), new Color(0.3633987f, 0.3633987f, 0.3633987f, 1).ToInt()
		},
		{
			Colors.Brown.ToInt(), new Color(0.3254902f, 0.3254902f, 0.3254902f, 1).ToInt()
		},
		{
			Colors.DarkGreen.ToInt(), new Color(0.13071896f, 0.13071896f, 0.13071896f, 1).ToInt()
		},
		{
			Colors.DarkSlateGray.ToInt(), new Color(0.26797387f, 0.26797387f, 0.26797387f, 1).ToInt()
		},
		{
			Colors.Transparent.ToInt(), Colors.Black.ToInt()
		},
		{
			Colors.DarkSalmon.ToInt(), new Color(0.66013074f, 0.66013074f, 0.66013074f, 1).ToInt()
		},
		{
			Colors.DarkOrchid.ToInt(), new Color(0.5320262f, 0.5320262f, 0.5320262f, 1).ToInt()
		},
		{
			Colors.DarkGrey.ToInt(), new Color(0.6627451f, 0.6627451f, 0.6627451f, 1).ToInt()
		}
	};

	[Theory]
	[MemberData(nameof(ColorToGrayScaleColorData))]
	public void ColorToGrayScaleColorConverterValidArgumentsTest(int initialColor, int expectedColor)
	{
		var converter = new ColorToGrayScaleColorConverter();

		var convertedColor = ((ICommunityToolkitValueConverter)converter).Convert(Color.FromInt(initialColor), typeof(Color), null, null);
		var convertedColorFrom = converter.ConvertFrom(Color.FromInt(initialColor));

		Assert.Equal(Color.FromInt(expectedColor), convertedColor);
		Assert.Equal(Color.FromInt(expectedColor), convertedColorFrom);
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