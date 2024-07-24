using CommunityToolkit.Maui.Converters;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Converters;

public class TimeSpanToSecondsConverterTests : BaseConverterTest<TimeSpanToSecondsConverter>
{
	public static readonly IReadOnlyList<object[]> ValidData =
	[
		[TimeSpan.MaxValue, 922337203685.4775],
		[TimeSpan.FromSeconds(100), 100],
		[new TimeSpan(), 0],
		[TimeSpan.Zero, 0],
		[default(TimeSpan), 0],
		[TimeSpan.MinValue, -922337203685.4775],
	];

	[Theory]
	[MemberData(nameof(ValidData))]
	public void TimeSpanToSecondsConverter_ConvertFrom_ValidData(TimeSpan value, double expectedResult)
	{
		var converter = new TimeSpanToSecondsConverter();

		var convertResult = ((ICommunityToolkitValueConverter)converter).Convert(value, typeof(double), null, null);
		var convertFromResult = converter.ConvertFrom(value);

		Assert.Equal(expectedResult, convertResult);
		Assert.Equal(expectedResult, convertFromResult);
	}

	[Theory]
	[MemberData(nameof(ValidData))]
	public void TimeSpanToSecondsConverter_ConvertBack_ValidData(TimeSpan expectedResult, double value)
	{
		var converter = new TimeSpanToSecondsConverter();

		var convertBackResult = ((ICommunityToolkitValueConverter)converter).ConvertBack(value, typeof(TimeSpan), null, null);
		var convertBackToResult = converter.ConvertBackTo(value);

		Assert.Equal(expectedResult, convertBackResult);
		Assert.Equal(expectedResult, convertBackToResult);
	}

	[Fact]
	public void TimeSpanToSecondsConverter_NullData_ThrowsArgumentNullException()
	{
		var converter = new TimeSpanToSecondsConverter();

		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)converter).Convert(null, typeof(double), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)converter).ConvertBack(null, typeof(TimeSpan), null, null));
	}

	[Fact]
	public void TimeSpanToSecondsConverterNullInputTest()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new TimeSpanToSecondsConverter()).Convert(TimeSpan.Zero, null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new TimeSpanToSecondsConverter()).Convert(null, typeof(double), null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new TimeSpanToSecondsConverter()).ConvertBack(0.0, null, null, null));
		Assert.Throws<ArgumentNullException>(() => ((ICommunityToolkitValueConverter)new TimeSpanToSecondsConverter()).ConvertBack(null, typeof(TimeSpan), null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
}