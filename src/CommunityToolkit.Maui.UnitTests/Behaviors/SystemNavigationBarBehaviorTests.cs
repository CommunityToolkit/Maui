using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class SystemNavigationBarBehaviorTests : BaseTest
{
	[Fact]
	public void VerifyDefaults()
	{
		var systemNavigationBarBehavior = new SystemNavigationBarBehavior();

		Assert.Equal(Colors.Transparent, systemNavigationBarBehavior.SystemNavigationBarColor);
		Assert.Equal(SystemNavigationBarStyle.Default, systemNavigationBarBehavior.SystemNavigationBarStyle);
	}

	[Fact]
	public void VerifyAttachToPageSucceeds()
	{
		var systemNavigationBarBehavior = new SystemNavigationBarBehavior();

		var contentPage = new Page
		{
			Behaviors = { systemNavigationBarBehavior }
		};

		Assert.Single(contentPage.Behaviors.OfType<SystemNavigationBarBehavior>());
	}

	[Fact]
	public void VerifyAttachToViewLabelFails()
	{
		var systemNavigationBarBehavior = new SystemNavigationBarBehavior();

		var view = new View();

		Assert.Throws<InvalidOperationException>(() => view.Behaviors.Add(systemNavigationBarBehavior));
	}

	[Fact]
	public void VerifySystemNavigationBarColorCallsOnPropertyChangedThrowsExceptionOnUnsupportedPlatform()
	{
		var systemNavigationBarBehavior = new SystemNavigationBarBehavior();
		var exception = Assert.Throws<NotSupportedException>(() => systemNavigationBarBehavior.SystemNavigationBarColor = Colors.Red);
		exception.Message.Should().Be("PlatformSetColor is only supported on Android 23 and later");
	}

	[Fact]
	public void VerifySystemNavigationBarStyleCallsOnPropertyChangedThrowsExceptionOnUnsupportedPlatform()
	{
		var systemNavigationBarBehavior = new SystemNavigationBarBehavior();
		var exception = Assert.Throws<NotSupportedException>(() => systemNavigationBarBehavior.SystemNavigationBarStyle = SystemNavigationBarStyle.DarkContent);
		exception.Message.Should().Be("PlatformSetStyle is only supported on Android 23 and later");
	}
}