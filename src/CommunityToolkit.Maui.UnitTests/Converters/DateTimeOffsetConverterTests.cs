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

	public static IReadOnlyList<object[]> Data { get; } =
	[
		[testDateTimeOffsetNow, testDateTimeNow],
		[DateTimeOffset.MinValue, DateTime.MinValue],
		[DateTimeOffset.MaxValue, DateTime.MaxValue],
		[testDateTimeOffsetLocal, testDateTimeLocal],
		[testDateTimeOffsetUtc, testDateTimeUtc],
		[testDateTimeOffsetUtc, testDateTimeUnspecified],
		[testDateTimeOffsetNow, testDateTimeNow, CultureInfo.CurrentCulture],
		[DateTimeOffset.MinValue, DateTime.MinValue, CultureInfo.CurrentCulture],
		[DateTimeOffset.MaxValue, DateTime.MaxValue, CultureInfo.CurrentCulture],
		[testDateTimeOffsetLocal, testDateTimeLocal, CultureInfo.CurrentCulture],
		[testDateTimeOffsetUtc, testDateTimeUtc, CultureInfo.CurrentCulture],
		[testDateTimeOffsetUtc, testDateTimeUnspecified, CultureInfo.CurrentCulture],
	];

	public static IReadOnlyList<object[]> DataReverse { get; } =
	[
		[testDateTimeNow, testDateTimeOffsetNow],
		[DateTime.MinValue, DateTimeOffset.MinValue],
		[DateTime.MaxValue, DateTimeOffset.MaxValue],
		[testDateTimeLocal, testDateTimeOffsetLocal],
		[testDateTimeUtc, testDateTimeOffsetUtc],
		[testDateTimeUnspecified, testDateTimeOffsetUtc],
		[testDateTimeNow, testDateTimeOffsetNow, CultureInfo.CurrentCulture],
		[DateTime.MinValue, DateTimeOffset.MinValue, CultureInfo.CurrentCulture],
		[DateTime.MaxValue, DateTimeOffset.MaxValue, CultureInfo.CurrentCulture],
		[testDateTimeLocal, testDateTimeOffsetLocal, CultureInfo.CurrentCulture],
		[testDateTimeUtc, testDateTimeOffsetUtc, CultureInfo.CurrentCulture],
		[testDateTimeUnspecified, testDateTimeOffsetUtc, CultureInfo.CurrentCulture],
	];

	[Theory]
	[MemberData(nameof(Data))]
	public void DateTimeOffsetConverter(DateTimeOffset value, DateTime expectedResult, CultureInfo? culture = null)
	{
		var dateTimeOffsetConverter = new DateTimeOffsetConverter();

		var convertResult = ((ICommunityToolkitValueConverter)dateTimeOffsetConverter).Convert(value, typeof(DateTime), null, culture);
		var convertFromResult = dateTimeOffsetConverter.ConvertFrom(value, culture);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[MemberData(nameof(DataReverse))]
	public void DateTimeOffsetConverterBack(DateTime value, DateTimeOffset expectedResult, CultureInfo? culture = null)
	{
		var dateTimeOffsetConverter = new DateTimeOffsetConverter();

		var convertBackResult = (DateTimeOffset)(((ICommunityToolkitValueConverter)dateTimeOffsetConverter).ConvertBack(value, typeof(DateTimeOffset), null, culture) ?? throw new InvalidOperationException());
		var convertBackToResult = dateTimeOffsetConverter.ConvertBackTo(value, culture);

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