using System.ComponentModel;
using CommunityToolkit.Maui.Services;
using CommunityToolkit.Maui.UnitTests.Mocks;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Shapes;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Services;

public class PopupServiceTests : BaseViewTest
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
		popupService.ShowPopup<MockPageViewModel>(navigation);

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
		popupService.ShowPopup<MockPopup>(page);

		// Assert
		Assert.Single(navigation.ModalStack);
		Assert.IsType<PopupPage>(navigation.ModalStack[0]);
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public async Task ShowPopupAsync_AwaitingShowPopupAsync_EnsurePreviousPopupClosed()
	{
		// Arrange
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		// Act
		await popupService.ShowPopupAsync<ShortLivedSelfClosingPopup>(navigation, PopupOptions.Empty, TestContext.Current.CancellationToken);
		await popupService.ShowPopupAsync<LongLivedSelfClosingPopup>(navigation, PopupOptions.Empty, TestContext.Current.CancellationToken);

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
		await popupService.ShowPopupAsync<ShortLivedMockPageViewModel>(page, PopupOptions.Empty, TestContext.Current.CancellationToken);
		await popupService.ShowPopupAsync<LongLivedMockPageViewModel>(page, PopupOptions.Empty, TestContext.Current.CancellationToken);

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
		popupService.ShowPopup<MockPopup>(navigation, PopupOptions.Empty);

		// Assert
		Assert.Single(navigation.ModalStack);
	}

	[Fact]
	public void ShowPopup_MultiplePopupsDisplayed()
	{
		// Arrange
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		// Act
		popupService.ShowPopup<MockPopup>(navigation, PopupOptions.Empty);
		popupService.ShowPopup<MockPopup>(navigation, PopupOptions.Empty);

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
		popupService.ShowPopup<MockPopup>(navigation, options);

		var popupPage = (PopupPage)navigation.ModalStack[0];
		var popupPageLayout = popupPage.Content;
		var border = popupPageLayout.PopupBorder;
		var popup = (MockPopup)(border.Content ?? throw new InvalidOperationException("Content cannot be null"));

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
		await Task.Delay(100, TestContext.Current.CancellationToken);

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		// Assert
		await Assert.ThrowsAsync<OperationCanceledException>(() => popupService.ShowPopupAsync<ShortLivedMockPageViewModel>(page.Navigation, PopupOptions.Empty, cts.Token));
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
		await Assert.ThrowsAsync<OperationCanceledException>(() => popupService.ShowPopupAsync<ShortLivedMockPageViewModel>(navigation, PopupOptions.Empty, cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShowPopupAsyncShouldValidateProperBindingContext()
	{
		// Arrange
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();
		var popupInstance = ServiceProvider.GetRequiredService<LongLivedSelfClosingPopup>();

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		// Act
		await popupService.ShowPopupAsync<LongLivedMockPageViewModel, object?>(page.Navigation, PopupOptions.Empty, TestContext.Current.CancellationToken);

		// Assert
		Assert.NotNull(popupInstance.BindingContext);
		Assert.IsType<LongLivedMockPageViewModel>(popupInstance.BindingContext);
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public async Task ShowPopupAsyncShouldReturnResultOnceClosed()
	{
		// Arrange
		var mockPopup = ServiceProvider.GetRequiredService<ShortLivedSelfClosingPopup>();
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		// Act
		var result = await popupService.ShowPopupAsync<ShortLivedMockPageViewModel, object?>(page.Navigation, PopupOptions.Empty, TestContext.Current.CancellationToken);

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
		var result = await popupService.ShowPopupAsync<ShortLivedMockPageViewModel>(page.Navigation, PopupOptions.Empty, TestContext.Current.CancellationToken);

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
		await Assert.ThrowsAsync<ArgumentNullException>(() => popupService.ShowPopupAsync<ShortLivedMockPageViewModel>((INavigation?)null, PopupOptions.Empty, TestContext.Current.CancellationToken));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_UsingPage_ShouldThrowArgumentNullException_WhenNavigationIsNull()
	{
		// Arrange
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		// Act // Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		await Assert.ThrowsAsync<ArgumentNullException>(() => popupService.ShowPopupAsync<ShortLivedMockPageViewModel>((Page?)null, PopupOptions.Empty, TestContext.Current.CancellationToken));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact]
	public void ShowPopup_UsingPage_ShouldThrowArgumentNullException_WhenNavigationIsNull()
	{
		// Arrange
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		// Act // Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => popupService.ShowPopup<ShortLivedMockPageViewModel>((Page?)null, PopupOptions.Empty));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ClosePopupAsync_UsingPage_ShouldThrowArgumentNullException_WhenPageIsNull()
	{
		// Arrange
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

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

		// Act // Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		await Assert.ThrowsAsync<ArgumentNullException>(() => popupService.ClosePopupAsync((Page?)null, 2, TestContext.Current.CancellationToken));
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
		await Assert.ThrowsAsync<InvalidOperationException>(() => popupService.ShowPopupAsync<object>(page.Navigation, PopupOptions.Empty, TestContext.Current.CancellationToken));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_ShouldReturnDefaultResult_WhenPopupIsClosedWithoutResult()
	{
		// Arrange
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();
		var mockPopup = ServiceProvider.GetRequiredService<ShortLivedSelfClosingPopup>();

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		// Act
		var result = await popupService.ShowPopupAsync<ShortLivedMockPageViewModel, object?>(page.Navigation, PopupOptions.Empty, TestContext.Current.CancellationToken);

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

class GarbageCollectionHeavySelfClosingPopup(MockPageViewModel viewModel, object? result = null) : MockSelfClosingPopup(viewModel, TimeSpan.FromMilliseconds(500), result)
{
	protected override void HandlePopupOpened(object? sender, EventArgs e)
	{
		GC.Collect(); // Run Garbage Collection before closing the popup

		base.HandlePopupOpened(sender, e); // Closes the popup
	}

	protected override void HandlePopupClosed(object? sender, EventArgs e)
	{
		base.HandlePopupClosed(sender, e);

		GC.Collect(); // Run Garbage collection again after closing the Popup
	}
}

sealed class LongLivedSelfClosingPopup(LongLivedMockPageViewModel viewModel) : MockSelfClosingPopup(viewModel, TimeSpan.FromMilliseconds(1500), "Long Lived");

sealed class ShortLivedSelfClosingPopup(ShortLivedMockPageViewModel viewModel) : MockSelfClosingPopup(viewModel, TimeSpan.FromMilliseconds(500), "Short Lived");

class MockSelfClosingPopup : Popup<object?>, IQueryAttributable, IDisposable
{
	readonly TaskCompletionSource popupClosedTCS = new();

	CancellationTokenSource? cancellationTokenSource;

	protected MockSelfClosingPopup(MockPageViewModel viewModel, TimeSpan displayDuration, object? result = null)
	{
		BackgroundColor = DefaultBackgroundColor;
		DisplayDuration = displayDuration;
		BindingContext = viewModel;
		Result = result;
		Opened += HandlePopupOpened;
		Closed += HandlePopupClosed;
	}

	~MockSelfClosingPopup()
	{
		Dispose(false);
	}

	public object? Result { get; }

	public TimeSpan DisplayDuration { get; }

	public static Color DefaultBackgroundColor { get; } = Colors.White;

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void HandlePopupClosed(object? sender, EventArgs e)
	{
		cancellationTokenSource?.Cancel();
	}

	protected virtual async void HandlePopupOpened(object? sender, EventArgs e)
	{
		if (cancellationTokenSource is not null)
		{
			await cancellationTokenSource.CancelAsync();
		}

		cancellationTokenSource = new CancellationTokenSource();

		Console.WriteLine($@"{DateTime.Now:O} HandlePopupOpened {BindingContext.GetType().Name}");

		await Task.Delay(DisplayDuration);

		if (cancellationTokenSource?.IsCancellationRequested is true)
		{
			return;
		}

		Console.WriteLine(
			$@"{DateTime.Now:O} Closing {BindingContext.GetType().Name} - {Application.Current?.Windows[0].Page?.Navigation.ModalStack.Count}");

		await CloseAsync(Result, cancellationTokenSource?.Token ?? TestContext.Current.CancellationToken);

		Console.WriteLine(
			$@"{DateTime.Now:O} Closed {BindingContext.GetType().Name} - {Application.Current?.Windows[0].Page?.Navigation.ModalStack.Count}");

		popupClosedTCS.SetResult();
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			cancellationTokenSource?.Dispose();
		}
	}

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