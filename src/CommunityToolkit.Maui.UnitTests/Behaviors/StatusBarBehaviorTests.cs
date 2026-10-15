using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;
	
#pragma warning disable CA1416
public class StatusBarBehaviorTests() : BaseBehaviorTest<StatusBarBehavior, Page>(new StatusBarBehavior(), new ContentPage())
{
	[Fact]
	public void VerifyDefaults()
	{
		var statusBarBehavior = new StatusBarBehavior();

		Assert.Equal(StatusBarBehaviorDefaults.StatusBarColor, statusBarBehavior.StatusBarColor);
		Assert.Equal(StatusBarBehaviorDefaults.StatusBarStyle, statusBarBehavior.StatusBarStyle);
		Assert.Equal(StatusBarBehaviorDefaults.ApplyOn, statusBarBehavior.ApplyOn);
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

		var view = new MockView();

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
	[Fact]
	public void VerifyDefaultStatusBarColorIsTransparent()
	{
		// The default transparent color is exactly the condition that triggered #2351.
		var statusBarBehavior = new StatusBarBehavior();

		Assert.Equal(Colors.Transparent, statusBarBehavior.StatusBarColor);
	}


	[Fact]
	public void VerifyStatusBarColorRaisesPropertyChanged()
	{
		var statusBarBehavior = new StatusBarBehavior();
		string? changedProperty = null;
		statusBarBehavior.PropertyChanged += (_, e) => changedProperty = e.PropertyName;

		var exception = Assert.Throws<NotSupportedException>(() => statusBarBehavior.StatusBarColor = Colors.Red);
		exception.Message.Should().Be("PlatformSetColor is only supported on iOS and Android 23 and later");
		changedProperty.Should().Be(nameof(StatusBarBehavior.StatusBarColor));
	}

	[Fact]
	public void VerifyStatusBarStyleRaisesPropertyChanged()
	{
		var statusBarBehavior = new StatusBarBehavior();
		string? changedProperty = null;
		statusBarBehavior.PropertyChanged += (_, e) => changedProperty = e.PropertyName;

		var exception = Assert.Throws<NotSupportedException>(() => statusBarBehavior.StatusBarStyle = StatusBarStyle.DarkContent);
		exception.Message.Should().Be("PlatformSetStyle is only supported on iOS and Android 23 and later");
		changedProperty.Should().Be(nameof(StatusBarBehavior.StatusBarStyle));
	}

	[Fact]
	public void VerifyApplyOnRaisesPropertyChanged()
	{
		var statusBarBehavior = new StatusBarBehavior();
		string? changedProperty = null;
		statusBarBehavior.PropertyChanged += (_, e) => changedProperty = e.PropertyName;

		statusBarBehavior.ApplyOn = StatusBarApplyOn.OnPageNavigatedTo;

		changedProperty.Should().Be(nameof(StatusBarBehavior.ApplyOn));
	}

	[Fact]
	public void VerifyStatusBarBehaviorOnContentPageHostedInNavigationPage()
	{
		var statusBarBehavior = new StatusBarBehavior();
		var contentPage = new ContentPage
		{
			Behaviors = { statusBarBehavior }
		};
		var navigationPage = new NavigationPage(contentPage);

		Assert.Contains(contentPage, navigationPage.Navigation.NavigationStack);
		Assert.Single(contentPage.Behaviors.OfType<StatusBarBehavior>());
		Assert.Equal(Colors.Transparent, statusBarBehavior.StatusBarColor);
	}

	[Fact]
	public void VerifyStatusBarBehaviorOnContentPagePushedModally()
	{
		var statusBarBehavior = new StatusBarBehavior();
		var modalPage = new ContentPage
		{
			Behaviors = { statusBarBehavior }
		};
		var navigationPage = new NavigationPage(new ContentPage());
		navigationPage.Navigation.PushModalAsync(modalPage);

		Assert.Contains(modalPage, navigationPage.Navigation.ModalStack);
		Assert.Single(modalPage.Behaviors.OfType<StatusBarBehavior>());
		Assert.Equal(Colors.Transparent, statusBarBehavior.StatusBarColor);
	}
}
#pragma warning restore CA1416