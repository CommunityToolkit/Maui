using System.Collections;
using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class IsListNullOrEmptyConverter_Tests : BaseTest
{
	public static IReadOnlyList<object?[]> Data { get; } = new[]
	{
		new object[] { new List<string>(), true },
		new object[] { new List<string>() { "TestValue" }, false },
		new object?[] { null, true },
		new object[] { Enumerable.Range(1, 3), false },
	};

	[Theory]
	[MemberData(nameof(Data))]
	public void IsListNullOrEmptyConverter(IEnumerable? value, bool expectedResult)
	{
		var listIstNullOrEmptyConverter = new IsListNullOrEmptyConverter();

		var convertResult = (bool?)listIstNullOrEmptyConverter.Convert(value, typeof(bool), null, CultureInfo.CurrentCulture);
		var convertFromResult = listIstNullOrEmptyConverter.ConvertFrom(value);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[InlineData(0)]
	public void InvalidConverterValuesThrowArgumentException(object value)
	{
		var listIstNullOrEmptyConverter = new IsListNullOrEmptyConverter();

		Assert.Throws<ArgumentException>(() => listIstNullOrEmptyConverter.Convert(value, typeof(bool), null, CultureInfo.CurrentCulture));
	}
}