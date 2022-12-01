﻿using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class IsInRangeConverterTests : BaseOneWayConverterTest<IsInRangeConverter>
{
	public const string FalseTestObject = nameof(FalseTestObject);
	public const string TrueTestObject = nameof(TrueTestObject);

	public enum Days { Sun, Mon, Tue, Wed, Thu, Fri, Sat };

	public static IReadOnlyList<object?[]> TestData { get; } = new[]
	{
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
		new object?[] { new DateOnly(2022, 07, 07), new DateOnly(2022, 5, 5), new DateOnly(2022, 12, 25), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { new DateOnly(2022, 05, 05), new DateOnly(2022, 5, 5), new DateOnly(2022, 12, 25), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { new DateOnly(2022, 12, 25), new DateOnly(2022, 5, 5), new DateOnly(2022, 12, 25), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { new DateOnly(2022, 01, 02), new DateOnly(2022, 5, 5), new DateOnly(2022, 12, 25), TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { new DateOnly(2022, 12, 26), new DateOnly(2022, 5, 5), new DateOnly(2022, 12, 25), TrueTestObject, FalseTestObject, FalseTestObject },
		// System.DateTime
		new object?[] { new DateTime(2022, 07, 07), new DateTime(2022, 5, 5), new DateTime(2022, 12, 25), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { new DateTime(2022, 05, 05), new DateTime(2022, 5, 5), new DateTime(2022, 12, 25), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { new DateTime(2022, 12, 25), new DateTime(2022, 5, 5), new DateTime(2022, 12, 25), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { new DateTime(2022, 01, 02), new DateTime(2022, 5, 5), new DateTime(2022, 12, 25), TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { new DateTime(2022, 12, 26), new DateTime(2022, 5, 5), new DateTime(2022, 12, 25), TrueTestObject, FalseTestObject, FalseTestObject },
		// System.DateTimeOffset
		new object?[] { new DateTimeOffset(new DateTime(1973, 1, 1), new TimeSpan(3,0,0)), new DateTimeOffset(new DateTime(1973, 1, 1), new TimeSpan(4, 0, 0)), new DateTimeOffset(new DateTime(1973, 1, 1), new TimeSpan(2, 0, 0)), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { new DateTimeOffset(new DateTime(1973, 1, 1), new TimeSpan(4,0,0)), new DateTimeOffset(new DateTime(1973, 1, 1), new TimeSpan(4, 0, 0)), new DateTimeOffset(new DateTime(1973, 1, 1), new TimeSpan(2, 0, 0)), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { new DateTimeOffset(new DateTime(1973, 1, 1), new TimeSpan(2,0,0)), new DateTimeOffset(new DateTime(1973, 1, 1), new TimeSpan(4, 0, 0)), new DateTimeOffset(new DateTime(1973, 1, 1), new TimeSpan(2, 0, 0)), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { new DateTimeOffset(new DateTime(1973, 1, 1), new TimeSpan(1,0,0)), new DateTimeOffset(new DateTime(1973, 1, 1), new TimeSpan(4, 0, 0)), new DateTimeOffset(new DateTime(1973, 1, 1), new TimeSpan(2, 0, 0)), TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { new DateTimeOffset(new DateTime(1973, 1, 1), new TimeSpan(5,0,0)), new DateTimeOffset(new DateTime(1973, 1, 1), new TimeSpan(4, 0, 0)), new DateTimeOffset(new DateTime(1973, 1, 1), new TimeSpan(2, 0, 0)), TrueTestObject, FalseTestObject, FalseTestObject },
		// System.Decimal
		new object?[] { new decimal(037.73), new decimal(7.73), new decimal(73.37), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { new decimal(007.73), new decimal(7.73), new decimal(73.37), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { new decimal(073.37), new decimal(7.73), new decimal(73.37), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { new decimal(003.37), new decimal(7.73), new decimal(73.37), TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { new decimal(373.73), new decimal(7.73), new decimal(73.37), TrueTestObject, FalseTestObject, FalseTestObject },
		// System.Double
		new object?[] { 037.73, 7.73, 73.37, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { 007.73, 7.73, 73.37, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { 073.37, 7.73, 73.37, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { 003.37, 7.73, 73.37, TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { 373.73, 7.73, 73.37, TrueTestObject, FalseTestObject, FalseTestObject },
		// System.Guid
		new object?[] { new Guid("00000000-0000-0000-0000-00000000000C"), new Guid("00000000-0000-0000-0000-00000000000B"), new Guid("00000000-0000-0000-0000-00000000000D"), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { new Guid("00000000-0000-0000-0000-00000000000B"), new Guid("00000000-0000-0000-0000-00000000000B"), new Guid("00000000-0000-0000-0000-00000000000D"), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { new Guid("00000000-0000-0000-0000-00000000000D"), new Guid("00000000-0000-0000-0000-00000000000B"), new Guid("00000000-0000-0000-0000-00000000000D"), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { new Guid("00000000-0000-0000-0000-00000000000A"), new Guid("00000000-0000-0000-0000-00000000000B"), new Guid("00000000-0000-0000-0000-00000000000D"), TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { new Guid("00000000-0000-0000-0000-00000000000E"), new Guid("00000000-0000-0000-0000-00000000000B"), new Guid("00000000-0000-0000-0000-00000000000D"), TrueTestObject, FalseTestObject, FalseTestObject },
		// System.Enum
		new object?[] { Days.Wed, Days.Tue, Days.Thu, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { Days.Tue, Days.Tue, Days.Thu, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { Days.Thu, Days.Tue, Days.Thu, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { Days.Mon, Days.Tue, Days.Thu, TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { Days.Fri, Days.Tue, Days.Thu, TrueTestObject, FalseTestObject, FalseTestObject },
		// System.Half
		new object?[] { (Half)037, (Half)7, (Half)73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (Half)007, (Half)7, (Half)73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (Half)073, (Half)7, (Half)73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (Half)003, (Half)7, (Half)73, TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { (Half)373, (Half)7, (Half)73, TrueTestObject, FalseTestObject, FalseTestObject },
		// System.Int128
		new object?[] { (Int128)037, (Int128)7, (Int128)73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (Int128)007, (Int128)7, (Int128)73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (Int128)073, (Int128)7, (Int128)73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (Int128)003, (Int128)7, (Int128)73, TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { (Int128)373, (Int128)7, (Int128)73, TrueTestObject, FalseTestObject, FalseTestObject },
		// System.Int16
		new object?[] { (short)037, (short)7, (short)73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (short)007, (short)7, (short)73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (short)073, (short)7, (short)73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (short)003, (short)7, (short)73, TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { (short)373, (short)7, (short)73, TrueTestObject, FalseTestObject, FalseTestObject },
		// System.Int32
		new object?[] { 037, 7, 73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { 007, 7, 73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { 073, 7, 73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { 003, 7, 73, TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { 373, 7, 73, TrueTestObject, FalseTestObject, FalseTestObject },
		// System.Int64
		new object?[] { (long)037, (long)7, (long)73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (long)007, (long)7, (long)73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (long)073, (long)7, (long)73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (long)003, (long)7, (long)73, TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { (long)373, (long)7, (long)73, TrueTestObject, FalseTestObject, FalseTestObject },
		// System.IntPtr
		new object?[] { (IntPtr)037, (IntPtr)7, (IntPtr)73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (IntPtr)007, (IntPtr)7, (IntPtr)73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (IntPtr)073, (IntPtr)7, (IntPtr)73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (IntPtr)003, (IntPtr)7, (IntPtr)73, TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { (IntPtr)373, (IntPtr)7, (IntPtr)73, TrueTestObject, FalseTestObject, FalseTestObject },
		// System.SByte
		new object?[] { (sbyte)037, (sbyte)7, (sbyte)73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (sbyte)007, (sbyte)7, (sbyte)73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (sbyte)073, (sbyte)7, (sbyte)73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (sbyte)003, (sbyte)7, (sbyte)73, TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { (sbyte)83, (sbyte)7, (sbyte)73, TrueTestObject, FalseTestObject, FalseTestObject },
		// System.Single
		new object?[] { (float)37.73, (float)7.37, (float)73.37, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (float)07.37, (float)7.37, (float)73.37, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (float)73.37, (float)7.37, (float)73.37, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (float)00.37, (float)7.37, (float)73.37, TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { (float)73.73, (float)7.37, (float)73.37, TrueTestObject, FalseTestObject, FalseTestObject },
		// System.String
		new object?[] { "C", "B", "D", TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { "B", "B", "D", TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { "D", "B", "D", TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { "A", "B", "D", TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { "E", "B", "D", TrueTestObject, FalseTestObject, FalseTestObject },
		// System.Text.Rune
		new object?[] { new System.Text.Rune('C'), new System.Text.Rune('B'), new System.Text.Rune('D'), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { new System.Text.Rune('B'), new System.Text.Rune('B'), new System.Text.Rune('D'), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { new System.Text.Rune('D'), new System.Text.Rune('B'), new System.Text.Rune('D'), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { new System.Text.Rune('A'), new System.Text.Rune('B'), new System.Text.Rune('D'), TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { new System.Text.Rune('E'), new System.Text.Rune('B'), new System.Text.Rune('D'), TrueTestObject, FalseTestObject, FalseTestObject },
		// System.TimeOnly
		new object?[] { new TimeOnly(07, 37, 07), new TimeOnly(03, 37, 03), new TimeOnly(17, 37, 07), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { new TimeOnly(03, 37, 03), new TimeOnly(03, 37, 03), new TimeOnly(17, 37, 07), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { new TimeOnly(17, 37, 07), new TimeOnly(03, 37, 03), new TimeOnly(17, 37, 07), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { new TimeOnly(03, 03, 03), new TimeOnly(03, 37, 03), new TimeOnly(17, 37, 07), TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { new TimeOnly(19, 37, 03), new TimeOnly(03, 37, 03), new TimeOnly(17, 37, 07), TrueTestObject, FalseTestObject, FalseTestObject },
		// System.TimeSpan
		new object?[] { new TimeSpan(07, 37, 07), new TimeSpan(03, 37, 03), new TimeSpan(17, 37, 07), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { new TimeSpan(03, 37, 03), new TimeSpan(03, 37, 03), new TimeSpan(17, 37, 07), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { new TimeSpan(17, 37, 07), new TimeSpan(03, 37, 03), new TimeSpan(17, 37, 07), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { new TimeSpan(03, 03, 03), new TimeSpan(03, 37, 03), new TimeSpan(17, 37, 07), TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { new TimeSpan(19, 37, 03), new TimeSpan(03, 37, 03), new TimeSpan(17, 37, 07), TrueTestObject, FalseTestObject, FalseTestObject },
		// System.UInt128
		new object?[] { (UInt128)037, (UInt128)7, (UInt128)73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (UInt128)007, (UInt128)7, (UInt128)73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (UInt128)073, (UInt128)7, (UInt128)73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (UInt128)003, (UInt128)7, (UInt128)73, TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { (UInt128)373, (UInt128)7, (UInt128)73, TrueTestObject, FalseTestObject, FalseTestObject },
		// System.UInt16
		new object?[] { (ushort)037, (ushort)7, (ushort)73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (ushort)007, (ushort)7, (ushort)73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (ushort)073, (ushort)7, (ushort)73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (ushort)003, (ushort)7, (ushort)73, TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { (ushort)373, (ushort)7, (ushort)73, TrueTestObject, FalseTestObject, FalseTestObject },
		// System.UInt32
		new object?[] { (uint)037, (uint)7, (uint)73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (uint)007, (uint)7, (uint)73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (uint)073, (uint)7, (uint)73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (uint)003, (uint)7, (uint)73, TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { (uint)373, (uint)7, (uint)73, TrueTestObject, FalseTestObject, FalseTestObject },
		// System.UInt64
		new object?[] { (ulong)037, (ulong)7, (ulong)73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (ulong)007, (ulong)7, (ulong)73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (ulong)073, (ulong)7, (ulong)73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (ulong)003, (ulong)7, (ulong)73, TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { (ulong)373, (ulong)7, (ulong)73, TrueTestObject, FalseTestObject, FalseTestObject },
		// System.UIntPtr
		new object?[] { (UIntPtr)037, (UIntPtr)7, (UIntPtr)73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (UIntPtr)007, (UIntPtr)7, (UIntPtr)73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (UIntPtr)073, (UIntPtr)7, (UIntPtr)73, TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { (UIntPtr)003, (UIntPtr)7, (UIntPtr)73, TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { (UIntPtr)373, (UIntPtr)7, (UIntPtr)73, TrueTestObject, FalseTestObject, FalseTestObject },
		//System.Version
		new object?[] { new Version(07, 37, 07, 03), new Version(3, 37, 3, 7), new Version(37, 7, 3 ,37), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { new Version(03, 37, 03, 07), new Version(3, 37, 3, 7), new Version(37, 7, 3, 37), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { new Version(37, 07, 03, 37), new Version(3, 37, 3, 7), new Version(37, 7, 3, 37), TrueTestObject, FalseTestObject, TrueTestObject },
		new object?[] { new Version(03, 03, 07, 07), new Version(3, 37, 3, 7), new Version(37, 7, 3, 37), TrueTestObject, FalseTestObject, FalseTestObject },
		new object?[] { new Version(73, 73, 37, 37), new Version(3, 37, 3, 7), new Version(37, 7, 3, 37), TrueTestObject, FalseTestObject, FalseTestObject },
	};

	[Theory]
	[InlineData(20d, "A", 'B', TrueTestObject, FalseTestObject)]
	[InlineData(20d, 1d, 'B', TrueTestObject, FalseTestObject)]
	[InlineData(20d, "A", 1d, TrueTestObject, FalseTestObject)]
	public void InvalidIComparableThrowArgumentException(IComparable value, IComparable comparingMinValue, IComparable comparingMaxValue, object trueObject, object falseObject)
	{
		IsInRangeConverter isInRangeConverter = new()
		{
			MinValue = comparingMinValue,
			MaxValue = comparingMaxValue,
			FalseObject = falseObject,
			TrueObject = trueObject,
		};

		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)isInRangeConverter).Convert(value, typeof(object), null, CultureInfo.CurrentCulture));
	}

	[Theory]
	[InlineData(20d, 1d, 30d, null, FalseTestObject)]
	[InlineData(20d, 1d, 30d, TrueTestObject, null)]
	public void InvalidIComparableThrowInvalidOperationException(IComparable value, IComparable comparingMinValue, IComparable comparingMaxValue, object trueObject, object falseObject)
	{
		IsInRangeConverter isInRangeConverter = new()
		{
			MinValue = comparingMinValue,
			MaxValue = comparingMaxValue,
			FalseObject = falseObject,
			TrueObject = trueObject,
		};

		Assert.Throws<InvalidOperationException>(() => ((ICommunityToolkitValueConverter)isInRangeConverter).Convert(value, typeof(object), null, CultureInfo.CurrentCulture));
	}

	[Theory]
	[InlineData(20d, null, null, TrueTestObject, FalseTestObject)]
	public void InvalidValuesThrowArgumentException(IComparable value, IComparable comparingMinValue, IComparable comparingMaxValue, object trueObject, object falseObject)
	{
		IsInRangeConverter isInRangeConverter = new()
		{
			MinValue = comparingMinValue,
			MaxValue = comparingMaxValue,
			FalseObject = falseObject,
			TrueObject = trueObject,
		};

		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)isInRangeConverter).Convert(value, typeof(object), null, CultureInfo.CurrentCulture));
		Assert.Throws<ArgumentException>(() => isInRangeConverter.ConvertFrom(value));
	}

	[Theory]
	[MemberData(nameof(TestData))]
	public void IsInRangeConverterConvertFrom(IComparable value, IComparable comparingMinValue, IComparable comparingMaxValue, object trueObject, object falseObject, object expectedResult)
	{
		IsInRangeConverter isInRangeConverter = new()
		{
			MinValue = comparingMinValue,
			MaxValue = comparingMaxValue,
			FalseObject = falseObject,
			TrueObject = trueObject,
		};

		object? convertResult = ((ICommunityToolkitValueConverter)isInRangeConverter).Convert(value, typeof(object), null, CultureInfo.CurrentCulture);
		object convertFromResult = isInRangeConverter.ConvertFrom(value);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[InlineData(20d, null, 30d, TrueTestObject, FalseTestObject, TrueTestObject)]
	[InlineData(20d, 10d, null, TrueTestObject, FalseTestObject, TrueTestObject)]
	[InlineData(40d, null, 30d, TrueTestObject, FalseTestObject, FalseTestObject)]
	[InlineData(5d, 10d, null, TrueTestObject, FalseTestObject, FalseTestObject)]
	public void NullToMinValueIsInRangeConverterConvertFrom(IComparable value, IComparable comparingMinValue, IComparable comparingMaxValue, object trueObject, object falseObject, object expectedResult)
	{
		IsInRangeConverter isInRangeConverter = new()
		{
			MinValue = comparingMinValue,
			MaxValue = comparingMaxValue,
			FalseObject = falseObject,
			TrueObject = trueObject,
		};

		object convertFromResult = isInRangeConverter.ConvertFrom(value);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[InlineData(20d, 10d, 30d, null, null, true)]
	[InlineData(5d, 10d, 30d, null, null, false)]
	public void ReturnObjectsNullExpectBoolReturn(IComparable value, IComparable comparingMinValue, IComparable comparingMaxValue, object trueObject, object falseObject, object expectedResult)
	{
		IsInRangeConverter isInRangeConverter = new()
		{
			MinValue = comparingMinValue,
			MaxValue = comparingMaxValue,
			FalseObject = falseObject,
			TrueObject = trueObject,
		};

		object convertFromResult = isInRangeConverter.ConvertFrom(value);
		Assert.Equal(expectedResult, convertFromResult);
	}
}