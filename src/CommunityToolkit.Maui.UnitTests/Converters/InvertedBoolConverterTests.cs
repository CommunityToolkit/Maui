using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class InvertedBoolConverterTests : BaseConverterTest<InvertedBoolConverter>
{
	[Theory]
	[InlineData(true, false)]
	[InlineData(false, true)]
	public void InvertedBoolConverter(bool value, bool expectedResult)
	{
		var invertedBoolConverter = new InvertedBoolConverter();

		var convertResult = (bool?)((ICommunityToolkitValueConverter)invertedBoolConverter).Convert(value, typeof(bool), null, CultureInfo.CurrentCulture);
		var convertFromResult = invertedBoolConverter.ConvertFrom(value);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[InlineData(2)]
	[InlineData("")]
	[InlineData('c')]
	[InlineData(5.5)]
	public void InvalidConverterValuesThrowArgumentException(object? value)
	{
		var invertedBoolConverter = new InvertedBoolConverter();
		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)invertedBoolConverter).Convert(value, typeof(bool), null, CultureInfo.CurrentCulture));
	}

	[Fact]
	public void NullConverterValuesThrowArgumentNullException()
	{
		var invertedBoolConverter = new InvertedBoolConverter();
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)invertedBoolConverter).Convert(null, typeof(bool), null, CultureInfo.CurrentCulture));
	}

	[Theory]
	[InlineData(true, false)]
	[InlineData(false, true)]
	public void InvertedBoolConverterConvertBack(bool value, bool expectedResult)
	{
		var invertedBoolConverter = new InvertedBoolConverter();

		var convertResult = (bool?)((ICommunityToolkitValueConverter)invertedBoolConverter).ConvertBack(value, typeof(bool), null, CultureInfo.CurrentCulture);
		var convertFromResult = invertedBoolConverter.ConvertBackTo(value);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[InlineData(2)]
	[InlineData("")]
	[InlineData('c')]
	[InlineData(5.5)]
	public void ConvertBackInvalidConverterValuesThrowArgumentException(object? value)
	{
		var invertedBoolConverter = new InvertedBoolConverter();
		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)invertedBoolConverter).ConvertBack(value, typeof(bool), null, CultureInfo.CurrentCulture));
	}

	[Fact]
	public void ConvertBackNullConverterValuesThrowArgumentNullException()
	{
		var invertedBoolConverter = new InvertedBoolConverter();
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)invertedBoolConverter).ConvertBack(null, typeof(bool), null, CultureInfo.CurrentCulture));
	}

	[Fact]
	public void InvertedBoolConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new InvertedBoolConverter()).Convert(true, null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new InvertedBoolConverter()).Convert(null, typeof(bool), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new InvertedBoolConverter()).ConvertBack(true, null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new InvertedBoolConverter()).ConvertBack(null, typeof(bool), null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}