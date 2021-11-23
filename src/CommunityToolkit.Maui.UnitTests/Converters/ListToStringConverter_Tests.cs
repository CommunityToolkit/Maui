using System;
using System.Collections.Generic;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class ListToStringConverter_Tests : BaseTest
{
	public static IReadOnlyList<object?[]> TestData { get; } = new[]
	{
		new object[] { new string[] { "A", "B", "C" }, "+_+", "A+_+B+_+C" },
		new object[] { new string[] { "A", string.Empty, "C" }, ",", "A,C" },
		new object?[] { new string?[] { "A", null, "C" }, ",", "A,C" },
		new object[] { new string[] { "A" }, ":-:", "A" },
		new object[] { Array.Empty<string>(), ",", string.Empty },
		new object?[] { null, ",", string.Empty },
		new object?[] { new string[] { "A", "B", "C" }, null, "ABC" },
	};

	[Theory]
	[MemberData(nameof(TestData))]
	public void ListToStringConverter(object value, object parameter, object expectedResult)
	{
		var listToStringConverter = new ListToStringConverter();

		var result = (string)listToStringConverter.Convert(value, null, parameter, null);

		Assert.Equal(result, expectedResult);
	}

	[Theory]
	[InlineData(0)]
	public void InValidConverterValuesThrowArgumenException(object value)
	{
		var listToStringConverter = new ListToStringConverter();

		Assert.Throws<ArgumentException>(() => listToStringConverter.Convert(value, null, null, null));
	}

	[Theory]
	[InlineData(0)]
	public void InValidConverterParametersThrowArgumenException(object parameter)
	{
		var listToStringConverter = new ListToStringConverter();

		Assert.Throws<ArgumentException>(() => listToStringConverter.Convert(Array.Empty<object>(), null, parameter, null));
	}
}