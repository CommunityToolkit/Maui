using System.ComponentModel;
using CommunityToolkit.Maui.Services;
using CommunityToolkit.Maui.UnitTests.Mocks;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Shapes;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Services;

public class PopupServiceTests : BaseHandlerTest
{
	readonly INavigation navigation;

	public PopupServiceTests()
	{
		var page = new MockPage(new MockPageViewModel());
		Assert.NotNull(Application.Current);

		Application.Current.Windows[0].Page = page;
		navigation = page.Navigation;
	}

	[Fact]
	public void AddPopup_RegistersViewInServiceCollection()
	{
		// Arrange
		var services = new ServiceCollection();

		// Act
		PopupService.AddPopup<MockPopup>(services, ServiceLifetime.Transient);

		// Assert
		var serviceDescriptor = Assert.Single(services);
		Assert.Equal(typeof(MockPopup), serviceDescriptor.ServiceType);
		Assert.Equal(ServiceLifetime.Transient, serviceDescriptor.Lifetime);
	}

	[Fact]
	public void AddPopup_WithViewModel_RegistersBothViewAndViewModel()
	{
		// Arrange
		var services = new ServiceCollection();

		// Act
		PopupService.AddPopup<MockPopup, PopupViewModel>(services, ServiceLifetime.Transient);

		// Assert
		Assert.Equal(2, services.Count);
		Assert.Contains(services, sd => sd.ServiceType == typeof(MockPopup));
		Assert.Contains(services, sd => sd.ServiceType == typeof(PopupViewModel));
	}

	[Fact]
	public void ShowPopupAsync_UsingNavigation_WithViewType_ShowsPopup()
	{
		// Arrange
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		// Act
		popupService.ShowPopup<MockSelfClosingPopup>(navigation);

		// Assert
		Assert.Single(navigation.ModalStack);
		Assert.IsType<PopupPage>(navigation.ModalStack[0]);
	}

