using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class IsStringNotNullOrWhiteSpaceConverterTests : BaseOneWayConverterTest<IsStringNotNullOrWhiteSpaceConverter>
{
	[Theory]
	[InlineData("Test", true)]
	[InlineData(null, false)]
	[InlineData("", false)]
	[InlineData(" ", false)]
	[InlineData("         ", false)]
	public void IsNotNullOrWhiteSpaceConverter_ValidStringValue(string? value, bool expectedResult)
	{
		var isNotNullOrWhiteSpaceConverter = new IsStringNotNullOrWhiteSpaceConverter();

		var convertResult = (bool?)((ICommunityToolkitValueConverter)isNotNullOrWhiteSpaceConverter).Convert(value, typeof(bool), null, null);
		var convertFromResult = isNotNullOrWhiteSpaceConverter.ConvertFrom(value);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[InlineData(17)]
	[InlineData(true)]
	[InlineData('c')]
	public void IsNotNullOrWhiteSpaceConverter_InvalidValue(object value)
	{
		var isNotNullOrWhiteSpaceConverter = new IsStringNotNullOrWhiteSpaceConverter();

		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)isNotNullOrWhiteSpaceConverter).Convert(value, typeof(bool), null, null));
	}

	[Fact]
	public void IsStringNotNullOrWhiteSpaceConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new IsStringNotNullOrWhiteSpaceConverter()).Convert(string.Empty, null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new IsStringNotNullOrWhiteSpaceConverter()).ConvertBack(true, null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new IsStringNotNullOrWhiteSpaceConverter()).ConvertBack(null, typeof(string), null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}