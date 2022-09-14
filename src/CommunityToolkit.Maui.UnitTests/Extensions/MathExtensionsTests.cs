using CommunityToolkit.Maui.Core.Extensions;
using Xunit;
namespace CommunityToolkit.Maui.UnitTests.Extensions;

public class MathExtensionsTests : BaseTest
{
	[Theory]
	[InlineData(0.0, true)]
	[InlineData(double.NaN, true)]
	[InlineData(100.0, false)]
	[InlineData(0.001, false)]
	public void CheckIfNumberIsNaNOrZero(double number, bool expected)
	{
		var result = number.IsZeroOrNaN();
		Assert.Equal(expected, result);
	}
}