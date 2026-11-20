using System.Globalization;
using CommunityToolkit.Maui.Media;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Essentials;

public class SilenceTimeoutMillisecondsTests : BaseTest
{
	[Theory]
	[InlineData(1, 1)]
	[InlineData(1500, 1500)]
	[InlineData(2_500.7, 2500)]
	[InlineData(int.MaxValue - 1, int.MaxValue - 1)]
	[InlineData(int.MaxValue, int.MaxValue)]
	[InlineData(3_000_000_000d, int.MaxValue)]
	[InlineData(double.MaxValue, int.MaxValue)]
	public void FromClampsToTheRangeOfAnInt(double milliseconds, int expected)
	{
		var timeout = milliseconds >= TimeSpan.MaxValue.TotalMilliseconds ? TimeSpan.MaxValue : TimeSpan.FromMilliseconds(milliseconds);

		Assert.Equal(expected, SilenceTimeoutMilliseconds.From(timeout));
	}

	[Fact]
	public void FromNeverReturnsZeroForASubMillisecondTimeout()
	{
		Assert.Equal(1, SilenceTimeoutMilliseconds.From(TimeSpan.FromTicks(1)));
	}

	[Theory]
	[InlineData("tr-TR")]
	[InlineData("az-Latn-AZ")]
	[InlineData("de-DE")]
	[InlineData("sv-SE")]
	[InlineData("ar-SA")]
	public void FromIgnoresTheCurrentCulture(string cultureName)
	{
		var originalCulture = CultureInfo.CurrentCulture;
		var originalUiCulture = CultureInfo.CurrentUICulture;
		try
		{
			CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo(cultureName);

			Assert.Equal(3500, SilenceTimeoutMilliseconds.From(TimeSpan.FromMilliseconds(3500.5)));
		}
		finally
		{
			CultureInfo.CurrentCulture = originalCulture;
			CultureInfo.CurrentUICulture = originalUiCulture;
		}
	}
}