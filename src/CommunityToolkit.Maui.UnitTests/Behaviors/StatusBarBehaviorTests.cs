using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class StatusBarBehaviorTests() : BaseBehaviorTest<StatusBarBehavior, Page>(new StatusBarBehavior(), new ContentPage())
{
	[Fact]
	public void VerifyDefaults()
	{
		var statusBarBehavior = new StatusBarBehavior();

		Assert.Equal(Colors.Transparent, statusBarBehavior.StatusBarColor);
		Assert.Equal(StatusBarStyle.Default, statusBarBehavior.StatusBarStyle);
	}

	[Fact]
	public void VerifyAttachToPageSucceeds()
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

	[Fact]
	public void VerifyStatusBarColorCallsOnPropertyChangedThrowsExceptionOnUnsupportedPlatform()
	{
		var statusBarBehavior = new StatusBarBehavior();
		var exception = Assert.Throws<NotSupportedException>(() => statusBarBehavior.StatusBarColor = Colors.Red);
		exception.Message.Should().Be("PlatformSetColor is only supported on iOS and Android 23 and later");
	}

	[Fact]
	public void VerifyStatusBarStyleCallsOnPropertyChangedThrowsExceptionOnUnsupportedPlatform()
	{
		var statusBarBehavior = new StatusBarBehavior();
		var exception = Assert.Throws<NotSupportedException>(() => statusBarBehavior.StatusBarStyle = StatusBarStyle.DarkContent);
		exception.Message.Should().Be("PlatformSetStyle is only supported on iOS and Android 23 and later");
	}
}