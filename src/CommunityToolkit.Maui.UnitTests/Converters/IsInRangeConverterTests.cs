using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class IsInRangeConverterTests : BaseOneWayConverterTest<IsInRangeConverter>
{
	public const string TrueTestObject = nameof(TrueTestObject);
	public const string FalseTestObject = nameof(FalseTestObject);

	public static IReadOnlyList<object?[]> TestData { get; } = new[]
	{
		// String
		new object?[] { "C", "B", "D", TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { "B", "B", "D", TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { "D", "B", "D", TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { "A", "B", "D", TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { "E", "B", "D", TrueTestObject, FalseTestObject, FalseTestObject },
		// System.Byte
		new object?[] { Convert.ToByte('C'), Convert.ToByte('B'), Convert.ToByte('D'), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { Convert.ToByte('B'), Convert.ToByte('B'), Convert.ToByte('D'), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { Convert.ToByte('D'), Convert.ToByte('B'), Convert.ToByte('D'), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { Convert.ToByte('A'), Convert.ToByte('B'), Convert.ToByte('D'), TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { Convert.ToByte('E'), Convert.ToByte('B'), Convert.ToByte('D'), TrueTestObject, FalseTestObject, FalseTestObject },
		// System.Char
		new object?[] { Convert.ToChar("C"), Convert.ToChar("B"), Convert.ToChar("D"), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { Convert.ToChar("B"), Convert.ToChar("B"), Convert.ToChar("D"), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { Convert.ToChar("D"), Convert.ToChar("B"), Convert.ToChar("D"), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { Convert.ToChar("A"), Convert.ToChar("B"), Convert.ToChar("D"), TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { Convert.ToChar("E"), Convert.ToChar("B"), Convert.ToChar("D"), TrueTestObject, FalseTestObject, FalseTestObject },
		// System.DateOnly
		new object?[] { new DateOnly(2022, 7, 7), new DateOnly(2022, 5, 5), new DateOnly(2022, 12, 25), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { new DateOnly(2022, 5, 5), new DateOnly(2022, 5, 5), new DateOnly(2022, 12, 25), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { new DateOnly(2022, 12, 25), new DateOnly(2022, 5, 5), new DateOnly(2022, 12, 25), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { new DateOnly(2022, 1, 2), new DateOnly(2022, 5, 5), new DateOnly(2022, 12, 25), TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { new DateOnly(2022, 12, 26), new DateOnly(2022, 5, 5), new DateOnly(2022, 12, 25), TrueTestObject, FalseTestObject, FalseTestObject },
		// System.DateTime
		new object?[] { new DateTime(2022, 7, 7), new DateTime(2022, 5, 5), new DateTime(2022, 12, 25), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { new DateTime(2022, 5, 5), new DateTime(2022, 5, 5), new DateTime(2022, 12, 25), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { new DateTime(2022, 12, 25), new DateTime(2022, 5, 5), new DateTime(2022, 12, 25), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { new DateTime(2022, 1, 2), new DateTime(2022, 5, 5), new DateTime(2022, 12, 25), TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { new DateTime(2022, 12, 26), new DateTime(2022, 5, 5), new DateTime(2022, 12, 25), TrueTestObject, FalseTestObject, FalseTestObject },

/*
System.DateTimeOffset
System.Decimal
System.Double
System.Enum
System.Guid
System.Half
System.Int128
System.Int16
System.Int32
System.Int64
System.IntPtr
System.Numerics.BigInteger
System.Numerics.IBinaryFloatingPointIeee754<TSelf>
System.Numerics.IBinaryInteger<TSelf>
System.Numerics.IBinaryNumber<TSelf>
System.Numerics.IFloatingPoint<TSelf>
System.Numerics.IFloatingPointIeee754<TSelf>
System.Numerics.INumber<TSelf>
System.Runtime.InteropServices.NFloat
System.SByte
System.Single
System.String
System.Text.Rune
System.TimeOnly
System.TimeSpan
System.UInt128
System.UInt16
System.UInt32
System.UInt64
System.UIntPtr
System.Version
System.Windows.Automation.AutomationIdentifier
System.Workflow.Activities.EventQueueName
*/
	};

	[Theory]
	[InlineData(20d, null, null, TrueTestObject, FalseTestObject)]
	public void InvalidValuesThrowArgumentNullException(IComparable value, IComparable comparingMinValue, IComparable comparingMaxValue, object trueObject, object falseObject)
	{
		var isInReangeConverter = new IsInRangeConverter()
		{
			MinValue = comparingMinValue,
			MaxValue = comparingMaxValue,
			FalseObject = falseObject,
			TrueObject = trueObject,
		};

		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)isInReangeConverter).Convert(value, typeof(object), null, CultureInfo.CurrentCulture));
		Assert.Throws<ArgumentNullException>(() => isInReangeConverter.ConvertFrom(value));
	}

	[Theory]
	[MemberData(nameof(TestData))]
	public void IsInRangeConverterConvertFrom(IComparable value, IComparable comparingMinValue, IComparable comparingMaxValue, object trueObject, object falseObject, object expectedResult)
	{
		var isInReangeConverter = new IsInRangeConverter()
		{
			MinValue = comparingMinValue,
			MaxValue = comparingMaxValue,
			FalseObject = falseObject,
			TrueObject = trueObject,
		};

		var convertResult = ((ICommunityToolkitValueConverter)isInReangeConverter).Convert(value, typeof(object), null, CultureInfo.CurrentCulture);
		var convertFromResult = isInReangeConverter.ConvertFrom(value);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[InlineData(20d, "A", 'B', TrueTestObject, FalseTestObject)]
	[InlineData(20d, 1d, 'B', TrueTestObject, FalseTestObject)]
	[InlineData(20d, "A", 1d, TrueTestObject, FalseTestObject)]
	public void InvalidIComparableThrowArgumentOutOfRangeException(IComparable value, IComparable comparingMinValue, IComparable comparingMaxValue, object trueObject, object falseObject)
	{
		var isInReangeConverter = new IsInRangeConverter()
		{
			MinValue = comparingMinValue,
			MaxValue = comparingMaxValue,
			FalseObject = falseObject,
			TrueObject = trueObject,
		};

		Assert.Throws<ArgumentOutOfRangeException>(() => ((ICommunityToolkitValueConverter)isInReangeConverter).Convert(value, typeof(object), null, CultureInfo.CurrentCulture));
	}

	[Theory]
	[InlineData(20d, 1d, 30d, null, FalseTestObject)]
	[InlineData(20d, 1d, 30d, TrueTestObject, null)]
	public void InvalidIComparableThrowInvalidOperationException(IComparable value, IComparable comparingMinValue, IComparable comparingMaxValue, object trueObject, object falseObject)
	{
		var isInReangeConverter = new IsInRangeConverter()
		{
			MinValue = comparingMinValue,
			MaxValue = comparingMaxValue,
			FalseObject = falseObject,
			TrueObject = trueObject,
		};

		Assert.Throws<InvalidOperationException>(() => ((ICommunityToolkitValueConverter)isInReangeConverter).Convert(value, typeof(object), null, CultureInfo.CurrentCulture));
	}
}