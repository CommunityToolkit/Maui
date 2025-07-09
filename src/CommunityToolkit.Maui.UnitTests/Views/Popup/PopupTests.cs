using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.UnitTests.Services;
using CommunityToolkit.Maui.Views;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class PopupTests : BaseViewTest
{
	[Fact]
	public void PopupBackgroundColor_DefaultValue_ShouldBeWhite()
	{
		var popup = new Popup();
		Assert.Equal(Colors.White, popup.BackgroundColor);
	}

	[Fact]
	public void CanBeDismissedByTappingOutsideOfPopup_DefaultValue_ShouldBeTrue()
	{
		var popup = new Popup();
		Assert.True(popup.CanBeDismissedByTappingOutsideOfPopup);
	}

	[Fact]
	public void Margin_DefaultValue_ShouldBeDefaultThickness()
	{
		var popup = new Popup();
		Assert.Equal(new Thickness(30), popup.Margin);
	}

	[Fact]
	public void Margin_SetValue_ShouldBeUpdated()
	{
		var popup = new Popup();
		var thickness = new Thickness(10);
		popup.Margin = thickness;
		Assert.Equal(thickness, popup.Margin);
	}

	[Fact]
	public void Padding_DefaultValue_ShouldBeDefaultThickness()
	{
		var popup = new Popup();
		Assert.Equal(new Thickness(15), popup.Padding);
	}

	[Fact]
	public void Padding_SetValue_ShouldBeUpdated()
	{
		var popup = new Popup();
		var thickness = new Thickness(10);
		popup.Padding = thickness;
		Assert.Equal(thickness, popup.Padding);
	}

	[Fact]
	public void VerticalOptions_DefaultValue_ShouldBeDefaultLayoutOptions()
	{
		var popup = new Popup();
		Assert.Equal(LayoutOptions.Center, popup.VerticalOptions);
	}

	[Fact]
	public void VerticalOptions_SetValue_ShouldBeUpdated()
	{
		var popup = new Popup();
		var layoutOptions = LayoutOptions.Center;
		popup.VerticalOptions = layoutOptions;
		Assert.Equal(layoutOptions, popup.VerticalOptions);
	}

	[Fact]
	public void HorizontalOptions_DefaultValue_ShouldBeDefaultLayoutOptions()
	{
		var popup = new Popup();
		Assert.Equal(LayoutOptions.Center, popup.HorizontalOptions);
	}

	[Fact]
	public void HorizontalOptions_SetValue_ShouldBeUpdated()
	{
		var popup = new Popup();
		var layoutOptions = LayoutOptions.Center;
		popup.HorizontalOptions = layoutOptions;
		Assert.Equal(layoutOptions, popup.HorizontalOptions);
	}

	[Fact]
	public void NotifyPopupIsOpened_ShouldInvokeOpenedEvent()
	{
		// Arrange
		var popup = new Popup();
		var eventInvoked = false;
		popup.Opened += (_, _) => eventInvoked = true;

		// Act
		popup.NotifyPopupIsOpened();

		// Assert
		eventInvoked.Should().BeTrue();
	}

	[Fact]
	public void NotifyPopupIsClosed_ShouldInvokeClosedEvent()
	{
		// Arrange
		var popup = new Popup();
		var eventInvoked = false;
		popup.Closed += (_, _) => eventInvoked = true;

		// Act
		popup.NotifyPopupIsClosed();

		// Assert
		eventInvoked.Should().BeTrue();
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task Popup_Close_ShouldThrowExceptionWhenCalledBeforeShown()
	{
		// Arrange
		var popup = new Popup();

		// Assert
		await Assert.ThrowsAsync<PopupNotFoundException>(() => popup.CloseAsync(TestContext.Current.CancellationToken));
		await Assert.ThrowsAnyAsync<InvalidOperationException>(() => popup.CloseAsync(TestContext.Current.CancellationToken));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task PopupT_Close_ShouldThrowExceptionWhenCalledBeforeShown()
	{
		// Arrange
		var popup = new Popup<string>();

		// Assert
		await Assert.ThrowsAsync<PopupNotFoundException>(() => popup.CloseAsync("Hello", TestContext.Current.CancellationToken));
		await Assert.ThrowsAnyAsync<InvalidOperationException>(() => popup.CloseAsync("Hello", TestContext.Current.CancellationToken));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task Popup_Close_ShouldNotThrowExceptionWhenCloseIsOverridden()
	{
		// Arrange
		var popup = new PopupOverridingClose();

		// Assert
		await popup.CloseAsync(TestContext.Current.CancellationToken);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task PopupT_Close_ShouldNotThrowExceptionWhenCloseIsOverridden()
	{
		// Arrange
		var popup = new PopupTOverridingClose();

		// Assert
		await popup.CloseAsync(TestContext.Current.CancellationToken);
		await popup.CloseAsync("Hello", TestContext.Current.CancellationToken);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_TaskShouldCompleteWhenPopupCloseAsyncIsCalled()
	{
		// Arrange
		Task<IPopupResult> showPopupAsyncTask;
		var popup = new Popup();

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		// Act
		showPopupAsyncTask = page.ShowPopupAsync(popup, token: TestContext.Current.CancellationToken);

		// Assert
		Assert.Single(page.Navigation.ModalStack);
		Assert.IsType<PopupPage>(page.Navigation.ModalStack[0]);

		// Act
		await popup.CloseAsync(TestContext.Current.CancellationToken);
		await showPopupAsyncTask;

		// Assert
		Assert.Empty(page.Navigation.ModalStack);
	}

	sealed class PopupOverridingClose : Popup
	{
		public override Task CloseAsync(CancellationToken token = default) => Task.CompletedTask;
	}

	sealed class PopupTOverridingClose : Popup<string>
	{
		public override Task CloseAsync(CancellationToken token = default) => Task.CompletedTask;
		public override Task CloseAsync(string result, CancellationToken token = default) => Task.CompletedTask;
	}
}