using System.ComponentModel;
using CommunityToolkit.Maui.UnitTests.Mocks;
using CommunityToolkit.Maui.Views;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests;

public class PopupServiceTests : BaseHandlerTest
{
	public PopupServiceTests() : base()
	{
		var appBuilder = MauiApp.CreateBuilder()
								.UseMauiCommunityToolkit()
								.UseMauiApp<MockApplication>();

		var mauiApp = appBuilder.Build();
		var application = mauiApp.Services.GetRequiredService<IApplication>();
		application.Handler = new ApplicationHandlerStub();
		application.Handler.SetMauiContext(new HandlersContextStub(mauiApp.Services));
	}

	[Fact]
	public static void ShowPopupWithNullViewModelShouldThrowArgumentNullException()
	{
		var popupService = new PopupService(new MockServiceProvider());

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => popupService.ShowPopup<INotifyPropertyChanged>(viewModel: null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

//	[Fact]
//	public static void ShowPopupWithNullQueryShouldThrowArgumentNullException()
//	{
//		var popupService = new PopupService(new MockServiceProvider());

//#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
//		Assert.Throws<ArgumentNullException>(() => popupService.ShowPopup<IArgumentsReceiver>(null));
//#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
//	}

	[Fact]
	public static void ShowPopupShouldThrowInvalidOperationExceptionWhenNoViewModelIsRegistered()
	{
		var popupService = new PopupService(new MockServiceProvider());

		Assert.Throws<InvalidOperationException>(popupService.ShowPopup<MockPageViewModel>);
	}

	[Fact]
	public static void D()
	{
		var serviceCollection = new ServiceCollection();
		PopupService.AddTransientPopup<MockPopup, MockPageViewModel>(serviceCollection);

		var popup = new MockPopup();

		var popupService = new PopupService(
			MockServiceProvider.ThatProvides(
				(implementation: popup, forType: typeof(MockPopup)),
				(implementation: new MockPageViewModel(), forType: typeof(MockPageViewModel))));

		Application.Current = new MockApplication();
		var app = Application.Current ?? throw new NullReferenceException();

		var page = new ContentPage
		{
			Content = new Label
			{
				Text = "Hello there"
			},
			IsPlatformEnabled = true
		};

		//// Make sure that our page will have a Handler
		//CreateViewHandler<MockPageHandler>(page);

		app.MainPage = page;

		// Make sure that our popup will have a Handler
		//CreateElementHandler<MockPopupHandler>(popup);

		bool popupWasOpened = false;

		popup.Opened += (s, e) =>
		{
			popupWasOpened = true;
		};

		popupService.ShowPopup<MockPageViewModel>();

		Assert.True(popupWasOpened);
	}
}

public class MockServiceProvider : IServiceProvider
{
	readonly IDictionary<Type, object> registrations;

	public MockServiceProvider()
	{
		registrations = new Dictionary<Type, object>();
	}

	public MockServiceProvider(params (object implementation, Type forType)[] registrations) : this()
	{
		foreach (var (implementation, forType) in registrations)
		{
			this.registrations.Add(forType, implementation);
		}
	}

	public static MockServiceProvider ThatProvides(params (object implementation, Type forType)[] registrations)
	{
		return new MockServiceProvider(registrations);
	}

	public object? GetService(Type serviceType)
	{
		if (this.registrations.TryGetValue(serviceType, out var registeredImplementation))
		{
			return registeredImplementation;
		}

		return null;
	}
}

public class MockPopup : Popup
{

}
