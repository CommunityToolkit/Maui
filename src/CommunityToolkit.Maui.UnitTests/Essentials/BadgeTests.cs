using CommunityToolkit.Maui.ApplicationModel;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Essentials;

public class BadgeTests : BaseTest
{
	[Fact]
	public void BadgeTestsSetDefaultUpdatesInstance()
	{
		var badgeImplementationMock = new BadgeImplementationMock();
		Badge.SetDefault(badgeImplementationMock);
		var badge = Badge.Default;
		badge.Should().BeSameAs(badgeImplementationMock);
	}

	[Fact]
	public void SetBadgeFailsOnNet()
	{
		Badge.SetDefault(new BadgeImplementation());
		Assert.Throws<NotSupportedException>(() => Badge.SetCount(1));
	}
}