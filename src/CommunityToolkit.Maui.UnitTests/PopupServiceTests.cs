using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
using CommunityToolkit.Maui.Views;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests;

public class PopupServiceTests : BaseHandlerTest
{
	public PopupServiceTests()
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
	public async Task ShowPopupAsyncWithNullOnPresentingShouldThrowArgumentNullException()
	{
		var popupService = new PopupService(new MockServiceProvider());

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		await Assert.ThrowsAsync<ArgumentNullException>(() =>
			popupService.ShowPopupAsync<INotifyPropertyChanged>(onPresenting: null, token: CancellationToken.None));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_CancellationTokenExpired()
	{
		var popupViewModel = new MockPageViewModel();
		var popupInstance = new MockSelfClosingPopup(string.Empty)
		{
			BindingContext = popupViewModel
		};

		SetupTest(popupInstance, () => popupViewModel, out var popupService);

		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Ensure CancellationToken has expired
		await Task.Delay(100, CancellationToken.None);

		await Assert.ThrowsAsync<TaskCanceledException>(() => popupService.ShowPopupAsync<MockPageViewModel>(token: cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_CancellationTokenCanceled()
	{
		var popupViewModel = new MockPageViewModel();
		var popupInstance = new MockSelfClosingPopup(string.Empty)
		{
			BindingContext = popupViewModel
		};

		SetupTest(popupInstance, () => popupViewModel, out var popupService);

		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Ensure CancellationToken has expired
		await cts.CancelAsync();

		await Assert.ThrowsAsync<TaskCanceledException>(() => popupService.ShowPopupAsync<MockPageViewModel>(token: cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsyncShouldThrowInvalidOperationExceptionWhenNoViewModelIsRegistered()
	{
		var popupInstance = new MockMismatchedPopup();
		var popupViewModel = new MockPageViewModel();

		SetupTest(popupInstance, () => popupViewModel, out var popupService);

		await Assert.ThrowsAsync<InvalidOperationException>(() => popupService.ShowPopupAsync<MockPageViewModel>(token: CancellationToken.None));
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShowPopupAsyncShouldValidateProperBindingContext()
	{
		var popupViewModel = new MockPageViewModel();
		var popupInstance = new MockSelfClosingPopup(string.Empty)
		{
			BindingContext = popupViewModel
		};

		SetupTest(popupInstance, () => popupViewModel, out var popupService);

		await popupService.ShowPopupAsync<MockPageViewModel>(token: CancellationToken.None);

		Assert.Same(popupInstance.BindingContext, popupViewModel);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsyncWithOnPresenting_CancellationTokenExpired()
	{
		var popupViewModel = new MockPageViewModel();
		var popupInstance = new MockSelfClosingPopup(string.Empty)
		{
			BindingContext = popupViewModel
		};

		SetupTest(popupInstance, () => popupViewModel, out var popupService);

		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Ensure CancellationToken has expired
		await Task.Delay(100, CancellationToken.None);

		await Assert.ThrowsAsync<TaskCanceledException>(() => popupService.ShowPopupAsync<MockPageViewModel>(viewModel => viewModel.HasLoaded = true, token: cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsyncWithOnPresenting_CancellationTokenCanceled()
	{
		var popupViewModel = new MockPageViewModel();
		var popupInstance = new MockSelfClosingPopup(string.Empty)
		{
			BindingContext = popupViewModel
		};

		SetupTest(popupInstance, () => popupViewModel, out var popupService);

		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Ensure CancellationToken has expired
		await cts.CancelAsync();

		await Assert.ThrowsAsync<TaskCanceledException>(() => popupService.ShowPopupAsync<MockPageViewModel>(viewModel => viewModel.HasLoaded = true, token: cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShowPopupAsyncWithOnPresentingShouldBeInvoked()
	{
		var popupViewModel = new MockPageViewModel();
		var popupInstance = new MockSelfClosingPopup(string.Empty)
		{
			BindingContext = popupViewModel
		};

		SetupTest(popupInstance, () => popupViewModel, out var popupService);

		await popupService.ShowPopupAsync<MockPageViewModel>(onPresenting: viewModel => viewModel.HasLoaded = true, token: CancellationToken.None);

		Assert.True(popupViewModel.HasLoaded);
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShowPopupAsyncShouldReturnResultOnceClosed()
	{
		var expectedResult = new object();

		var popupViewModel = new MockPageViewModel();
		var popupInstance = new MockSelfClosingPopup(expectedResult)
		{
			BindingContext = popupViewModel
		};

		SetupTest(popupInstance, () => popupViewModel, out var popupService);

		var result = await popupService.ShowPopupAsync<MockPageViewModel>(token: CancellationToken.None);

		Assert.Same(expectedResult, result);
	}

	[Fact]
	public void ShowPopupWithNullOnPresentingShouldThrowArgumentNullException()
	{
		var popupService = new PopupService(new MockServiceProvider());

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => popupService.ShowPopup<INotifyPropertyChanged>(onPresenting: null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact]
	public void ShowPopupShouldThrowInvalidOperationExceptionWhenNoViewModelIsRegistered()
	{
		var popupInstance = new MockMismatchedPopup();
		var popupViewModel = new MockPageViewModel();

		SetupTest(popupInstance, () => popupViewModel, out var popupService);

		Assert.Throws<InvalidOperationException>(() => popupService.ShowPopup<MockPageViewModel>());
	}

	[Fact]
	public void ShowPopupWithOnPresentingShouldBeInvoked()
	{
		var popupViewModel = new MockPageViewModel();
		var popupInstance = new MockPopup
		{
			BindingContext = popupViewModel
		};

		SetupTest(popupInstance, () => popupViewModel, out var popupService);

		popupService.ShowPopup<MockPageViewModel>(onPresenting: viewModel => viewModel.HasLoaded = true);

		Assert.True(popupViewModel.HasLoaded);
	}

	static void SetupTest(
		Popup popup,
		Func<INotifyPropertyChanged> createViewModelInstance,
		out IPopupService popupService)
	{
		popupService = new PopupService(
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

		CreateElementHandler<MockPopupHandler>(popup);

		Assert.NotNull(popup.Handler);
		Assert.NotNull(page.Handler);
	}

	class MockServiceProvider : IServiceProvider
	{
		readonly IDictionary<Type, object> registrations = new Dictionary<Type, object>();

		public MockServiceProvider()
		{
		}

		MockServiceProvider(params (object implementation, Type forType)[] registrations) : this()
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
			if (registrations.TryGetValue(serviceType, out var registeredImplementation))
			{
				return registeredImplementation;
			}

			return null;
		}
	}

	class MockPopup : Popup
	{
		public MockPopup()
		{
		}
	}

	class MockSelfClosingPopup : Popup
	{
		readonly object result;

		public MockSelfClosingPopup(object result)
		{
			this.result = result;
		}

		internal override async void OnOpened()
		{
			base.OnOpened();

			await Task.Delay(TimeSpan.FromMilliseconds(500));

			Close(result);
		}
	}

	class MockMismatchedPopup : Popup
	{
		public MockMismatchedPopup()
		{
			BindingContext = new object();
		}
	}
}