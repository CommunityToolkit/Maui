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
	public void IsListNotNullOrEmptyConverter(object value, bool expectedResult)
	{
		var listIsNotNullOrEmptyConverter = new IsListNotNullOrEmptyConverter();

		var result = (bool)listIsNotNullOrEmptyConverter.Convert(value, typeof(IsListNotNullOrEmptyConverter), null, CultureInfo.CurrentCulture);

		Assert.Equal(expectedResult, result);
	}

	[Theory]
	[InlineData(0)]
	public void InvalidConverterValuesThrowArgumentException(object value)
	{
		var listIsNotNullOrEmptyConverter = new IsListNotNullOrEmptyConverter();

		Assert.Throws<ArgumentException>(() => listIsNotNullOrEmptyConverter.Convert(value, typeof(IsListNotNullOrEmptyConverter), null, CultureInfo.CurrentCulture));
	}
}