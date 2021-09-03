#nullable enable
using CommunityToolkit.Maui.Converters;
using NUnit.Framework;
using System.Globalization;

namespace CommunityToolkit.Maui.UnitTests.Converters
{
    public class ListIsNullOrEmptyConverter_Tests
    {
        public static IEnumerable<object?[]> GetData() => new List<object?[]>
        {
            new object[] { new List<string>(), true },
            new object[] { new List<string>() { "TestValue" }, false },
            new object?[] { null, true },
            new object[] { Enumerable.Range(1, 3), false },
        };

        [TestCaseSource(nameof(GetData))]
        public void ListIsNullOrEmptyConverter(object value, bool expectedResult)
        {
            var listIstNullOrEmptyConverter = new ListIsNullOrEmptyConverter();

            var result = listIstNullOrEmptyConverter.Convert(value, typeof(ListIsNullOrEmptyConverter), null, CultureInfo.CurrentCulture);

            Assert.AreEqual(result, expectedResult);
        }

        [TestCase(0)]
        public void InValidConverterValuesThrowArgumenException(object value)
        {
            var listIstNullOrEmptyConverter = new ListIsNullOrEmptyConverter();

            Assert.Throws<ArgumentException>(() => listIstNullOrEmptyConverter.Convert(value, typeof(ListIsNullOrEmptyConverter), null, CultureInfo.CurrentCulture));
        }
    }
}