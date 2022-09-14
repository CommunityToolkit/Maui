using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class IsStringNullOrEmptyConverterTests : BaseOneWayConverterTest<IsStringNullOrEmptyConverter>
{
	[Theory]
	[InlineData(null, true)]
	[InlineData("", true)]
	[InlineData("Test", false)]
	[InlineData(" ", false)]
	public void IsNullOrEmptyConverter_ValidStringValue(string? value, bool expectedResult)
	{
		var isNullOrEmptyConverter = new IsStringNullOrEmptyConverter();

		var convertResult = (bool?)((ICommunityToolkitValueConverter)isNullOrEmptyConverter).Convert(value, typeof(bool), null, null);
		var convertFromResult = isNullOrEmptyConverter.ConvertFrom(value);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[InlineData(17)]
	[InlineData(true)]
	[InlineData('c')]
	public void IsNullOrEmptyConverter_InvalidValue(object value)
	{
		var isNullOrEmptyConverter = new IsStringNullOrEmptyConverter();

		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)isNullOrEmptyConverter).Convert(value, typeof(bool), null, null));
	}

	[Fact]
	public void IsStringNullOrEmptyConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new IsStringNullOrEmptyConverter()).Convert(true, null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new IsStringNullOrEmptyConverter()).ConvertBack(true, null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new IsStringNullOrEmptyConverter()).ConvertBack(null, typeof(string), null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}