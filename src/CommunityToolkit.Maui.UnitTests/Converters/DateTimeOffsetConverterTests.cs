using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class DateTimeOffsetConverterTests : BaseConverterTest<DateTimeOffsetConverter>
{
	static readonly DateTime testDateTimeNow = DateTime.Now;
	static readonly DateTime testDateTimeUtc = new(2020, 08, 25, 13, 37, 00, DateTimeKind.Utc);
	static readonly DateTime testDateTimeLocal = new(2020, 08, 26, 13, 37, 00, DateTimeKind.Local);
	static readonly DateTime testDateTimeUnspecified = new(2020, 08, 27, 13, 37, 00);

	static readonly DateTimeOffset testDateTimeOffsetNow = new(testDateTimeNow);
	static readonly DateTimeOffset testDateTimeOffsetUtc = new(2020, 08, 25, 13, 37, 00, TimeZoneInfo.Utc.GetUtcOffset(testDateTimeUtc));
	static readonly DateTimeOffset testDateTimeOffsetLocal = new(2020, 08, 26, 13, 37, 00, TimeZoneInfo.Local.GetUtcOffset(testDateTimeLocal));
	static readonly DateTimeOffset testDateTimeOffsetUnspecified = new(2020, 08, 27, 13, 37, 00, TimeSpan.Zero);

	public static TheoryData<DateTimeOffset, DateTime, CultureInfo?> Data { get; } = new()
	{
		{
			testDateTimeOffsetNow, testDateTimeNow, null
		},
		{
			DateTimeOffset.MinValue, DateTime.MinValue, null
		},
		{
			DateTimeOffset.MaxValue, DateTime.MaxValue, null
		},
		{
			testDateTimeOffsetLocal, testDateTimeLocal, null
		},
		{
			testDateTimeOffsetUtc, testDateTimeUtc, null
		},
		{
			testDateTimeOffsetUnspecified, testDateTimeUnspecified, null
		},
		{
			testDateTimeOffsetNow, testDateTimeNow, CultureInfo.InvariantCulture
		},
		{
			DateTimeOffset.MinValue, DateTime.MinValue, CultureInfo.InvariantCulture
		},
		{
			DateTimeOffset.MaxValue, DateTime.MaxValue, CultureInfo.InvariantCulture
		},
		{
			testDateTimeOffsetLocal, testDateTimeLocal, CultureInfo.InvariantCulture
		},
		{
			testDateTimeOffsetUtc, testDateTimeUtc, CultureInfo.InvariantCulture
		},
		{
			testDateTimeOffsetUnspecified, testDateTimeUnspecified, CultureInfo.InvariantCulture
		},
	};

	public static TheoryData<DateTime, DateTimeOffset, CultureInfo?> DataReverse { get; } = new()
	{
		{
			testDateTimeNow, testDateTimeOffsetNow, null
		},
		{
			DateTime.MinValue, DateTimeOffset.MinValue, null
		},
		{
			DateTime.MaxValue, DateTimeOffset.MaxValue, null
		},
		{
			testDateTimeLocal, testDateTimeOffsetLocal, null
		},
		{
			testDateTimeUtc, testDateTimeOffsetUtc, null
		},
		{
			testDateTimeUnspecified, testDateTimeOffsetUnspecified, null
		},
		{
			testDateTimeNow, testDateTimeOffsetNow, CultureInfo.InvariantCulture
		},
		{
			DateTime.MinValue, DateTimeOffset.MinValue, CultureInfo.InvariantCulture
		},
		{
			DateTime.MaxValue, DateTimeOffset.MaxValue, CultureInfo.InvariantCulture
		},
		{
			testDateTimeLocal, testDateTimeOffsetLocal, CultureInfo.InvariantCulture
		},
		{
			testDateTimeUtc, testDateTimeOffsetUtc, CultureInfo.InvariantCulture
		},
		{
			testDateTimeUnspecified, testDateTimeOffsetUnspecified, CultureInfo.InvariantCulture
		},
		{
			new(2026, 03, 15, 13, 37, 00, DateTimeKind.Local), new(2026, 03, 15, 13, 37, 00, TimeZoneInfo.Local.GetUtcOffset(new(2026, 03, 15, 13, 37, 00))), CultureInfo.InvariantCulture
		},
		{
			new(2026, 07, 15, 13, 37, 00, DateTimeKind.Local), new(2026, 07, 15, 13, 37, 00, TimeZoneInfo.Local.GetUtcOffset(new(2026, 07, 15, 13, 37, 00))), CultureInfo.InvariantCulture
		},
	};

	[Theory]
	[MemberData(nameof(Data))]
	public void DateTimeOffsetConverter(DateTimeOffset value, DateTime expectedResult, CultureInfo? culture)
	{
		var dateTimeOffsetConverter = new DateTimeOffsetConverter();

		var convertResult = ((ICommunityToolkitValueConverter)dateTimeOffsetConverter).Convert(value, typeof(DateTime), null, culture);
		var convertFromResult = dateTimeOffsetConverter.ConvertFrom(value, culture);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[MemberData(nameof(DataReverse))]
	public void DateTimeOffsetConverterBack(DateTime value, DateTimeOffset expectedResult, CultureInfo? culture)
	{
		var dateTimeOffsetConverter = new DateTimeOffsetConverter();

		var convertBackResult = (DateTimeOffset)(((ICommunityToolkitValueConverter)dateTimeOffsetConverter).ConvertBack(value, typeof(DateTimeOffset), null, culture) ?? throw new InvalidOperationException());
		var convertBackToResult = dateTimeOffsetConverter.ConvertBackTo(value, culture);

		Assert.Equal(expectedResult, convertBackResult, new DateTimeOffsetComparer());
		Assert.Equal(expectedResult, convertBackToResult, new DateTimeOffsetComparer());
	}

	[Theory]
	[InlineData("th-TH")]
	[InlineData("fa-IR")]
	[InlineData("ar-SA")]
	[InlineData("en-US")]
	public void DateTimeOffsetConverterBack_IgnoresCultureCalendar(string cultureName)
	{
		var value = new DateTime(2026, 3, 15, 13, 37, 0, DateTimeKind.Utc).AddTicks(1234567);

		var result = new DateTimeOffsetConverter().ConvertBackTo(value, new CultureInfo(cultureName));

		Assert.Equal(new DateTimeOffset(value, TimeSpan.Zero), result);
		Assert.Equal(value.Ticks, result.Ticks);
	}

	[Theory]
	[InlineData(5.5)]
	[InlineData('c')]
	[InlineData(true)]
	[InlineData("abc")]
	public void DateTimeOffsetConverter_GivenInvalidParameters_ThrowsException(object invalidData)
	{
		var dateTimeOffsetConverter = new DateTimeOffsetConverter();

		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)dateTimeOffsetConverter).Convert(invalidData, typeof(DateTime), null, CultureInfo.InvariantCulture));
	}

	[Theory]
	[InlineData(5.5)]
	[InlineData('c')]
	[InlineData(true)]
	[InlineData("abc")]
	public void DateTimeOffsetConverterBack_GivenInvalidParameters_ThrowsException(object invalidData)
	{
		var dateTimeOffsetConverter = new DateTimeOffsetConverter();

		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)dateTimeOffsetConverter).ConvertBack(invalidData, typeof(DateTimeOffset), null, CultureInfo.InvariantCulture));
	}

	[Fact]
	public void DateTimeOffsetConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new DateTimeOffsetConverter()).Convert(DateTimeOffset.UtcNow, null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new DateTimeOffsetConverter()).ConvertBack(DateTimeOffset.UtcNow, null, null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	class DateTimeOffsetComparer : IEqualityComparer<DateTimeOffset>
	{
		public bool Equals(DateTimeOffset x, DateTimeOffset y)
		{
			return x.Ticks == y.Ticks && x.Offset == y.Offset;
		}

		public int GetHashCode(DateTimeOffset obj)
		{
			return HashCode.Combine(
				obj.Year,
				obj.Month,
				obj.Day,
				obj.Hour,
				obj.Minute,
				obj.Second,
				obj.Millisecond,
				HashCode.Combine(obj.Microsecond, obj.Offset));
		}
	}
}