using CommunityToolkit.Maui.Core.Views;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class MediaManagerTests : BaseViewTest
{
	[Theory]
	[InlineData(1, 1)]
	[InlineData(0, 0)]
	[InlineData(1.5, 1.5)]
	[InlineData(1, 1.005)]
	[InlineData(1.005, 1)]
	[InlineData(-2, -2)]
	public void AreFloatingPointNumbersEqual_NumbersWithinDefaultTolerance_ReturnsTrue(double number1, double number2)
	{
		MediaManager.AreFloatingPointNumbersEqual(number1, number2).Should().BeTrue();
	}

	[Theory]
	[InlineData(1, 2)]
	[InlineData(2, 1)]
	[InlineData(0, 1)]
	[InlineData(1, 1.02)]
	[InlineData(1.5, -1.5)]
	public void AreFloatingPointNumbersEqual_NumbersOutsideDefaultTolerance_ReturnsFalse(double number1, double number2)
	{
		MediaManager.AreFloatingPointNumbersEqual(number1, number2).Should().BeFalse();
	}

	[Theory]
	[InlineData(1, 1.4, 0.5, true)]
	[InlineData(1, 1.6, 0.5, false)]
	[InlineData(1, 1.05, 0.1, true)]
	[InlineData(1, 1.05, 0.001, false)]
	public void AreFloatingPointNumbersEqual_CustomTolerance_ReturnsExpectedResult(double number1, double number2, double tolerance, bool expectedResult)
	{
		MediaManager.AreFloatingPointNumbersEqual(number1, number2, tolerance).Should().Be(expectedResult);
	}
}