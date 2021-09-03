using CommunityToolkit.Maui.Converters;
using NUnit.Framework;
using System.Globalization;

namespace CommunityToolkit.Maui.UnitTests.Converters
{
    public class EqualConverter_Tests
    {
        public const string TestValue = nameof(TestValue);

        [TestCase(200, 200)]
        [TestCase(TestValue, TestValue)]
        public void IsEqual(object value, object comparedValue)
        {
            var equalConverter = new EqualConverter();

            var result = equalConverter.Convert(value, typeof(EqualConverter_Tests), comparedValue, CultureInfo.CurrentCulture);

            Assert.IsInstanceOf<bool>(result);
            Assert.IsTrue((bool)result);
        }

        [TestCase(200, 400)]
        [TestCase(TestValue, "")]
        public void IsNotEqual(object value, object comparedValue)
        {
            var equalConverter = new EqualConverter();

            var result = equalConverter.Convert(value, typeof(EqualConverter_Tests), comparedValue, CultureInfo.CurrentCulture);

            Assert.IsInstanceOf<bool>(result);
            Assert.False((bool)result);
        }
    }
}