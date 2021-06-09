using System;
using System.Collections.Generic;
using System.Globalization;
using CommunityToolkit.Maui.Converters;
using NUnit.Framework;

namespace CommunityToolkit.Maui.UnitTests.Converters
{
    public class DateTimeOffsetConverter_Tests
	{
		static readonly DateTime testDateTimeNow = DateTime.Now;
		static readonly DateTime testDateTimeLocal = new DateTime(2020, 08, 25, 13, 37, 00, DateTimeKind.Local);
		static readonly DateTime testDateTimeUtc = new DateTime(2020, 08, 25, 13, 37, 00, DateTimeKind.Utc);
		static readonly DateTime testDateTimeUnspecified = new DateTime(2020, 08, 25, 13, 37, 00);

		static readonly DateTimeOffset testDateTimeOffsetNow = new DateTimeOffset(testDateTimeNow);
		static readonly DateTimeOffset testDateTimeOffsetLocal = new DateTimeOffset(2020, 08, 25, 13, 37, 00, DateTimeOffset.Now.Offset);
		static readonly DateTimeOffset testDateTimeOffsetUtc = new DateTimeOffset(2020, 08, 25, 13, 37, 00, DateTimeOffset.UtcNow.Offset);

		public static IEnumerable<object[]> GetData() =>
			new List<object[]>
			{
				new object[] { testDateTimeOffsetNow, testDateTimeNow },
				new object[] { DateTimeOffset.MinValue, DateTime.MinValue },
				new object[] { DateTimeOffset.MaxValue, DateTime.MaxValue },
				new object[] { testDateTimeOffsetLocal, testDateTimeLocal },
				new object[] { testDateTimeOffsetUtc, testDateTimeUtc },
				new object[] { testDateTimeOffsetUtc, testDateTimeUnspecified },
			};

		public static IEnumerable<object[]> GetDataReverse() =>
			new List<object[]>
			{
				new object[] { testDateTimeNow, testDateTimeOffsetNow },
				new object[] { DateTime.MinValue, DateTimeOffset.MinValue },
				new object[] { DateTime.MaxValue, DateTimeOffset.MaxValue },
				new object[] { testDateTimeLocal, testDateTimeOffsetLocal },
				new object[] { testDateTimeUtc, testDateTimeOffsetUtc },
				new object[] { testDateTimeUnspecified, testDateTimeOffsetUtc },
			};

		[TestCaseSource(nameof(GetData))]
		public void DateTimeOffsetConverter(DateTimeOffset value, DateTime expectedResult)
		{
			var dateTimeOffsetConverter = new DateTimeOffsetConverter();

			var result = dateTimeOffsetConverter.Convert(value, typeof(DateTimeOffsetConverter_Tests), null,
				CultureInfo.CurrentCulture);

			Assert.AreEqual(expectedResult, result);
		}

		[TestCaseSource(nameof(GetDataReverse))]
		public void DateTimeOffsetConverterBack(DateTime value, DateTimeOffset expectedResult)
		{
			var dateTimeOffsetConverter = new DateTimeOffsetConverter();

			var result = dateTimeOffsetConverter.ConvertBack(value, typeof(DateTimeOffsetConverter_Tests), null,
				CultureInfo.CurrentCulture);

			Assert.AreEqual(expectedResult, result);
		}

		[Test]
		public void DateTimeOffsetConverter_GivenInvalidParameters_ThrowsException()
		{
			var dateTimeOffsetConverter = new DateTimeOffsetConverter();

			Assert.Throws<ArgumentException>(() => dateTimeOffsetConverter.Convert("Not a DateTimeOffset",
				typeof(DateTimeOffsetConverter_Tests), null,
				CultureInfo.CurrentCulture));
		}

		[Test]
		public void DateTimeOffsetConverterBack_GivenInvalidParameters_ThrowsException()
		{
			var dateTimeOffsetConverter = new DateTimeOffsetConverter();

			Assert.Throws<ArgumentException>(() => dateTimeOffsetConverter.ConvertBack("Not a DateTime",
				typeof(DateTimeOffsetConverter_Tests), null,
				CultureInfo.CurrentCulture));
		}
	}
}