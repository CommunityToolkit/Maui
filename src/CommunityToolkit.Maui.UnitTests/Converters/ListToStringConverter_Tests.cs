using System;
using System.Collections.Generic;
using CommunityToolkit.Maui.Converters;
using NUnit.Framework;

namespace CommunityToolkit.Maui.UnitTests.Converters
{
    public class ListToStringConverter_Tests
	{
		public static IEnumerable<object?[]> GetData() => new List<object?[]>
		{
			new object[] { new string[] { "A", "B", "C" }, "+_+", "A+_+B+_+C" },
			new object[] { new string[] { "A", string.Empty, "C" }, ",", "A,C" },
			new object?[] { new string?[] { "A", null, "C" }, ",", "A,C" },
			new object[] { new string[] { "A" }, ":-:", "A" },
			new object[] { new string[] { }, ",", string.Empty },
			new object?[] { null, ",", string.Empty },
			new object?[] { new string[] { "A", "B", "C" }, null, "ABC" },
		};

		[TestCaseSource(nameof(GetData))]
		public void ListToStringConverter(object value, object parameter, object expectedResult)
		{
			var listToStringConverter = new ListToStringConverter();

			var result = listToStringConverter.Convert(value, null, parameter, null);

			Assert.AreEqual(result, expectedResult);
		}

		[TestCase(0)]
		public void InValidConverterValuesThrowArgumenException(object value)
		{
			var listToStringConverter = new ListToStringConverter();

			Assert.Throws<ArgumentException>(() => listToStringConverter.Convert(value, null, null, null));
		}

		[TestCase(0)]
		public void InValidConverterParametersThrowArgumenException(object parameter)
		{
			var listToStringConverter = new ListToStringConverter();

			Assert.Throws<ArgumentException>(() => listToStringConverter.Convert(new object[0], null, parameter, null));
		}
	}
}