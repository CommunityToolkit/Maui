using CommunityToolkit.Maui.Converters;
using NUnit.Framework;
using System.Globalization;

namespace CommunityToolkit.Maui.UnitTests.Converters
{
    public class CompareConverter_Tests
    {
        public const string TrueTestObject = nameof(TrueTestObject);
        public const string FalseTestObject = nameof(FalseTestObject);


        static IEnumerable<object?[]> GetTestData()
        {
            yield return new object?[] { 10d, CompareConverter.OperatorType.Greater, 20d, TrueTestObject, FalseTestObject, FalseTestObject };
            yield return new object?[] { 10d, CompareConverter.OperatorType.GreaterOrEqual, 20d, TrueTestObject, FalseTestObject, FalseTestObject };
            yield return new object?[] { 10d, CompareConverter.OperatorType.Equal, 20d, TrueTestObject, FalseTestObject, FalseTestObject };
            yield return new object?[] { 10d, CompareConverter.OperatorType.NotEqual, 20d, TrueTestObject, FalseTestObject, TrueTestObject };
            yield return new object?[] { 10d, CompareConverter.OperatorType.Smaller, 20d, TrueTestObject, FalseTestObject, TrueTestObject };
            yield return new object?[] { 10d, CompareConverter.OperatorType.SmallerOrEqual, 20d, TrueTestObject, FalseTestObject, TrueTestObject };

            yield return new object?[] { 20d, CompareConverter.OperatorType.Greater, 20d, TrueTestObject, FalseTestObject, FalseTestObject };
            yield return new object?[] { 20d, CompareConverter.OperatorType.GreaterOrEqual, 20d, TrueTestObject, FalseTestObject, TrueTestObject };
            yield return new object?[] { 20d, CompareConverter.OperatorType.Equal, 20d, TrueTestObject, FalseTestObject, TrueTestObject };
            yield return new object?[] { 20d, CompareConverter.OperatorType.NotEqual, 20d, TrueTestObject, FalseTestObject, FalseTestObject };
            yield return new object?[] { 20d, CompareConverter.OperatorType.Smaller, 20d, TrueTestObject, FalseTestObject, FalseTestObject };
            yield return new object?[] { 20d, CompareConverter.OperatorType.SmallerOrEqual, 20d, TrueTestObject, FalseTestObject, TrueTestObject };

            yield return new object?[] { 20d, CompareConverter.OperatorType.Greater, 10d, TrueTestObject, FalseTestObject, TrueTestObject };
            yield return new object?[] { 20d, CompareConverter.OperatorType.GreaterOrEqual, 10d, TrueTestObject, FalseTestObject, TrueTestObject };
            yield return new object?[] { 20d, CompareConverter.OperatorType.Equal, 10d, TrueTestObject, FalseTestObject, FalseTestObject };
            yield return new object?[] { 20d, CompareConverter.OperatorType.NotEqual, 10d, TrueTestObject, FalseTestObject, TrueTestObject };
            yield return new object?[] { 20d, CompareConverter.OperatorType.Smaller, 10d, TrueTestObject, FalseTestObject, FalseTestObject };
            yield return new object?[] { 20d, CompareConverter.OperatorType.SmallerOrEqual, 10d, TrueTestObject, FalseTestObject, FalseTestObject };


            yield return new object?[] { 20d, CompareConverter.OperatorType.Greater, 20d, null, null, false };
            yield return new object?[] { 20d, CompareConverter.OperatorType.GreaterOrEqual, 20d, null, null, true };
            yield return new object?[] { 20d, CompareConverter.OperatorType.Equal, 20d, null, null, true };
            yield return new object?[] { 20d, CompareConverter.OperatorType.NotEqual, 20d, null, null, false };
            yield return new object?[] { 20d, CompareConverter.OperatorType.Smaller, 20d, null, null, false };
            yield return new object?[] { 20d, CompareConverter.OperatorType.SmallerOrEqual, 20d, null, null, true };

            yield return new object?[] { 20d, CompareConverter.OperatorType.Greater, 10d, null, null, true };
            yield return new object?[] { 20d, CompareConverter.OperatorType.GreaterOrEqual, 10d, null, null, true };
            yield return new object?[] { 20d, CompareConverter.OperatorType.Equal, 10d, null, null, false };
            yield return new object?[] { 20d, CompareConverter.OperatorType.NotEqual, 10d, null, null, true };
            yield return new object?[] { 20d, CompareConverter.OperatorType.Smaller, 10d, null, null, false };
            yield return new object?[] { 20d, CompareConverter.OperatorType.SmallerOrEqual, 10d, null, null, false };

            yield return new object?[] { 10d, CompareConverter.OperatorType.Greater, 20d, null, null, false };
            yield return new object?[] { 10d, CompareConverter.OperatorType.GreaterOrEqual, 20d, null, null, false };
            yield return new object?[] { 10d, CompareConverter.OperatorType.Equal, 20d, null, null, false };
            yield return new object?[] { 10d, CompareConverter.OperatorType.NotEqual, 20d, null, null, true };
            yield return new object?[] { 10d, CompareConverter.OperatorType.Smaller, 20d, null, null, true };
            yield return new object?[] { 10d, CompareConverter.OperatorType.SmallerOrEqual, 20d, null, null, true };
        }

