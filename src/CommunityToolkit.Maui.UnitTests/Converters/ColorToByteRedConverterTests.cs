using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ColorToByteRedConverterTests : BaseOneWayConverterTest<ColorToByteRedConverter>
{
	public static readonly IReadOnlyList<object[]> ValidInputData =
	[
		[float.MinValue, (byte)0],
		[-0.01f, (byte)0],
		[-0f, (byte)0],
		[0f, (byte)0],
		[0.25f, (byte)64],
		[0.5f, (byte)128],
		[0.75f, (byte)191],
		[1f, (byte)255],
		[1.001f, (byte)255],
		[float.MaxValue, (byte)255],
	];

	[Theory]
	[MemberData(nameof(ValidInputData))]
	public void ColorToByteRedConverterValidInputTest(float red, byte expectedResult)
	{
		var converter = new ColorToByteRedConverter();
		var color = new Color(red, 0, 0, 0);

		var resultConvertFrom = converter.ConvertFrom(color);
		var resultConvert = ((ICommunityToolkitValueConverter)converter).Convert(color, typeof(byte), null, null);

		Assert.Equal(expectedResult, resultConvertFrom);
		Assert.Equal(expectedResult, resultConvert);
	}

	[Fact]
	public void ColorToByteRedConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => new ColorToByteRedConverter().ConvertFrom(null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToByteRedConverter()).Convert(null, typeof(byte), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToByteRedConverter()).Convert(new Color(), null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}