using System.ComponentModel;
using CommunityToolkit.Maui.Services;
using CommunityToolkit.Maui.UnitTests.Mocks;
using CommunityToolkit.Maui.Views;
using FluentAssertions;
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

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_WithViewType_ShowsPopup()
	{
		// Arrange
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		// Act
		await popupService.ShowPopupAsync<MockSelfClosingPopup>(navigation, cancellationToken: CancellationToken.None);

		// Assert
		Assert.Single(navigation.ModalStack);
		Assert.IsType<PopupContainer>(navigation.ModalStack[0]);
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

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_WithCustomOptions_AppliesOptions()
	{
		// Arrange
		var onTappingOutsideOfPopup = () => { };

		var popupService = ServiceProvider.GetRequiredService<IPopupService>();
		var options = new PopupOptions
		{
			BackgroundColor = Colors.Red,
			CanBeDismissedByTappingOutsideOfPopup = false,
			HorizontalOptions = LayoutOptions.Start,
			VerticalOptions = LayoutOptions.End,
			Padding = new Thickness(1,2,3,4),
			Margin = new Thickness(5,6,7,8),
			Shape = new Ellipse(),
			OnTappingOutsideOfPopup = onTappingOutsideOfPopup
		};

		// Act
		var result = await popupService.ShowPopupAsync<MockSelfClosingPopup>(
			navigation,
			options,
			CancellationToken.None);

		var popupContainer = (PopupContainer)navigation.ModalStack[0];
		var popupContainerContent = popupContainer.Content;
		var border = (Border)popupContainerContent.Children[0];
		var popup = border.Content;

		// Assert
		Assert.NotNull(popup);

		Assert.Equal(options.BackgroundColor, border.BackgroundColor);
		Assert.Equal(options.HorizontalOptions, border.HorizontalOptions);
		Assert.Equal(options.VerticalOptions, border.VerticalOptions);
		Assert.Equal(options.Shape, border.StrokeShape);
		Assert.Equal(options.Margin, border.Margin);
		Assert.Equal(options.Padding, border.Padding);
		Assert.Equal(popup.BindingContext, border.BindingContext);
		Assert.Equal(popupContainerContent.BindingContext, border.BindingContext);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsyncWithNotRegisteredServiceShouldThrowInvalidOperationException()
	{
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		await Assert.ThrowsAsync<InvalidOperationException>(() => popupService.ShowPopupAsync<INotifyPropertyChanged>(page.Navigation, PopupOptions.Empty, TestContext.Current.CancellationToken));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_CancellationTokenExpired()
	{
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Ensure CancellationToken has expired
		await Task.Delay(100, CancellationToken.None);

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		await Assert.ThrowsAsync<OperationCanceledException>(() => popupService.ShowPopupAsync<MockPageViewModel>(page.Navigation, PopupOptions.Empty, cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_CancellationTokenCanceled()
	{
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Ensure CancellationToken has expired
		await cts.CancelAsync();

		await Assert.ThrowsAsync<OperationCanceledException>(() => popupService.ShowPopupAsync<MockPageViewModel>(Application.Current!.Windows[0].Page!.Navigation, PopupOptions.Empty, cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShowPopupAsyncShouldValidateProperBindingContext()
	{
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();
		var popupInstance = ServiceProvider.GetRequiredService<MockSelfClosingPopup>();
		var popupViewModel = ServiceProvider.GetRequiredService<MockPageViewModel>();

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		await popupService.ShowPopupAsync<MockPageViewModel, object?>(page.Navigation, PopupOptions.Empty, TestContext.Current.CancellationToken);

		Assert.Same(popupInstance.BindingContext, popupViewModel);
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShowPopupAsyncShouldReturnResultOnceClosed()
	{
		var mockPopup = ServiceProvider.GetRequiredService<MockSelfClosingPopup>();
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		var result = await popupService.ShowPopupAsync<MockPageViewModel, object?>(page.Navigation, PopupOptions.Empty, CancellationToken.None);

		Assert.Same(mockPopup.Result, result.Result);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_ShouldThrowArgumentNullException_WhenNavigationIsNull()
	{
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		await Assert.ThrowsAsync<ArgumentNullException>(() => popupService.ShowPopupAsync<MockPageViewModel>(null, PopupOptions.Empty, CancellationToken.None));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_ShouldThrowArgumentNullException_WhenPopupOptionsIsNull()
	{
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		await Assert.ThrowsAsync<ArgumentNullException>(() => popupService.ShowPopupAsync<MockPageViewModel>(page.Navigation, null, CancellationToken.None));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_ShouldThrowArgumentNullException_WhenViewModelIsNull()
	{
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		await Assert.ThrowsAsync<ArgumentNullException>(() => popupService.ShowPopupAsync<object>(page.Navigation, PopupOptions.Empty, CancellationToken.None));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_ShouldReturnDefaultResult_WhenPopupIsClosedWithoutResult()
	{
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();
		var mockPopup = ServiceProvider.GetRequiredService<MockSelfClosingPopup>();

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		var result = await popupService.ShowPopupAsync<MockPageViewModel, object?>(page.Navigation, PopupOptions.Empty, CancellationToken.None);

		result.Result.Should().BeNull();
	}
}

sealed class MockSelfClosingPopup : Popup<object?>
{
	public MockSelfClosingPopup(MockPageViewModel viewModel, object? result = null)
	{
		BindingContext = viewModel;
		Result = result;
		Opened += MockSelfClosingPopupOpened;
	}

	void MockSelfClosingPopupOpened(object? sender, EventArgs e)
	{
		var timer = Dispatcher.CreateTimer();
		timer.Interval = TimeSpan.FromMilliseconds(500);
		timer.Tick += async (_, _) => await Close(Result);
		timer.Start();
	}

	public object? Result { get; }
}

sealed file class MockPopup : Popup
{
}

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
