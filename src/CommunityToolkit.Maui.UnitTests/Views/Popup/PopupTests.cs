using CommunityToolkit.Maui.Views;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class PopupTests
{
	[Fact]
	public void NotifyPopupIsOpened_ShouldInvokeOpenedEvent()
	{
		// Arrange
		var popup = new Popup();
		var eventInvoked = false;
		popup.Opened += (sender, args) => eventInvoked = true;

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
		popup.Closed += (sender, args) => eventInvoked = true;

		// Act
		popup.NotifyPopupIsClosed();

		// Assert
		eventInvoked.Should().BeTrue();
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
