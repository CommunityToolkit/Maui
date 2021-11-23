using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ListIsNullOrEmptyConverter_Tests : BaseTest
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
	public void ListIsNullOrEmptyConverter(object value, bool expectedResult)
	{
		var listIstNullOrEmptyConverter = new ListIsNullOrEmptyConverter();

		var result = (bool)listIstNullOrEmptyConverter.Convert(value, typeof(ListIsNullOrEmptyConverter), null, CultureInfo.CurrentCulture);

		Assert.Equal(result, expectedResult);
	}

	[Theory]
	[InlineData(0)]
	public void InValidConverterValuesThrowArgumenException(object value)
	{
		var listIstNullOrEmptyConverter = new ListIsNullOrEmptyConverter();

		Assert.Throws<ArgumentException>(() => listIstNullOrEmptyConverter.Convert(value, typeof(ListIsNullOrEmptyConverter), null, CultureInfo.CurrentCulture));
	}
}