using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class CompareConverterTests : BaseOneWayConverterTest<CompareConverter>
{
	public const string TrueTestObject = nameof(TrueTestObject);
	public const string FalseTestObject = nameof(FalseTestObject);

	public static IReadOnlyList<object?[]> TestData { get; } =
	[
		[10d, CompareConverter.OperatorType.Greater, 20d, TrueTestObject, FalseTestObject, FalseTestObject],
		[10d, CompareConverter.OperatorType.GreaterOrEqual, 20d, TrueTestObject, FalseTestObject, FalseTestObject],
		[10d, CompareConverter.OperatorType.Equal, 20d, TrueTestObject, FalseTestObject, FalseTestObject],
		[10d, CompareConverter.OperatorType.NotEqual, 20d, TrueTestObject, FalseTestObject, TrueTestObject],
		[10d, CompareConverter.OperatorType.Smaller, 20d, TrueTestObject, FalseTestObject, TrueTestObject],
		[10d, CompareConverter.OperatorType.SmallerOrEqual, 20d, TrueTestObject, FalseTestObject, TrueTestObject],
		[20d, CompareConverter.OperatorType.Greater, 20d, TrueTestObject, FalseTestObject, FalseTestObject],
		[20d, CompareConverter.OperatorType.GreaterOrEqual, 20d, TrueTestObject, FalseTestObject, TrueTestObject],
		[20d, CompareConverter.OperatorType.Equal, 20d, TrueTestObject, FalseTestObject, TrueTestObject],
		[20d, CompareConverter.OperatorType.NotEqual, 20d, TrueTestObject, FalseTestObject, FalseTestObject],
		[20d, CompareConverter.OperatorType.Smaller, 20d, TrueTestObject, FalseTestObject, FalseTestObject],
		[20d, CompareConverter.OperatorType.SmallerOrEqual, 20d, TrueTestObject, FalseTestObject, TrueTestObject],
		[20d, CompareConverter.OperatorType.Greater, 10d, TrueTestObject, FalseTestObject, TrueTestObject],
		[20d, CompareConverter.OperatorType.GreaterOrEqual, 10d, TrueTestObject, FalseTestObject, TrueTestObject],
		[20d, CompareConverter.OperatorType.Equal, 10d, TrueTestObject, FalseTestObject, FalseTestObject],
		[20d, CompareConverter.OperatorType.NotEqual, 10d, TrueTestObject, FalseTestObject, TrueTestObject],
		[20d, CompareConverter.OperatorType.Smaller, 10d, TrueTestObject, FalseTestObject, FalseTestObject],
		[20d, CompareConverter.OperatorType.SmallerOrEqual, 10d, TrueTestObject, FalseTestObject, FalseTestObject],
		[20d, CompareConverter.OperatorType.Greater, 20d, null, null, false],
		[20d, CompareConverter.OperatorType.GreaterOrEqual, 20d, null, null, true],
		[20d, CompareConverter.OperatorType.Equal, 20d, null, null, true],
		[20d, CompareConverter.OperatorType.NotEqual, 20d, null, null, false],
		[20d, CompareConverter.OperatorType.Smaller, 20d, null, null, false],
		[20d, CompareConverter.OperatorType.SmallerOrEqual, 20d, null, null, true],
		[20d, CompareConverter.OperatorType.Greater, 10d, null, null, true],
		[20d, CompareConverter.OperatorType.GreaterOrEqual, 10d, null, null, true],
		[20d, CompareConverter.OperatorType.Equal, 10d, null, null, false],
		[20d, CompareConverter.OperatorType.NotEqual, 10d, null, null, true],
		[20d, CompareConverter.OperatorType.Smaller, 10d, null, null, false],
		[20d, CompareConverter.OperatorType.SmallerOrEqual, 10d, null, null, false],
		[10d, CompareConverter.OperatorType.Greater, 20d, null, null, false],
		[10d, CompareConverter.OperatorType.GreaterOrEqual, 20d, null, null, false],
		[10d, CompareConverter.OperatorType.Equal, 20d, null, null, false],
		[10d, CompareConverter.OperatorType.NotEqual, 20d, null, null, true],
		[10d, CompareConverter.OperatorType.Smaller, 20d, null, null, true],
		[10d, CompareConverter.OperatorType.SmallerOrEqual, 20d, null, null, true]
	];

	public static IReadOnlyList<object?[]> ThrowArgumentExceptionTestData { get; } =
	[
		[new { Name = "Not IComparable" }]
	];

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

		var convertResult = ((ICommunityToolkitValueConverter)compareConverter).Convert(value, typeof(object), null, CultureInfo.CurrentCulture);
		var convertFromResult = compareConverter.ConvertFrom(value);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[MemberData(nameof(ThrowArgumentExceptionTestData))]
	public void CompareConverterInvalidValuesThrowsArgumentException(object value)
	{
		var compareConverter = new CompareConverter()
		{
			ComparingValue = 20d
		};

		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)compareConverter).Convert(value, typeof(object), null, CultureInfo.CurrentCulture));
	}

	[Fact]
	public void CompareConverterNullValuesThrowsArgumentNullException()
	{
		var compareConverter = new CompareConverter()
		{
			ComparingValue = 20d
		};

		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)compareConverter).Convert(null, typeof(object), null, CultureInfo.CurrentCulture));
	}

	[Theory]
	[InlineData(20d, null, TrueTestObject, FalseTestObject)]
	public void CompareConverterInvalidValuesThrowArgumentNullException(IComparable value, IComparable? comparingValue, object trueObject, object falseObject)
	{
		var compareConverter = new CompareConverter()
		{
			ComparingValue = comparingValue,
			FalseObject = falseObject,
			TrueObject = trueObject
		};

		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)compareConverter).Convert(value, typeof(object), null, CultureInfo.CurrentCulture));
		Assert.Throws<ArgumentNullException>(() => compareConverter.ConvertFrom(value));
	}

	[Theory]
	[InlineData(20d, 20d, TrueTestObject, null)]
	[InlineData(20d, 20d, null, FalseTestObject)]
	public void CompareConverterInvalidValuesThrowInvalidOperationException(IComparable value, IComparable comparingValue, object? trueObject, object? falseObject)
	{
		var compareConverter = new CompareConverter()
		{
			ComparingValue = comparingValue,
			FalseObject = falseObject,
			TrueObject = trueObject
		};

		Assert.Throws<InvalidOperationException>(() => ((ICommunityToolkitValueConverter)compareConverter).Convert(value, typeof(object), null, CultureInfo.CurrentCulture));
		Assert.Throws<InvalidOperationException>(() => compareConverter.ConvertFrom(value));
	}

	[Theory]
	[InlineData(20d, (CompareConverter.OperatorType)10, 20d)]
	public void CompareConverterInvalidValuesThrowInvalidEnumArgumentException(IComparable value, CompareConverter.OperatorType comparisonOperator, IComparable comparingValue)
	{
		var compareConverter = new CompareConverter
		{
			ComparisonOperator = comparisonOperator,
			ComparingValue = comparingValue
		};

		Assert.Throws<InvalidEnumArgumentException>(() => ((ICommunityToolkitValueConverter)compareConverter).Convert(value, typeof(object), null, CultureInfo.CurrentCulture));
		Assert.Throws<InvalidEnumArgumentException>(() => compareConverter.ConvertFrom(value));
	}

	[Fact]
	public void CompareConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToPercentYellowConverter()).Convert(new object(), null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new ColorToPercentYellowConverter()).ConvertBack(new object(), null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}