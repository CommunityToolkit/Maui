using CommunityToolkit.Maui.Converters;
using Xunit;
namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ColorToByteBlueConverterTests : BaseOneWayConverterTest<ColorToByteBlueConverter>
{
	public static readonly TheoryData<float, byte> ValidInputData = new()
	{
		{
			float.MinValue, 0
		},
		{
			-0.01f, 0
		},
		{
			-0f, 0
		},
		{
			0f, 0
		},
		{
			0.25f, 64
		},
		{
			0.5f, 128
		},
		{
			0.75f, 191
		},
		{
			1f, 255
		},
		{
			1.001f, 255
		},
		{
			float.MaxValue, 255
		},
	};

	[Theory]
	[MemberData(nameof(ValidInputData))]
	public void ColorToByteBlueConverterValidInputTest(float blue, byte expectedResult)
	{
		var color = new Color(0, 0, blue, 0);
		var converter = new ColorToByteBlueConverter();

		var resultConvertFrom = converter.ConvertFrom(color);
		var resultConvert = ((ICommunityToolkitValueConverter)converter).Convert(color, typeof(byte), null, null);

		Assert.Equal(expectedResult, resultConvertFrom);
		Assert.Equal(expectedResult, resultConvert);
	}

	[Fact]
	public void ColorToByteBlueConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => new ColorToByteBlueConverter().ConvertFrom(null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToByteBlueConverter()).Convert(null, typeof(byte), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToByteBlueConverter()).Convert(new Color(), null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}