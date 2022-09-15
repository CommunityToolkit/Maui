using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests;

public class StatusBarTests : BaseTest
{
	[Fact]
	public void VerifyDefaults()
	{
		var statusBarBehavior = new StatusBarBehavior();

		Assert.Equal(Colors.Transparent, statusBarBehavior.StatusBarColor);
		Assert.Equal(Core.StatusBarStyle.Default, statusBarBehavior.StatusBarStyle);
	}
}

