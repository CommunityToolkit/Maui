using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class StatusBarBehaviorTests : BaseTest
{
	[Fact]
	public void VerifyDefaults()
	{
		var statusBarBehavior = new StatusBarBehavior();

		Assert.Equal(Colors.Transparent, statusBarBehavior.StatusBarColor);
		Assert.Equal(Core.StatusBarStyle.Default, statusBarBehavior.StatusBarStyle);
	}

	[Fact]
	public void VerifyAttachToPageSuceedes()
	{
		var statusBarBehavior = new StatusBarBehavior();

		var contentPage = new Page
		{
			Behaviors = { statusBarBehavior }
		};

		Assert.Single(contentPage.Behaviors.OfType<StatusBarBehavior>());
	}

	[Fact]
	public void VerifyAttachToViewLabelFails()
	{
		var statusBarBehavior = new StatusBarBehavior();

		var view = new View();

		Assert.Throws<InvalidOperationException>(() => view.Behaviors.Add(statusBarBehavior));
	}
}