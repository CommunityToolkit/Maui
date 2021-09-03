using CommunityToolkit.Maui.Converters;
using NUnit.Framework;
using System.Globalization;

namespace CommunityToolkit.Maui.UnitTests.Converters
{
    public class IndexToArrayItemConverter_Tests
    {
        [TestCase(new int[] { 1, 2, 3, 4, 5 }, 2, 3)]
        [TestCase(new string[] { "Val1", "Val2", "Val3" }, 0, "Val1")]
        [TestCase(new double[] { 1.3, 4.3, 4.3 }, 1, 4.3)]
        public void IndexToArrayConverter(Array value, int position, object expectedResult)
        {
            var indexToArrayConverter = new IndexToArrayItemConverter();

            var result = indexToArrayConverter.Convert(position, typeof(IndexToArrayItemConverter_Tests), value, CultureInfo.CurrentCulture);

            Assert.AreEqual(result, expectedResult);
        }

        [TestCase(null, null)]
        [TestCase(null, 100)]
        public void IndexToArrayInValidValuesThrowArgumenException(object value, object position)
        {
            var indexToArrayConverter = new IndexToArrayItemConverter();
            Assert.Throws<ArgumentException>(() => indexToArrayConverter.Convert(position, typeof(IndexToArrayItemConverter), value, CultureInfo.CurrentCulture));
        }

        [TestCase(new int[] { 1, 2, 3, 4, 5 }, 100)]
        [TestCase(new int[] { 1, 2, 3, 4, 5 }, -1)]
        public void IndexToArrayInValidValuesThrowArgumenOutOfRangeException(object value, object position)
        {
            var indexToArrayConverter = new IndexToArrayItemConverter();
            Assert.Throws<ArgumentOutOfRangeException>(() => indexToArrayConverter.Convert(position, typeof(IndexToArrayItemConverter), value, CultureInfo.CurrentCulture));
        }
    }
}