        [TestCaseSource(nameof(GetTestData))]
        public void CompareConverterConvert(IComparable value, CompareConverter.OperatorType comparisonOperator, IComparable comparingValue, object trueObject, object falseObject, object expectedResult)
        {
            var compareConverter = new CompareConverter
            {
                TrueObject = trueObject,
                FalseObject = falseObject,
                ComparisonOperator = comparisonOperator,
                ComparingValue = comparingValue
            };

            var result = compareConverter.Convert(value, typeof(BoolToObjectConverter_Tests), null!, CultureInfo.CurrentCulture);
            Assert.AreEqual(result, expectedResult);
        }

        static IEnumerable<object?[]> GetThrowArgumenExceptionTestData()
        {
            yield return new object?[] { new { Name = "Not IComparable" } };
            yield return new object?[] { null };
        }

        [TestCaseSource(nameof(GetThrowArgumenExceptionTestData))]
        public void CompareConverterInValidValuesThrowArgumenException(object value)
        {
            var compareConverter = new CompareConverter()
            {
                ComparingValue = 20d
            };

            Assert.Throws<ArgumentException>(() => compareConverter.Convert(value, typeof(BoolToObjectConverter_Tests), null!, CultureInfo.CurrentCulture));
        }

        [TestCase(20d, null, TrueTestObject, FalseTestObject)]
        [TestCase(20d, 20d, TrueTestObject, null)]
        [TestCase(20d, 20d, null, FalseTestObject)]
        public void CompareConverterInValidValuesThrowArgumentNullException(object value, IComparable comparingValue, object trueObject, object falseObject)
        {
            var compareConverter = new CompareConverter()
            {
                ComparingValue = comparingValue,
                FalseObject = falseObject,
                TrueObject = trueObject
            };

            Assert.Throws<ArgumentNullException>(() => compareConverter.Convert(value, typeof(BoolToObjectConverter_Tests), null!, CultureInfo.CurrentCulture));
        }

        [TestCase(20d, (CompareConverter.OperatorType)10, 20d)]
        public void CompareConverterInValidValuesThrowArgumentOutOfRangeException(object value, CompareConverter.OperatorType comparisonOperator, IComparable comparingValue)
        {
            var compareConverter = new CompareConverter
            {
                ComparisonOperator = comparisonOperator,
                ComparingValue = comparingValue
            };

            Assert.Throws<ArgumentOutOfRangeException>(() => compareConverter.Convert(value, typeof(BoolToObjectConverter_Tests), null!, CultureInfo.CurrentCulture));
        }
    }
}