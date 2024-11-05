using CommunityToolkit.Maui.UnitTests.Mocks;
using Xunit;
using ParentWindow = CommunityToolkit.Maui.Extensions.PageExtensions.ParentWindow;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class ParentWindowTests : BaseHandlerTest
{
	[Fact]
	public void Exists_WhenParentWindowIsNull_ReturnsFalse()
	{
		Assert.NotNull(Application.Current);

		Application.Current.Windows[0].Page = new ContentPage();

		Assert.False(ParentWindow.Exists);
	}

	[Fact]
	public void Exists_WhenParentWindowHandlerIsNull_ReturnsFalse()
	{
		Assert.NotNull(Application.Current);

		var mockWindow = new Window();
		var mockPage = new ContentPage();
		mockWindow.Page = mockPage;
		Application.Current.Windows[0].Page = mockPage;

		Assert.False(ParentWindow.Exists);
	}

	[Fact]
	public void Exists_WhenParentWindowHandlerPlatformViewIsNull_ReturnsFalse()
	{
		Assert.NotNull(Application.Current);

		var mockWindow = new Window();
		var mockPage = new ContentPage();
		mockWindow.Page = mockPage;
		Application.Current.Windows[0].Page = mockPage;

		// Simulate a scenario where the handler is set but the platform view is null
		mockWindow.Handler = new MockWindowHandler();

		Assert.False(ParentWindow.Exists);
	}

	[Fact]
	public void Exists_WhenAllConditionsAreMet_ReturnsTrue()
	{
		Assert.NotNull(Application.Current);

		var window = Application.Current.Windows[0];
		Application.Current.Windows[0].Page = new ContentPage();

		// Simulate a scenario where all conditions are met
		window.Handler = new MockWindowHandler { PlatformView = new object() };

		Assert.True(ParentWindow.Exists);
	}
}