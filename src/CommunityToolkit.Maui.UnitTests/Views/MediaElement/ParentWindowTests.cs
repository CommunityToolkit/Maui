using CommunityToolkit.Maui.UnitTests.Mocks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ParentWindow = CommunityToolkit.Maui.Extensions.PageExtensions.ParentWindow;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class ParentWindowTests : BaseHandlerTest
{
	Application application { get; }
	public ParentWindowTests()
	{
		var appBuilder = MauiApp.CreateBuilder()
								.UseMauiCommunityToolkit()
								.UseMauiApp<MockApplication>();

		var mauiApp = appBuilder.Build();

		var Application = mauiApp.Services.GetRequiredService<IApplication>();
		application = (Application)Application;
		IPlatformApplication.Current = (IPlatformApplication)application;

		application.Handler = new ApplicationHandlerStub();
		application.Handler.SetMauiContext(new HandlersContextStub(mauiApp.Services));
	}
	[Fact]
	public void Exists_WhenParentWindowIsNull_ReturnsFalse()
	{
		application.MainPage = new ContentPage();

		Assert.False(ParentWindow.Exists);
	}

	[Fact]
	public void Exists_WhenParentWindowHandlerIsNull_ReturnsFalse()
	{
		var mockWindow = new Window();
		var mockPage = new ContentPage();
		mockWindow.Page = mockPage;
		application.MainPage = mockPage;

		Assert.False(ParentWindow.Exists);
	}

	[Fact]
	public void Exists_WhenParentWindowHandlerPlatformViewIsNull_ReturnsFalse()
	{
		var mockWindow = new Window();
		var mockPage = new ContentPage();
		mockWindow.Page = mockPage;
		application.MainPage = mockPage;

		// Simulate a scenario where the handler is set but the platform view is null
		mockWindow.Handler = new MockWindowHandler();

		Assert.False(ParentWindow.Exists);
	}

	[Fact]
	public void Exists_WhenAllConditionsAreMet_ReturnsTrue()
	{
		var mockWindow = new Window();
		var mockPage = new ContentPage();
		mockWindow.Page = mockPage;
		application.MainPage = mockPage;

		// Simulate a scenario where all conditions are met
		mockWindow.Handler = new MockWindowHandler { PlatformView = new object() };

		Assert.True(ParentWindow.Exists);
	}
}