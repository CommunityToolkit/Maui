using CommunityToolkit.Maui.Behaviors;
using Xunit;

namespace CommunityToolkit.Maui.DeviceTests.Tests.UITests;
public sealed class StatusBarUITest : UITests<StatusBarTestPage>
{
	[UIFact]
	public void IsBehaviorAttached()
	{
		var behavior = CurrentPage.Behaviors.FirstOrDefault( x => x is StatusBarBehavior);

		Assert.NotNull(behavior);
	}

	[UIFact]
	public void GetTheColor()
	{
		var behavior = CurrentPage.Behaviors.FirstOrDefault(x => x is StatusBarBehavior) as StatusBarBehavior;

		Assert.NotNull(behavior);

		var color = behavior.StatusBarColor;

		Assert.Equal(Colors.Fuchsia, color);
	}
}
