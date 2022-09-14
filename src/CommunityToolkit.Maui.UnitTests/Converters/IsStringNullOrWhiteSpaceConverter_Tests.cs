using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class IsStringNullOrWhiteSpaceConverterTests : BaseOneWayConverterTest<IsStringNullOrWhiteSpaceConverter>
{
	[Theory]
	[InlineData(null, true)]
	[InlineData("", true)]
	[InlineData("Test", false)]
	[InlineData(" ", true)]
	[InlineData("         ", true)]
	public void IsNullOrWhiteSpaceConverter_ValidStringValue(string? value, bool expectedResult)
	{
		var isNullOrWhiteSpaceConverter = new IsStringNullOrWhiteSpaceConverter();

		var convertResult = (bool?)((ICommunityToolkitValueConverter)isNullOrWhiteSpaceConverter).Convert(value, typeof(bool), null, null);
		var convertFromResult = isNullOrWhiteSpaceConverter.ConvertFrom(value);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[InlineData(17)]
	[InlineData(true)]
	[InlineData('c')]
	public void IsNullOrWhiteSpaceConverter_InvalidValue(object value)
	{
		var isNullOrWhiteSpaceConverter = new IsStringNullOrWhiteSpaceConverter();

		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)isNullOrWhiteSpaceConverter).Convert(value, typeof(bool), null, null));
	}

	[Fact]
	public void IsStringNullOrWhiteSpaceConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new IsStringNullOrWhiteSpaceConverter()).Convert(true, null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new IsStringNullOrWhiteSpaceConverter()).ConvertBack(true, null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new IsStringNullOrWhiteSpaceConverter()).ConvertBack(null, typeof(string), null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}