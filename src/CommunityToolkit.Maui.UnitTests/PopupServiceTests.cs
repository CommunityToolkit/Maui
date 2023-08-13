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

	static PopupServiceTests()
	{
		var serviceCollection = new ServiceCollection();
		PopupService.AddTransientPopup<MockPopup, MockPageViewModel>(serviceCollection);
	}

	[Fact]
	public static void ShowPopupAsyncWithNullViewModelShouldThrowArgumentNullException()
	{
		var popupService = new PopupService(new MockServiceProvider());

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.ThrowsAsync<ArgumentNullException>(() => popupService.ShowPopupAsync<INotifyPropertyChanged>(viewModel: null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact]
	public static void ShowPopupAsyncWithNullOnPresentingShouldThrowArgumentNullException()
	{
		var popupService = new PopupService(new MockServiceProvider());

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.ThrowsAsync<ArgumentNullException>(() => popupService.ShowPopupAsync<INotifyPropertyChanged>(onPresenting: null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact]
	public static void ShowPopupAsyncWithMismatchedViewModelTypeShouldThrowInvalidOperationException()
	{
		var popupService = new PopupService(new MockServiceProvider());

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.ThrowsAsync<ArgumentNullException>(() => popupService.ShowPopupAsync<INotifyPropertyChanged>(viewModel: null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact]
	public static void ShowPopupAsyncShouldThrowInvalidOperationExceptionWhenNoViewModelIsRegistered()
	{
		var popupInstance = new MockMismatchedPopup();
		var popupViewModel = new MockPageViewModel();

		var popupService = SetupTest(popupInstance, () => popupViewModel);

		Assert.ThrowsAsync<InvalidOperationException>(popupService.ShowPopupAsync<MockPageViewModel>);
	}

	[Fact]
	public void ShowPopupAsyncShouldCreateAndAssignBindingContext()
	{
		var popupInstance = new MockPopup();
		var popupViewModel = new MockPageViewModel();

		var popupService = SetupTest(popupInstance, () => popupViewModel);

		_ = popupService.ShowPopupAsync<MockPageViewModel>();

		Assert.Same(popupInstance.BindingContext, popupViewModel);
	}

	[Fact]
	public void ShowPopupAsyncShouldAssignPassedInViewModelToBindingContext()
	{
		var popupInstance = new MockPopup();
		var popupViewModel = new MockPageViewModel();

		var popupService = SetupTest(popupInstance, () => popupViewModel);

		_ = popupService.ShowPopupAsync(popupViewModel);

		Assert.Same(popupInstance.BindingContext, popupViewModel);
	}

	[Fact]
	public void ShowPopupAsyncWithOnPresentingShouldBeInvoked()
	{
		var popupInstance = new MockPopup();
		var popupViewModel = new MockPageViewModel();

		var popupService = SetupTest(popupInstance, () => popupViewModel);

		_ = popupService.ShowPopupAsync<MockPageViewModel>(onPresenting: viewModel => viewModel.HasLoaded = true);

		Assert.True(popupViewModel.HasLoaded);
	}

	[Fact]
	public async Task ShowPopupAsyncShouldReturnResultOnceClosed()
	{
		var expectedResult = new object();

		var popupInstance = new MockSelfClosingPopup(expectedResult);
		var popupViewModel = new MockPageViewModel();

		var popupService = SetupTest(popupInstance, () => popupViewModel);

		var result = await popupService.ShowPopupAsync<MockPageViewModel>();

		Assert.Same(expectedResult, result);
	}

	[Fact]
	public static void ShowPopupWithNullViewModelShouldThrowArgumentNullException()
	{
		var popupService = new PopupService(new MockServiceProvider());

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => popupService.ShowPopup<INotifyPropertyChanged>(viewModel: null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact]
	public static void ShowPopupWithNullOnPresentingShouldThrowArgumentNullException()
	{
		var popupService = new PopupService(new MockServiceProvider());

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => popupService.ShowPopup<INotifyPropertyChanged>(onPresenting: null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact]
	public static void ShowPopupWithMismatchedViewModelTypeShouldThrowInvalidOperationException()
	{
		var popupService = new PopupService(new MockServiceProvider());

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => popupService.ShowPopup<INotifyPropertyChanged>(viewModel: null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact]
	public static void ShowPopupShouldThrowInvalidOperationExceptionWhenNoViewModelIsRegistered()
	{
		var popupInstance = new MockMismatchedPopup();
		var popupViewModel = new MockPageViewModel();

		var popupService = SetupTest(popupInstance, () => popupViewModel);

		Assert.Throws<InvalidOperationException>(popupService.ShowPopup<MockPageViewModel>);
	}

	[Fact]
	public void ShowPopupShouldCreateAndAssignBindingContext()
	{
		var popupInstance = new MockPopup();
		var popupViewModel = new MockPageViewModel();

		var popupService = SetupTest(popupInstance, () => popupViewModel);

		popupService.ShowPopup<MockPageViewModel>();

		Assert.Same(popupInstance.BindingContext, popupViewModel);
	}

	[Fact]
	public void ShowPopupShouldAssignPassedInViewModelToBindingContext()
	{
		var popupInstance = new MockPopup();
		var popupViewModel = new MockPageViewModel();

		var popupService = SetupTest(popupInstance, () => popupViewModel);

		popupService.ShowPopup(popupViewModel);

		Assert.Same(popupInstance.BindingContext, popupViewModel);
	}

	[Fact]
	public void ShowPopupWithOnPresentingShouldBeInvoked()
	{
		var popupInstance = new MockPopup();
		var popupViewModel = new MockPageViewModel();

		var popupService = SetupTest(popupInstance, () => popupViewModel);

		popupService.ShowPopup<MockPageViewModel>(onPresenting: viewModel => viewModel.HasLoaded = true);

		Assert.True(popupViewModel.HasLoaded);
	}

	static PopupService SetupTest(
		Popup popup,
		Func<INotifyPropertyChanged> createViewModelInstance)
	{
		var popupService = new PopupService(
			MockServiceProvider.ThatProvides(
				(implementation: popup, forType: typeof(MockPopup)),
				(implementation: createViewModelInstance.Invoke(), forType: typeof(MockPageViewModel))));

		var app = Application.Current ?? throw new NullReferenceException();

		var page = new ContentPage
		{
			Content = new Label
			{
				Text = "Hello there"
			}
		};

		// Make sure that our page will have a Handler
		CreateViewHandler<MockPageHandler>(page);

		app.MainPage = page;

		_ = CreateElementHandler<MockPopupHandler>(popup);

		Assert.NotNull(popup.Handler);
		Assert.NotNull(page.Handler);

		return popupService;
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
	public MockPopup()
	{

	}
}

public class MockSelfClosingPopup : Popup
{
	readonly object result;

	public MockSelfClosingPopup(object result)
	{
		this.result = result;
	}

	internal override void OnOpened()
	{
		base.OnOpened();

		Task.Run(async () =>
		{
			await Task.Delay(1000);

			this.Close(this.result);
		});
	}
}

public class MockMismatchedPopup : Popup
{
	public MockMismatchedPopup()
	{
		BindingContext = new object();
	}
}
