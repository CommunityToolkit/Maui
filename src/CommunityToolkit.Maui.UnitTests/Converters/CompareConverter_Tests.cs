using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class CompareConverter_Tests : BaseTest
{
	public const string TrueTestObject = nameof(TrueTestObject);
	public const string FalseTestObject = nameof(FalseTestObject);

	public static IReadOnlyList<object?[]> TestData { get; } = new[]
	{
		new object?[] { 10d, CompareConverter.OperatorType.Greater, 20d, TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { 10d, CompareConverter.OperatorType.GreaterOrEqual, 20d, TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { 10d, CompareConverter.OperatorType.Equal, 20d, TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { 10d, CompareConverter.OperatorType.NotEqual, 20d, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { 10d, CompareConverter.OperatorType.Smaller, 20d, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { 10d, CompareConverter.OperatorType.SmallerOrEqual, 20d, TrueTestObject, FalseTestObject, TrueTestObject },

		new object?[] { 20d, CompareConverter.OperatorType.Greater, 20d, TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { 20d, CompareConverter.OperatorType.GreaterOrEqual, 20d, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { 20d, CompareConverter.OperatorType.Equal, 20d, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { 20d, CompareConverter.OperatorType.NotEqual, 20d, TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { 20d, CompareConverter.OperatorType.Smaller, 20d, TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { 20d, CompareConverter.OperatorType.SmallerOrEqual, 20d, TrueTestObject, FalseTestObject, TrueTestObject },

		new object?[] { 20d, CompareConverter.OperatorType.Greater, 10d, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { 20d, CompareConverter.OperatorType.GreaterOrEqual, 10d, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { 20d, CompareConverter.OperatorType.Equal, 10d, TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { 20d, CompareConverter.OperatorType.NotEqual, 10d, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { 20d, CompareConverter.OperatorType.Smaller, 10d, TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { 20d, CompareConverter.OperatorType.SmallerOrEqual, 10d, TrueTestObject, FalseTestObject, FalseTestObject },


		new object?[] { 20d, CompareConverter.OperatorType.Greater, 20d, null, null, false },
		new object?[] { 20d, CompareConverter.OperatorType.GreaterOrEqual, 20d, null, null, true },
		new object?[] { 20d, CompareConverter.OperatorType.Equal, 20d, null, null, true },
		new object?[] { 20d, CompareConverter.OperatorType.NotEqual, 20d, null, null, false },
		new object?[] { 20d, CompareConverter.OperatorType.Smaller, 20d, null, null, false },
		new object?[] { 20d, CompareConverter.OperatorType.SmallerOrEqual, 20d, null, null, true },

		new object?[] { 20d, CompareConverter.OperatorType.Greater, 10d, null, null, true },
		new object?[] { 20d, CompareConverter.OperatorType.GreaterOrEqual, 10d, null, null, true },
		new object?[] { 20d, CompareConverter.OperatorType.Equal, 10d, null, null, false },
		new object?[] { 20d, CompareConverter.OperatorType.NotEqual, 10d, null, null, true },
		new object?[] { 20d, CompareConverter.OperatorType.Smaller, 10d, null, null, false },
		new object?[] { 20d, CompareConverter.OperatorType.SmallerOrEqual, 10d, null, null, false },

		new object?[] { 10d, CompareConverter.OperatorType.Greater, 20d, null, null, false },
		new object?[] { 10d, CompareConverter.OperatorType.GreaterOrEqual, 20d, null, null, false },
		new object?[] { 10d, CompareConverter.OperatorType.Equal, 20d, null, null, false },
		new object?[] { 10d, CompareConverter.OperatorType.NotEqual, 20d, null, null, true },
		new object?[] { 10d, CompareConverter.OperatorType.Smaller, 20d, null, null, true },
		new object?[] { 10d, CompareConverter.OperatorType.SmallerOrEqual, 20d, null, null, true }
	};

	public static IReadOnlyList<object?[]> ThrowArgumenExceptionTestData { get; } = new[]
	{
		new object?[] { new { Name = "Not IComparable" } },
		new object?[] { null }
	};

	[Theory]
	[MemberData(nameof(TestData))]
	public void CompareConverterConvert(IComparable value, CompareConverter.OperatorType comparisonOperator, IComparable comparingValue, object trueObject, object falseObject, object expectedResult)
	{
		var compareConverter = new CompareConverter
		{
			TrueObject = trueObject,
			FalseObject = falseObject,
			ComparisonOperator = comparisonOperator,
			ComparingValue = comparingValue
		};

		object result = compareConverter.Convert(value, typeof(bool), null, CultureInfo.CurrentCulture);
		Assert.Equal(result, expectedResult);
	}

	[Theory]
	[MemberData(nameof(ThrowArgumenExceptionTestData))]
	public void CompareConverterInValidValuesThrowArgumenException(object value)
	{
		var compareConverter = new CompareConverter()
		{
			ComparingValue = 20d
		};

		Assert.Throws<ArgumentException>(() => compareConverter.Convert(value, typeof(bool), null, CultureInfo.CurrentCulture));
	}

	[Theory]
	[InlineData(20d, null, TrueTestObject, FalseTestObject)]
	[InlineData(20d, 20d, TrueTestObject, null)]
	[InlineData(20d, 20d, null, FalseTestObject)]
	public void CompareConverterInValidValuesThrowArgumentNullException(object value, IComparable comparingValue, object trueObject, object falseObject)
	{
		var compareConverter = new CompareConverter()
		{
			ComparingValue = comparingValue,
			FalseObject = falseObject,
			TrueObject = trueObject
		};

		Assert.Throws<ArgumentNullException>(() => compareConverter.Convert(value, typeof(bool), null, CultureInfo.CurrentCulture));
	}

	[Theory]
	[InlineData(20d, (CompareConverter.OperatorType)10, 20d)]
	public void CompareConverterInValidValuesThrowArgumentOutOfRangeException(object value, CompareConverter.OperatorType comparisonOperator, IComparable comparingValue)
	{
		var compareConverter = new CompareConverter
		{
			ComparisonOperator = comparisonOperator,
			ComparingValue = comparingValue
		};

		Assert.Throws<ArgumentOutOfRangeException>(() => compareConverter.Convert(value, typeof(bool), null, CultureInfo.CurrentCulture));
	}
}