using CommunityToolkit.Maui.Views;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class PopupTests : BaseTest
{
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
		await Assert.ThrowsAsync<InvalidOperationException>(() => popup.Close(TestContext.Current.CancellationToken));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task PopupT_Close_ShouldThrowExceptionWhenCalledBeforeShown()
	{
		// Arrange
		var popup = new Popup<string>();

		// Assert
		await Assert.ThrowsAsync<InvalidOperationException>(() => popup.Close("Hello", TestContext.Current.CancellationToken));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task Popup_Close_ShouldNotThrowExceptionWhenCloseIsOverridden()
	{
		// Arrange
		var popup = new PopupOverridingClose();

		// Assert
		await popup.Close(TestContext.Current.CancellationToken);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task PopupT_Close_ShouldNotThrowExceptionWhenCloseIsOverridden()
	{
		// Arrange
		var popup = new PopupTOverridingClose();

		// Assert
		await popup.Close(TestContext.Current.CancellationToken);
		await popup.Close("Hello", TestContext.Current.CancellationToken);
	}

	class PopupOverridingClose : Popup
	{
		public override Task Close(CancellationToken token = default) => Task.CompletedTask;
	}

	class PopupTOverridingClose : Popup<string>
	{
		public override Task Close(CancellationToken token = default) => Task.CompletedTask;
		public override Task Close(string result, CancellationToken token = default) => Task.CompletedTask;
	}
}

file class MockPopup : Popup
{
}

file class MockPopup<T> : Popup<T>
{
}

file class MockPopupOptions : IPopupOptions
{
	public bool CanBeDismissedByTappingOutsideOfPopup { get; set; }
	public Color BackgroundColor { get; set; } = Colors.Transparent;
	public Action? OnTappingOutsideOfPopup { get; set; }
	public IShape? Shape { get; set; }
	public Thickness Margin { get; set; }
	public Thickness Padding { get; set; }
	public LayoutOptions VerticalOptions { get; set; }
	public LayoutOptions HorizontalOptions { get; set; }
}