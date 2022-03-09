using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ColorToByteAlphaConverter_Tests : BaseTest
{
	public static IReadOnlyList<object[]> ValidInputData = new[]
	{
		new object[] { float.MinValue, (byte)0 },
		new object[] { -0.01f, (byte)0 },
		new object[] { -0f, (byte)0 },
		new object[] { 0f, (byte)0 },
		new object[] { 100f, (byte)100 },
		new object[] { 255f, (byte)255 },
		new object[] { 255.01f, (byte)255 },
		new object[] { float.MaxValue, (byte)255 },
	};

	[Theory]
	[MemberData(nameof(ValidInputData))]
	public void ColorToByteAlphaConverterValidInputTest(float alpha, byte expectedResult)
	{
		var converter = new ColorToByteAlphaConverter();

		var resultConvertFrom = converter.ConvertFrom(new Color(0, 0, 0, alpha));
		var resultConvert = converter.Convert(new Color(0, 0, 0, alpha), typeof(byte), null, null);

		Assert.Equal(expectedResult, resultConvertFrom);
		Assert.Equal(expectedResult, resultConvert);
	}

	[Fact]
	public void ColorToComponentConvertersNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => new ColorToByteAlphaConverter().ConvertFrom(null));
		Assert.Throws<ArgumentNullException>(() => new ColorToByteAlphaConverter().Convert(null, typeof(byte), null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}