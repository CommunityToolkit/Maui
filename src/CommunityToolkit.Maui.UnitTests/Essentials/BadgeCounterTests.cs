using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Essentials;

public class BadgeCounterTests
{
	[Fact]
	public void BadgeCounterTestsSetDefaultUpdatesInstance()
	{
		var badgeCounterImplementationMock = new BadgeCounterImplementationMock();
		BadgeCounter.BadgeCounter.SetDefault(badgeCounterImplementationMock);
		var badgeCounter = BadgeCounter.BadgeCounter.Default;
		badgeCounter.Should().BeSameAs(badgeCounterImplementationMock);
	}

	[Fact]
	public void SetBadgeCountFailsOnNet()
	{
		BadgeCounter.BadgeCounter.SetDefault(new BadgeCounter.BadgeCounterImplementation());
		Assert.Throws<NotImplementedException>(() => BadgeCounter.BadgeCounter.SetBadgeCount(1));
	}
}