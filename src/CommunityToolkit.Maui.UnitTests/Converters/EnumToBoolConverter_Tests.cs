#nullable enable
using CommunityToolkit.Maui.Converters;
using NUnit.Framework;
using System.Globalization;

namespace CommunityToolkit.Maui.UnitTests.Converters
{
    public enum TestEnumForEnumToBoolConverter
    {
        None = 0,
        One = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6
    }

    [Flags]
    public enum TestFlaggedEnumForEnumToBoolConverter
    {
        None = 0b0000,
        One = 0b0001,
        Two = 0b0010,
        Three = 0b0100,
        Four = 0b1000
    }

    public class EnumToBoolConverter_Tests
    {
        [Test]
        public void EnumToBoolConvertBack_ThrowsNotImplementedException()
        {
            var enumToBoolConverter = new EnumToBoolConverter();

            Assert.Throws<NotImplementedException>(() =>
                enumToBoolConverter.ConvertBack(TestEnumForEnumToBoolConverter.Five, typeof(bool), null, CultureInfo.InvariantCulture));
        }

        [TestCase("a string")]
        [TestCase(42)]
        [TestCase(null)]
        [TestCase(false)]
        public void EnumToBoolConvert_ValueNotEnum_ThrowsArgumentException(object value)
        {
            var enumToBoolConverter = new EnumToBoolConverter();

            Assert.Throws<ArgumentException>(() =>
                enumToBoolConverter.Convert(value, typeof(bool), TestEnumForEnumToBoolConverter.Five, CultureInfo.InvariantCulture));
        }

        [TestCase("a string")]
        [TestCase(42)]
        [TestCase(null)]
        [TestCase(false)]
        [TestCase(TestFlaggedEnumForEnumToBoolConverter.Four)]
        public void EnumToBoolConvert_ParameterNotSameEnum_ReturnsFalse(object parameter)
        {
            var enumToBoolConverter = new EnumToBoolConverter();

            var result = enumToBoolConverter.Convert(TestEnumForEnumToBoolConverter.Five, typeof(bool), parameter, CultureInfo.InvariantCulture);

            Assert.False(result as bool?);
        }

        [TestCase(null, TestEnumForEnumToBoolConverter.Five, TestEnumForEnumToBoolConverter.Five, true)]
        [TestCase(null, TestEnumForEnumToBoolConverter.Five, TestEnumForEnumToBoolConverter.Six, false)]
        [TestCase(new object?[] { TestEnumForEnumToBoolConverter.Five, TestEnumForEnumToBoolConverter.Six }, TestEnumForEnumToBoolConverter.Five, TestEnumForEnumToBoolConverter.Six, true)]
        [TestCase(new object?[] { TestEnumForEnumToBoolConverter.Five, TestEnumForEnumToBoolConverter.Six }, TestEnumForEnumToBoolConverter.Six, null, true)]
        [TestCase(new object?[] { TestEnumForEnumToBoolConverter.Five, TestEnumForEnumToBoolConverter.Six }, TestEnumForEnumToBoolConverter.One, TestEnumForEnumToBoolConverter.Five, false)]
        [TestCase(new object?[] { TestEnumForEnumToBoolConverter.Five, TestEnumForEnumToBoolConverter.Six }, TestEnumForEnumToBoolConverter.Two, null, false)]
        [TestCase(new object?[] { (TestFlaggedEnumForEnumToBoolConverter.One | TestFlaggedEnumForEnumToBoolConverter.Three), TestFlaggedEnumForEnumToBoolConverter.Two }, TestFlaggedEnumForEnumToBoolConverter.One, null, true)]
        [TestCase(new object?[] { (TestFlaggedEnumForEnumToBoolConverter.One | TestFlaggedEnumForEnumToBoolConverter.Three), TestFlaggedEnumForEnumToBoolConverter.Two }, TestFlaggedEnumForEnumToBoolConverter.Two, null, true)]
        [TestCase(new object?[] { (TestFlaggedEnumForEnumToBoolConverter.One | TestFlaggedEnumForEnumToBoolConverter.Three), TestFlaggedEnumForEnumToBoolConverter.Two }, TestFlaggedEnumForEnumToBoolConverter.Three, null, true)]
        [TestCase(new object?[] { TestFlaggedEnumForEnumToBoolConverter.One | TestFlaggedEnumForEnumToBoolConverter.Three, TestFlaggedEnumForEnumToBoolConverter.Two }, TestFlaggedEnumForEnumToBoolConverter.Four, null, false)]
        [TestCase(null, TestFlaggedEnumForEnumToBoolConverter.One, TestFlaggedEnumForEnumToBoolConverter.One | TestFlaggedEnumForEnumToBoolConverter.Three, true)]
        [TestCase(null, TestFlaggedEnumForEnumToBoolConverter.Three, TestFlaggedEnumForEnumToBoolConverter.One | TestFlaggedEnumForEnumToBoolConverter.Three, true)]
        [TestCase(null, TestFlaggedEnumForEnumToBoolConverter.Two, TestFlaggedEnumForEnumToBoolConverter.One | TestFlaggedEnumForEnumToBoolConverter.Three, false)]
        [TestCase(null, TestFlaggedEnumForEnumToBoolConverter.One | TestFlaggedEnumForEnumToBoolConverter.Three, TestFlaggedEnumForEnumToBoolConverter.One | TestFlaggedEnumForEnumToBoolConverter.Three, true)]
        [TestCase(null, TestFlaggedEnumForEnumToBoolConverter.One | TestFlaggedEnumForEnumToBoolConverter.Two | TestFlaggedEnumForEnumToBoolConverter.Three, TestFlaggedEnumForEnumToBoolConverter.One | TestFlaggedEnumForEnumToBoolConverter.Three, false)]
        public void EnumToBoolConvert_Validation(object?[]? trueValues, object? value, object parameter, bool expectedResult)
        {
            var enumToBoolConverter = new EnumToBoolConverter();
            trueValues?.OfType<Enum>().ToList().ForEach(fe => enumToBoolConverter.TrueValues.Add(fe));

            var result = enumToBoolConverter.Convert(value, typeof(bool), parameter, CultureInfo.InvariantCulture);
            Assert.AreEqual(expectedResult, result);
        }
    }
}