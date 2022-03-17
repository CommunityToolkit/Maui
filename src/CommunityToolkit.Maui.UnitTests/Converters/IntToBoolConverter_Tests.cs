using System.Globalization;
using CommunityToolkit.Maui.Converters;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class IntToBoolConverter_Tests : BaseTest
{
	[Theory]
	[InlineData(1, true)]
	[InlineData(0, false)]
	public void IntToBoolConverter(int value, bool expectedResult)
	{
		var intToBoolConverter = new IntToBoolConverter();

		var result = intToBoolConverter.Convert(value, typeof(bool), null, CultureInfo.CurrentCulture);
		var typedResult = intToBoolConverter.ConvertFrom(value);

		result.Should().BeEquivalentTo(expectedResult);
		typedResult.Should().Be(expectedResult);
	}

	[Theory]
	[InlineData(true, 1)]
	[InlineData(false, 0)]
	public void IntToBoolConverterBack(bool value, int expectedResult)
	{
		var intToBoolConverter = new IntToBoolConverter();

		var result = intToBoolConverter.ConvertBack(value, typeof(bool), null, CultureInfo.CurrentCulture);
		var typedResult = intToBoolConverter.ConvertBackTo(value);

		result.Should().BeEquivalentTo(expectedResult);
		typedResult.Should().Be(expectedResult);
	}

	[Theory]
	[InlineData(2.5)]
	[InlineData("")]
	public void InvalidConverterValuesThrowArgumentException(object value)
	{
		var intToBoolConverter = new IntToBoolConverter();
		intToBoolConverter.ConvertShouldThrow<ArgumentException>(value, typeof(bool), null, CultureInfo.CurrentCulture);
	}

	[Theory]
	[InlineData(2.5)]
	[InlineData("")]
	[InlineData(null)]
	public void InvalidConverterBackValuesThrowArgumentException(object value)
	{
		var intToBoolConverter = new IntToBoolConverter();
		intToBoolConverter.ConvertBackShouldThrow<ArgumentException>(value, typeof(bool), null, CultureInfo.CurrentCulture);
	}

	[Fact]
	public void NullConverterValueThrowsArgumentNullException()
	{
		var intToBoolConverter = new IntToBoolConverter();
		intToBoolConverter.ConvertShouldThrow<ArgumentNullException>(null, typeof(bool), null, CultureInfo.CurrentCulture);
	}	
}