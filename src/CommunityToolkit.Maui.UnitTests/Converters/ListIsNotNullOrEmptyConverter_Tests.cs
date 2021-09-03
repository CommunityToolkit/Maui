#nullable enable
using CommunityToolkit.Maui.Converters;
using NUnit.Framework;
using System.Globalization;

namespace CommunityToolkit.Maui.UnitTests.Converters
{
    public class ListIsNotNullOrEmptyConverter_Tests
    {
        public static IEnumerable<object?[]> GetData() => new List<object?[]>
        {
            new object[] { new List<string>(), false },
            new object[] { new List<string>() { "TestValue" }, true },
            new object?[] { null, false },
            new object[] { Enumerable.Range(1, 3), true },
        };

        [TestCaseSource(nameof(GetData))]
        public void ListIsNotNullOrEmptyConverter(object value, bool expectedResult)
        {
            var listIsNotNullOrEmptyConverter = new ListIsNotNullOrEmptyConverter();

            var result = listIsNotNullOrEmptyConverter.Convert(value, typeof(ListIsNotNullOrEmptyConverter), null, CultureInfo.CurrentCulture);

            Assert.AreEqual(result, expectedResult);
        }

        [TestCase(0)]
        public void InValidConverterValuesThrowArgumenException(object value)
        {
            var listIsNotNullOrEmptyConverter = new ListIsNotNullOrEmptyConverter();

            Assert.Throws<ArgumentException>(() => listIsNotNullOrEmptyConverter.Convert(value, typeof(ListIsNotNullOrEmptyConverter), null, CultureInfo.CurrentCulture));
        }
    }
}