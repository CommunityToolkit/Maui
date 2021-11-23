using System;
using System.Collections.Generic;
using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class DateTimeOffsetConverter_Tests : BaseTest
{
	static readonly DateTime testDateTimeNow = DateTime.Now;
	static readonly DateTime testDateTimeLocal = new(2020, 08, 25, 13, 37, 00, DateTimeKind.Local);
	static readonly DateTime testDateTimeUtc = new(2020, 08, 25, 13, 37, 00, DateTimeKind.Utc);
	static readonly DateTime testDateTimeUnspecified = new(2020, 08, 25, 13, 37, 00);

	static readonly DateTimeOffset testDateTimeOffsetNow = new(testDateTimeNow);
	static readonly DateTimeOffset testDateTimeOffsetLocal = new(2020, 08, 25, 13, 37, 00, DateTimeOffset.Now.Offset);
	static readonly DateTimeOffset testDateTimeOffsetUtc = new(2020, 08, 25, 13, 37, 00, DateTimeOffset.UtcNow.Offset);

	public static IReadOnlyList<object[]> Data { get; } = new[]
	{
		new object[] { testDateTimeOffsetNow, testDateTimeNow },
		new object[] { DateTimeOffset.MinValue, DateTime.MinValue },
		new object[] { DateTimeOffset.MaxValue, DateTime.MaxValue },
		new object[] { testDateTimeOffsetLocal, testDateTimeLocal },
		new object[] { testDateTimeOffsetUtc, testDateTimeUtc },
		new object[] { testDateTimeOffsetUtc, testDateTimeUnspecified },
	};

	public static IReadOnlyList<object[]> DataReverse { get; } = new[]
	{
		new object[] { testDateTimeNow, testDateTimeOffsetNow },
		new object[] { DateTime.MinValue, DateTimeOffset.MinValue },
		new object[] { DateTime.MaxValue, DateTimeOffset.MaxValue },
		new object[] { testDateTimeLocal, testDateTimeOffsetLocal },
		new object[] { testDateTimeUtc, testDateTimeOffsetUtc },
		new object[] { testDateTimeUnspecified, testDateTimeOffsetUtc },
	};

	[Theory]
	[MemberData(nameof(Data))]
	public void DateTimeOffsetConverter(DateTimeOffset value, DateTime expectedResult)
	{
		var dateTimeOffsetConverter = new DateTimeOffsetConverter();

		var result = (DateTime)dateTimeOffsetConverter.Convert(value, typeof(DateTimeOffsetConverter_Tests), null, CultureInfo.CurrentCulture);

		Assert.Equal(expectedResult, result);
	}

	[Theory]
	[MemberData(nameof(DataReverse))]
	public void DateTimeOffsetConverterBack(DateTime value, DateTimeOffset expectedResult)
	{
		var dateTimeOffsetConverter = new DateTimeOffsetConverter();

		var result = (DateTimeOffset)dateTimeOffsetConverter.ConvertBack(value, typeof(DateTimeOffsetConverter_Tests), null, CultureInfo.CurrentCulture);

		Assert.Equal(expectedResult, result);
	}

	[Fact]
	public void DateTimeOffsetConverter_GivenInvalidParameters_ThrowsException()
	{
		var dateTimeOffsetConverter = new DateTimeOffsetConverter();

		Assert.Throws<ArgumentException>(() => dateTimeOffsetConverter.Convert("Not a DateTimeOffset",
			typeof(DateTimeOffsetConverter_Tests), null,
			CultureInfo.CurrentCulture));
	}

	[Fact]
	public void DateTimeOffsetConverterBack_GivenInvalidParameters_ThrowsException()
	{
		var dateTimeOffsetConverter = new DateTimeOffsetConverter();

		Assert.Throws<ArgumentException>(() => dateTimeOffsetConverter.ConvertBack("Not a DateTime",
			typeof(DateTimeOffsetConverter_Tests), null,
			CultureInfo.CurrentCulture));
	}
}