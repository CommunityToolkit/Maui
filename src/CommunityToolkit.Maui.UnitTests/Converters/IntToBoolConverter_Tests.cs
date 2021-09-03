using CommunityToolkit.Maui.Converters;
using NUnit.Framework;
using System.Globalization;

namespace CommunityToolkit.Maui.UnitTests.Converters
{
    public class IntToBoolConverter_Tests
    {
        [TestCase(1, true)]
        [TestCase(0, false)]
        public void IndexToArrayConverter(int value, bool expectedResult)
        {
            var intToBoolConverter = new IntToBoolConverter();

            var result = intToBoolConverter.Convert(value, typeof(IntToBoolConverter_Tests), null, CultureInfo.CurrentCulture);

            Assert.AreEqual(result, expectedResult);
        }

        [TestCase(true, 1)]
        [TestCase(false, 0)]
        public void IndexToArrayConverterBack(bool value, int expectedResult)
        {
            var intToBoolConverter = new IntToBoolConverter();

            var result = intToBoolConverter.ConvertBack(value, typeof(IntToBoolConverter_Tests), null, CultureInfo.CurrentCulture);

            Assert.AreEqual(result, expectedResult);
        }

        [TestCase(2.5)]
        [TestCase("")]
        [TestCase(null)]
        public void InValidConverterValuesThrowArgumenException(object value)
        {
            var intToBoolConverter = new IntToBoolConverter();
            Assert.Throws<ArgumentException>(() => intToBoolConverter.Convert(value, typeof(IndexToArrayItemConverter), null, CultureInfo.CurrentCulture));
        }

        [TestCase(2.5)]
        [TestCase("")]
        [TestCase(null)]
        public void InValidConverterBackValuesThrowArgumenException(object value)
        {
            var intToBoolConverter = new IntToBoolConverter();
            Assert.Throws<ArgumentException>(() => intToBoolConverter.ConvertBack(value, typeof(IndexToArrayItemConverter), null, CultureInfo.CurrentCulture));
        }
    }
}