	[Fact]
	public void ShowPopupAsync_UsingPage_WithViewType_ShowsPopup()
	{
		// Arrange
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		// Act
		popupService.ShowPopup<MockSelfClosingPopup>(page);

		// Assert
		Assert.Single(navigation.ModalStack);
		Assert.IsType<PopupPage>(navigation.ModalStack[0]);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_AwaitingShowPopupAsync_EnsurePreviousPopupClosed()
	{
		// Arrange
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		// Act
		await popupService.ShowPopupAsync<MockSelfClosingPopup>(navigation, PopupOptions.Empty, CancellationToken.None);
		await popupService.ShowPopupAsync<MockSelfClosingPopup>(navigation, PopupOptions.Empty, CancellationToken.None);

		// Assert
		Assert.Empty(navigation.ModalStack);
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public async Task ShowPopupAsync_UsingPage_AwaitingShowPopupAsync_EnsurePreviousPopupClosed()
	{
		// Arrange
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		// Act
		await popupService.ShowPopupAsync<MockSelfClosingPopup>(page, PopupOptions.Empty, CancellationToken.None);
		await popupService.ShowPopupAsync<MockSelfClosingPopup>(page, PopupOptions.Empty, CancellationToken.None);

		// Assert
		Assert.Empty(navigation.ModalStack);
	}

	[Fact]
	public void ShowPopup_NavigationModalStackCountIncreases()
	{
		// Arrange
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();
		Assert.Empty(navigation.ModalStack);

		// Act
		popupService.ShowPopup<MockSelfClosingPopup>(navigation, PopupOptions.Empty);

		// Assert
		Assert.Single(navigation.ModalStack);
	}

	[Fact]
	public void ShowPopup_MultiplePopupsDisplayed()
	{
		// Arrange
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		// Act
		popupService.ShowPopup<MockSelfClosingPopup>(navigation, PopupOptions.Empty);
		popupService.ShowPopup<MockSelfClosingPopup>(navigation, PopupOptions.Empty);

		// Assert
		Assert.Equal(2, navigation.ModalStack.Count);
	}

	[Fact]
	public void ShowPopupAsync_WithCustomOptions_AppliesOptions()
	{
		// Arrange
		var onTappingOutsideOfPopup = () => { };

		var popupService = ServiceProvider.GetRequiredService<IPopupService>();
		var options = new PopupOptions
		{
			PageOverlayColor = Colors.Red,
			CanBeDismissedByTappingOutsideOfPopup = false,
			Shape = new Ellipse(),
			OnTappingOutsideOfPopup = onTappingOutsideOfPopup
		};

		// Act
		popupService.ShowPopup<MockSelfClosingPopup>(
			navigation,
			options);

		var popupPage = (PopupPage)navigation.ModalStack[0];
		var popupPageLayout = popupPage.Content;
		var border = popupPageLayout.Border;
		var popup = border.Content;

		// Assert
		Assert.NotNull(popup);

		Assert.Equal(popup.HorizontalOptions, border.HorizontalOptions);
		Assert.Equal(popup.VerticalOptions, border.VerticalOptions);
		Assert.Equal(popup.Margin, border.Margin);
		Assert.Equal(border.Padding, border.Padding);
		Assert.Equal(options.PageOverlayColor, popupPage.BackgroundColor);
		Assert.Equal(options.Shape, border.StrokeShape);
		Assert.Equal(popup.BindingContext, border.BindingContext);
		Assert.Equal(popupPageLayout.BindingContext, border.BindingContext);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsyncWithNotRegisteredServiceShouldThrowInvalidOperationException()
	{
		// Arrange
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		// Act // Assert
		await Assert.ThrowsAsync<InvalidOperationException>(() => popupService.ShowPopupAsync<INotifyPropertyChanged>(page.Navigation, PopupOptions.Empty, TestContext.Current.CancellationToken));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_CancellationTokenExpired()
	{
		// Arrange
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Act
		await Task.Delay(100, CancellationToken.None);

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		// Assert
		await Assert.ThrowsAsync<OperationCanceledException>(() => popupService.ShowPopupAsync<MockPageViewModel>(page.Navigation, PopupOptions.Empty, cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_CancellationTokenCanceled()
	{
		// Arrange
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Act
		await cts.CancelAsync();

		// Assert
		await Assert.ThrowsAsync<OperationCanceledException>(() => popupService.ShowPopupAsync<MockPageViewModel>(navigation, PopupOptions.Empty, cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShowPopupAsyncShouldValidateProperBindingContext()
	{
		// Arrange
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();
		var popupInstance = ServiceProvider.GetRequiredService<MockSelfClosingPopup>();
		var popupViewModel = ServiceProvider.GetRequiredService<MockPageViewModel>();

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		// Act
		await popupService.ShowPopupAsync<MockPageViewModel, object?>(page.Navigation, PopupOptions.Empty, TestContext.Current.CancellationToken);


		// Assert
		Assert.Same(popupInstance.BindingContext, popupViewModel);
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public async Task ShowPopupAsyncShouldReturnResultOnceClosed()
	{
		// Arrange
		var mockPopup = ServiceProvider.GetRequiredService<MockSelfClosingPopup>();
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		// Act
		var result = await popupService.ShowPopupAsync<MockPageViewModel, object?>(page.Navigation, PopupOptions.Empty, CancellationToken.None);

		// Assert
		Assert.Same(mockPopup.Result, result.Result);
		Assert.False(result.WasDismissedByTappingOutsideOfPopup);
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShowPopupTAsyncShouldReturnResultOnceClosed()
	{
		// Arrange
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		// Act
		var result = await popupService.ShowPopupAsync<MockPageViewModel>(page.Navigation, PopupOptions.Empty, CancellationToken.None);

		// Assert
		Assert.False(result.WasDismissedByTappingOutsideOfPopup);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_UsingNavigation_ShouldThrowArgumentNullException_WhenNavigationIsNull()
	{
		// Arrange
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		// Act // Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		await Assert.ThrowsAsync<ArgumentNullException>(() => popupService.ShowPopupAsync<MockPageViewModel>((INavigation?)null, PopupOptions.Empty, CancellationToken.None));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_UsingPage_ShouldThrowArgumentNullException_WhenNavigationIsNull()
	{
		// Arrange
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		// Act // Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		await Assert.ThrowsAsync<ArgumentNullException>(() => popupService.ShowPopupAsync<MockPageViewModel>((Page?)null, PopupOptions.Empty, CancellationToken.None));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact]
	public void ShowPopup_UsingPage_ShouldThrowArgumentNullException_WhenNavigationIsNull()
	{
		// Arrange
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();
		Page? page = null;

		// Act // Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => popupService.ShowPopup<MockPageViewModel>((Page?)null, PopupOptions.Empty));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ClosePopupAsync_UsingPage_ShouldThrowArgumentNullException_WhenPageIsNull()
	{
		// Arrange
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();
		Page? page = null;

		// Act // Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		await Assert.ThrowsAsync<ArgumentNullException>(() => popupService.ClosePopupAsync((Page?)null, TestContext.Current.CancellationToken));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ClosePopupAsyncT_UsingPage_ShouldThrowArgumentNullException_WhenPageIsNull()
	{
		// Arrange
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();
		Page? page = null;

		// Act // Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		await Assert.ThrowsAsync<ArgumentNullException>(() => popupService.ClosePopupAsync<int>((Page?)null, 2, TestContext.Current.CancellationToken));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ClosePopupAsync_UsingNavigation_ShouldThrowArgumentNullException_WhenNavigationIsNull()
	{
		// Arrange
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		// Act // Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		await Assert.ThrowsAsync<ArgumentNullException>(() => popupService.ClosePopupAsync((INavigation?)null, TestContext.Current.CancellationToken));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ClosePopupAsyncT_UsingNavigation_ShouldThrowArgumentNullException_WhenNavigationIsNull()
	{
		// Arrange
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();
		Page? page = null;

		// Act // Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		await Assert.ThrowsAsync<ArgumentNullException>(() => popupService.ClosePopupAsync((INavigation?)null, 2, TestContext.Current.CancellationToken));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_ShouldThrowArgumentNullException_WhenViewModelIsNull()
	{
		// Arrange
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		// Act // Assert
		await Assert.ThrowsAsync<InvalidOperationException>(() => popupService.ShowPopupAsync<object>(page.Navigation, PopupOptions.Empty, CancellationToken.None));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_ShouldReturnDefaultResult_WhenPopupIsClosedWithoutResult()
	{
		// Arrange
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();
		var mockPopup = ServiceProvider.GetRequiredService<MockSelfClosingPopup>();

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		// Act
		var result = await popupService.ShowPopupAsync<MockPageViewModel, object?>(page.Navigation, PopupOptions.Empty, CancellationToken.None);

		// Assert
		Assert.Equal(mockPopup.Result, result.Result);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ClosePopupAsync_ShouldClosePopupUsingNavigationAndReturnResult()
	{
		// Arrange
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();


		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		// Act
		popupService.ShowPopup<MockPopup>(page.Navigation);

		// Assert
		Assert.Single(page.Navigation.ModalStack);
		Assert.IsType<PopupPage>(page.Navigation.ModalStack[0]);

		// Act 
		var popupResult = await popupService.ClosePopupAsync(page.Navigation, TestContext.Current.CancellationToken);

		// Assert
		Assert.Empty(page.Navigation.ModalStack);
		Assert.False(popupResult.WasDismissedByTappingOutsideOfPopup);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ClosePopupAsync_ShouldClosePopupUsingPageAndReturnResult()
	{
		// Arrange
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();


		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		// Act
		popupService.ShowPopup<MockPopup>(page);

		// Assert
		Assert.Single(page.Navigation.ModalStack);
		Assert.IsType<PopupPage>(page.Navigation.ModalStack[0]);

		// Act 
		var popupResult = await popupService.ClosePopupAsync(page, TestContext.Current.CancellationToken);

		// Assert
		Assert.Empty(page.Navigation.ModalStack);
		Assert.False(popupResult.WasDismissedByTappingOutsideOfPopup);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ClosePopupAsyncT_ShouldClosePopupUsingNavigationAndReturnResult()
	{
		// Arrange
		const int expectedResult = 2;
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		// Act
		popupService.ShowPopup<MockPopup>(page.Navigation);

		// Assert
		Assert.Single(page.Navigation.ModalStack);
		Assert.IsType<PopupPage>(page.Navigation.ModalStack[0]);

		// Act 
		var popupResult = await popupService.ClosePopupAsync(page.Navigation, expectedResult, TestContext.Current.CancellationToken);

		// Assert
		Assert.Empty(page.Navigation.ModalStack);
		Assert.Equal(expectedResult, popupResult.Result);
		Assert.False(popupResult.WasDismissedByTappingOutsideOfPopup);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ClosePopupAsyncT_ShouldClosePopupUsingPageAndReturnResult()
	{
		// Arrange
		const int expectedResult = 2;
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		// Act
		popupService.ShowPopup<MockPopup>(page);

		// Assert
		Assert.Single(page.Navigation.ModalStack);
		Assert.IsType<PopupPage>(page.Navigation.ModalStack[0]);

		// Act 
		var popupResult = await popupService.ClosePopupAsync(page.Navigation, expectedResult, TestContext.Current.CancellationToken);

		// Assert
		Assert.Empty(page.Navigation.ModalStack);
		Assert.Equal(expectedResult, popupResult.Result);
		Assert.False(popupResult.WasDismissedByTappingOutsideOfPopup);
	}
}

sealed class MockSelfClosingPopup : Popup<object?>, IQueryAttributable
{
	public const int ExpectedResult = 2;

	public MockSelfClosingPopup(MockPageViewModel viewModel, object? result = null)
	{
		BackgroundColor = DefaultBackgroundColor;
		BindingContext = viewModel;
		Result = result;
		Opened += HandlePopupOpened;
	}

	public static Color DefaultBackgroundColor { get; } = Colors.White;

	void HandlePopupOpened(object? sender, EventArgs e)
	{
		var timer = Dispatcher.CreateTimer();
		timer.Interval = TimeSpan.FromMilliseconds(500);
		timer.Tick += HandleTick;
		timer.Start();

		async void HandleTick(object? sender, EventArgs e)
		{
			timer.Tick -= HandleTick;
			try
			{
				await CloseAsync(Result);
			}
			catch (InvalidOperationException)
			{
				// If test has already ended, Popup.CloseAsync will throw an InvalidOperationException
				// because all Popups are removed from ModalStack in BaseHandlerTest.DisposeAsyncCore()
			}
		}
	}

	public object? Result { get; } = ExpectedResult;

	void IQueryAttributable.ApplyQueryAttributes(IDictionary<string, object> query)
	{
		BackgroundColor = (Color)query[nameof(BackgroundColor)];
	}
}

sealed class MockPopup : Popup;

sealed file class PopupViewModel : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler? PropertyChanged;

	public Color? Color
	{
		get;
		set
		{
			if (!Equals(value, field))
			{
				field = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Color)));
			}
		}
	} = new();
}