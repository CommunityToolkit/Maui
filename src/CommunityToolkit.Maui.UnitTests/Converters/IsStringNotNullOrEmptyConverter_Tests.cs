using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class IsStringNotNullOrEmptyConverterTests : BaseOneWayConverterTest<IsStringNotNullOrEmptyConverter>
{
	[Theory]
	[InlineData("Test", true)]
	[InlineData(null, false)]
	[InlineData("", false)]
	[InlineData(" ", true)]
	public void IsStringNotNullOrEmptyConverter_ValidStringValue(string? value, bool expectedResult)
	{
		var isNotNullOrEmptyConverter = new IsStringNotNullOrEmptyConverter();

		var convertResult = (bool?)((ICommunityToolkitValueConverter)isNotNullOrEmptyConverter).Convert(value, typeof(bool), null, null);
		var convertFromResult = isNotNullOrEmptyConverter.ConvertFrom(value);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[InlineData(17)]
	[InlineData(true)]
	[InlineData('c')]
	public void IsStringNotNullOrEmptyConverter_InvalidValue(object value)
	{
		var isNotNullOrEmptyConverter = new IsStringNotNullOrEmptyConverter();

		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)isNotNullOrEmptyConverter).Convert(value, typeof(bool), null, null));
	}

	[Fact]
	public void IsStringNotNullOrEmptyConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new IsStringNotNullOrEmptyConverter()).Convert(true, null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new IsStringNotNullOrEmptyConverter()).ConvertBack(true, null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}