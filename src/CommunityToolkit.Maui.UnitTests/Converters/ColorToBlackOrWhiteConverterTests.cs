using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ColorToBlackOrWhiteConverterTests : BaseOneWayConverterTest<ColorToBlackOrWhiteConverter>
{
	public static TheoryData<int, int> ColorToBlackOrWhiteData { get; } = new()
	{
		{
			Colors.Black.ToInt(), Colors.Black.ToInt()
		},
		{
			Colors.DarkBlue.ToInt(), Colors.Black.ToInt()
		},
		{
			Colors.DarkCyan.ToInt(), Colors.Black.ToInt()
		},
		{
			Colors.Brown.ToInt(), Colors.Black.ToInt()
		},
		{
			Colors.DarkGreen.ToInt(), Colors.Black.ToInt()
		},
		{
			Colors.DarkSlateGray.ToInt(), Colors.Black.ToInt()
		},
		{
			Colors.Transparent.ToInt(), Colors.Black.ToInt()
		},
		{
			Colors.White.ToInt(), Colors.White.ToInt()
		},
		{
			Colors.DarkSalmon.ToInt(), Colors.White.ToInt()
		},
		{
			Colors.DarkOrchid.ToInt(), Colors.White.ToInt()
		},
		{
			Colors.DarkGrey.ToInt(), Colors.White.ToInt()
		},
		{
			Colors.Yellow.ToInt(), Colors.White.ToInt()
		},
		{
			Colors.Pink.ToInt(), Colors.White.ToInt()
		},
		{
			Colors.LightBlue.ToInt(), Colors.White.ToInt()
		},
		{
			Colors.Wheat.ToInt(), Colors.White.ToInt()
		}
	};

	[Theory]
	[MemberData(nameof(ColorToBlackOrWhiteData))]
	public void ColorToBlackOrWhiteConverterValidArgumentsTest(int initialColor, int expectedColor)
	{
		var converter = new ColorToBlackOrWhiteConverter();

		var convertedColor = ((ICommunityToolkitValueConverter)converter).Convert(Color.FromInt(initialColor), typeof(Color), null, null);
		var convertedColorFrom = converter.ConvertFrom(Color.FromInt(initialColor));

		Assert.Equal(Color.FromInt(expectedColor), convertedColor);
		Assert.Equal(Color.FromInt(expectedColor), convertedColorFrom);
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