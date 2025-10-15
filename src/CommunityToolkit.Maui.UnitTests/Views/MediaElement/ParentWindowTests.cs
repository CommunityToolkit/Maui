using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;
using ParentWindow = CommunityToolkit.Maui.Extensions.PageExtensions.ParentWindow;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class ParentWindowTests : BaseViewTest
{
	[Fact]
	public void Exists_WhenParentWindowIsNull_ReturnsFalse()
	{
		Application.Current.Should().NotBeNull();

		var window = new Window
		{
			Page = new ContentPage()
		};
		Application.Current.AddWindow(window);

		ParentWindow.Exists.Should().BeFalse();
		Application.Current.RemoveWindow(window);
	}

	[Fact]
	public void Exists_WhenParentWindowHandlerIsNull_ReturnsFalse()
	{
		Application.Current.Should().NotBeNull();

		var mockWindow = new Window();
		var mockPage = new ContentPage();
		mockWindow.Page = mockPage;
		Application.Current.AddWindow(mockWindow);

		ParentWindow.Exists.Should().BeFalse();
		Application.Current.RemoveWindow(mockWindow);
	}

	[Fact]
	public void Exists_WhenParentWindowHandlerPlatformViewIsNull_ReturnsFalse()
	{
		Application.Current.Should().NotBeNull();

		var mockWindow = new Window();
		var mockPage = new ContentPage();
		mockWindow.Page = mockPage;
		Application.Current.AddWindow(mockWindow);

		// Simulate a scenario where the handler is set but the platform view is null
		mockWindow.Handler = new MockWindowHandler();

		ParentWindow.Exists.Should().BeFalse();
		Application.Current.RemoveWindow(mockWindow);
	}

	[Fact]
	public void Exists_WhenAllConditionsAreMet_ReturnsTrue()
	{
		Application.Current.Should().NotBeNull();

		var mockWindow = new Window();
		var mockPage = new ContentPage();
		mockWindow.Page = mockPage;
		Application.Current.AddWindow(mockWindow);

		// Simulate a scenario where all conditions are met
		mockWindow.Handler = new MockWindowHandler { PlatformView = new object() };

		ParentWindow.Exists.Should().BeTrue();
		Application.Current.RemoveWindow(mockWindow);
	}
}