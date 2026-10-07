using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ColorToColorForTextConverterTests : BaseOneWayConverterTest<ColorToColorForTextConverter>
{
	public static TheoryData<int, int> ColorToColorForTextData { get; } = new()
	{
		{
			Colors.White.ToInt(), Colors.Black.ToInt()
		},
		{
			Colors.Yellow.ToInt(), Colors.Black.ToInt()
		},
		{
			Colors.Pink.ToInt(), Colors.Black.ToInt()
		},
		{
			Colors.LightBlue.ToInt(), Colors.Black.ToInt()
		},
		{
			Colors.Wheat.ToInt(), Colors.Black.ToInt()
		},
		{
			Colors.Black.ToInt(), Colors.White.ToInt()
		},
		{
			Colors.DarkBlue.ToInt(), Colors.White.ToInt()
		},
		{
			Colors.DarkCyan.ToInt(), Colors.White.ToInt()
		},
		{
			Colors.Brown.ToInt(), Colors.White.ToInt()
		},
		{
			Colors.DarkGreen.ToInt(), Colors.White.ToInt()
		},
		{
			Colors.DarkSlateGray.ToInt(), Colors.White.ToInt()
		},
		{
			Colors.Transparent.ToInt(), Colors.White.ToInt()
		},
		{
			Colors.DarkSalmon.ToInt(), Colors.White.ToInt()
		},
		{
			Colors.DarkOrchid.ToInt(), Colors.White.ToInt()
		},
		{
			Colors.DarkGrey.ToInt(), Colors.White.ToInt()
		}
	};

	[Theory]
	[MemberData(nameof(ColorToColorForTextData))]
	public void ColorToColorForTextConverterValidArgumentsTest(int initialColor, int expectedColor)
	{
		var converter = new ColorToColorForTextConverter();

		var convertedColor = ((ICommunityToolkitValueConverter)converter).Convert(Color.FromInt(initialColor), typeof(Color), null, null);
		var convertedColorFrom = converter.ConvertFrom(Color.FromInt(initialColor));

		Assert.Equal(Color.FromInt(expectedColor), convertedColor);
		Assert.Equal(Color.FromInt(expectedColor), convertedColorFrom);
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