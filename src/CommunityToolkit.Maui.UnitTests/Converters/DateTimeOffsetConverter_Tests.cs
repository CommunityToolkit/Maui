using System.Globalization;
using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class DateTimeOffsetConverterTests : BaseConverterTest<DateTimeOffsetConverter>
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

		var convertResult = ((ICommunityToolkitValueConverter)dateTimeOffsetConverter).Convert(value, typeof(DateTime), null, CultureInfo.CurrentCulture);
		var convertFromResult = dateTimeOffsetConverter.ConvertFrom(value);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[MemberData(nameof(DataReverse))]
	public void DateTimeOffsetConverterBack(DateTime value, DateTimeOffset expectedResult)
	{
		var dateTimeOffsetConverter = new DateTimeOffsetConverter();

		var convertBackResult = (DateTimeOffset)(((ICommunityToolkitValueConverter)dateTimeOffsetConverter).ConvertBack(value, typeof(DateTimeOffset), null, CultureInfo.CurrentCulture) ?? throw new InvalidOperationException());
		var convertBackToResult = dateTimeOffsetConverter.ConvertBackTo(value);

		Assert.Equal(expectedResult, convertBackResult, new DateTimeOffsetComparer());
		Assert.Equal(expectedResult, convertBackToResult, new DateTimeOffsetComparer());
	}

	[Theory]
	[InlineData(5.5)]
	[InlineData('c')]
	[InlineData(true)]
	[InlineData("abc")]
	public void DateTimeOffsetConverter_GivenInvalidParameters_ThrowsException(object invalidData)
	{
		var dateTimeOffsetConverter = new DateTimeOffsetConverter();

		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)dateTimeOffsetConverter).Convert(invalidData, typeof(DateTime), null, CultureInfo.CurrentCulture));
	}

	[Theory]
	[InlineData(5.5)]
	[InlineData('c')]
	[InlineData(true)]
	[InlineData("abc")]
	public void DateTimeOffsetConverterBack_GivenInvalidParameters_ThrowsException(object invalidData)
	{
		var dateTimeOffsetConverter = new DateTimeOffsetConverter();

		Assert.Throws<ArgumentException>(() => ((ICommunityToolkitValueConverter)dateTimeOffsetConverter).ConvertBack(invalidData, typeof(DateTimeOffset), null, CultureInfo.CurrentCulture));
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
			return x.Year == y.Year && x.Month == y.Month && x.Day == y.Day && x.Hour == y.Hour && x.Minute == y.Minute && x.Second == y.Second;
		}

		public int GetHashCode(DateTimeOffset obj)
		{
			return HashCode.Combine(obj.Year, obj.Month, obj.Day, obj.Hour, obj.Minute, obj.Second);
		}
	}
}