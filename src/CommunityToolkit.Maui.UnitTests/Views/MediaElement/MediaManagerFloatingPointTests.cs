using CommunityToolkit.Maui.Core.Views;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

// MediaManager.AreFloatingPointNumbersEqual is public, shared logic (MediaManager.shared.cs), so its tests belong here rather than in a device test.
public class MediaManagerFloatingPointTests : BaseTest
{
	[Fact]
	public void AreFloatingPointNumbersEqual_NumbersWithinTheTolerance_ReturnsTrue()
	{
		MediaManager.AreFloatingPointNumbersEqual(1.5, 1.5).Should().BeTrue();
		MediaManager.AreFloatingPointNumbersEqual(1.5, 1.505).Should().BeTrue();
		MediaManager.AreFloatingPointNumbersEqual(1.0, 1.4, tolerance: 0.5).Should().BeTrue();
	}

	[Fact]
	public void AreFloatingPointNumbersEqual_NumbersFurtherApartThanTheTolerance_ReturnsFalse()
	{
		MediaManager.AreFloatingPointNumbersEqual(1.5, 2.0).Should().BeFalse();
		MediaManager.AreFloatingPointNumbersEqual(1.5, 1.52).Should().BeFalse();
		MediaManager.AreFloatingPointNumbersEqual(1.0, 1.6, tolerance: 0.5).Should().BeFalse();
	}
}