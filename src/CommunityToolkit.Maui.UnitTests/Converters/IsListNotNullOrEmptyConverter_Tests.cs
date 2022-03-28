using System.Collections;
using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class IsListNotNullOrEmptyConverter_Tests : BaseTest
{
	public static IReadOnlyList<object?[]> Data { get; } = new[]
	{
		new object[] { new List<string>(), false },
		new object[] { new List<string>() { "TestValue" }, true },
		new object?[] { null, false },
		new object[] { Enumerable.Range(1, 3), true },
	};

	[Theory]
	[MemberData(nameof(Data))]
	public void IsListNotNullOrEmptyConverter(IEnumerable? value, bool expectedResult)
	{
		var listIsNotNullOrEmptyConverter = new IsListNotNullOrEmptyConverter();

		var convertResult = (bool?)((ICommunityToolkitValueConverter)listIsNotNullOrEmptyConverter).Convert(value, typeof(bool), null, CultureInfo.CurrentCulture);
		var convertFromResult = listIsNotNullOrEmptyConverter.ConvertFrom(value, typeof(bool), null, CultureInfo.CurrentCulture);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[InlineData(7)]
	[InlineData('c')]
	[InlineData(true)]
	public void InvalidConverterValuesThrowArgumentException(object? value)
	{
		var listIsNotNullOrEmptyConverter = new IsListNotNullOrEmptyConverter();

		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)listIsNotNullOrEmptyConverter).Convert(value, typeof(bool), null, CultureInfo.CurrentCulture));
	}

	[Fact]
	public void IsListNotNullOrEmptyConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new IsListNotNullOrEmptyConverter()).Convert(true, null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new IsListNotNullOrEmptyConverter()).ConvertBack(true, null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}