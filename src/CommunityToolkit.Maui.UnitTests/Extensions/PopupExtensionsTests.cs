using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
using CommunityToolkit.Maui.UnitTests.Services;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Shapes;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions;

public class PopupExtensionsTests : BaseHandlerTest
{
	readonly INavigation navigation;

	public PopupExtensionsTests()
	{
		var page = new MockPage(new MockPageViewModel());
		Assert.NotNull(Application.Current);

		Application.Current.Windows[0].Page = page;
		navigation = page.Navigation;
	}

	[Fact]
	public void ShowPopupAsync_WithPopupType_ShowsPopup()
	{
		// Arrange
		var selfClosingPopup = ServiceProvider.GetRequiredService<MockSelfClosingPopup>() ?? throw new InvalidOperationException();

		// Act
		navigation.ShowPopup(selfClosingPopup);

		// Assert
		Assert.Single(navigation.ModalStack);
		Assert.IsType<PopupContainer>(navigation.ModalStack[0]);
	}
	
	[Fact]
	public void ShowPopupAsync_WithViewType_ShowsPopup()
	{
		// Arrange
		var view = new Grid();

		// Act
		navigation.ShowPopup(view);

		// Assert
		Assert.Single(navigation.ModalStack);
		Assert.IsType<PopupContainer>(navigation.ModalStack[0]);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_AwaitingShowPopupAsync_EnsurePreviousPopupClosed()
	{
		// Arrange
		var selfClosingPopup = ServiceProvider.GetRequiredService<MockSelfClosingPopup>() ?? throw new InvalidOperationException();

		// Act
		await navigation.ShowPopupAsync<object?>(selfClosingPopup, PopupOptions.Empty, CancellationToken.None);
		await navigation.ShowPopupAsync<object?>(selfClosingPopup, PopupOptions.Empty, CancellationToken.None);

		// Assert
		Assert.Empty(navigation.ModalStack);
	}
	
	[Fact]
	public void ShowPopup_NavigationModalStackCountIncreases()
	{
		// Arrange
		var selfClosingPopup = ServiceProvider.GetRequiredService<MockSelfClosingPopup>() ?? throw new InvalidOperationException();
		Assert.Empty(navigation.ModalStack);

		// Act
		navigation.ShowPopup(selfClosingPopup, PopupOptions.Empty);

		// Assert
		Assert.Single(navigation.ModalStack);
	}
	
	[Fact]
	public void ShowPopupWithView_NavigationModalStackCountIncreases()
	{
		// Arrange
		var view = new Grid();
		Assert.Empty(navigation.ModalStack);

		// Act
		navigation.ShowPopup(view, PopupOptions.Empty);

		// Assert
		Assert.Single(navigation.ModalStack);
	}
	
	[Fact]
	public void ShowPopup_MultiplePopupsDisplayed()
	{
		// Arrange
		var selfClosingPopup = ServiceProvider.GetRequiredService<MockSelfClosingPopup>() ?? throw new InvalidOperationException();

		// Act
		navigation.ShowPopup(selfClosingPopup, PopupOptions.Empty);
		navigation.ShowPopup(selfClosingPopup, PopupOptions.Empty);

		// Assert
		Assert.Equal(2, navigation.ModalStack.Count);
	}
	
	[Fact]
	public void ShowPopupView_MultiplePopupsDisplayed()
	{
		// Arrange
		var view = new Grid();

		// Act
		navigation.ShowPopup(view, PopupOptions.Empty);
		navigation.ShowPopup(view, PopupOptions.Empty);

		// Assert
		Assert.Equal(2, navigation.ModalStack.Count);
	}

	[Fact]
	public void ShowPopupAsync_WithCustomOptions_AppliesOptions()
	{
		// Arrange
		var onTappingOutsideOfPopup = () => { };

		var selfClosingPopup = ServiceProvider.GetRequiredService<MockSelfClosingPopup>() ?? throw new InvalidOperationException();
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
		navigation.ShowPopup(selfClosingPopup, options);

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
	
	[Fact]
	public void ShowPopupAsyncWithView_WithCustomOptions_AppliesOptions()
	{
		// Arrange
		var onTappingOutsideOfPopup = () => { };

		var view = new Grid();
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
		navigation.ShowPopup(view, options);

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
	public async Task ShowPopupAsync_CancellationTokenExpired()
	{
		var selfClosingPopup = ServiceProvider.GetRequiredService<MockSelfClosingPopup>() ?? throw new InvalidOperationException();

		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Ensure CancellationToken has expired
		await Task.Delay(100, CancellationToken.None);

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		await Assert.ThrowsAsync<OperationCanceledException>(() => navigation.ShowPopupAsync(selfClosingPopup, PopupOptions.Empty, cts.Token));
	}
	
	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsyncWithView_CancellationTokenExpired()
	{
		var view = new Grid();

		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Ensure CancellationToken has expired
		await Task.Delay(100, CancellationToken.None);

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		await Assert.ThrowsAsync<OperationCanceledException>(() => navigation.ShowPopupAsync(view, PopupOptions.Empty, cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_CancellationTokenCanceled()
	{
		var selfClosingPopup = ServiceProvider.GetRequiredService<MockSelfClosingPopup>() ?? throw new InvalidOperationException();

		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Ensure CancellationToken has expired
		await cts.CancelAsync();

		await Assert.ThrowsAsync<OperationCanceledException>(() => navigation.ShowPopupAsync(selfClosingPopup, PopupOptions.Empty, cts.Token));
	}
	
	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsyncWithView_CancellationTokenCanceled()
	{
		var view = new Grid();

		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Ensure CancellationToken has expired
		await cts.CancelAsync();

		await Assert.ThrowsAsync<OperationCanceledException>(() => navigation.ShowPopupAsync(view, PopupOptions.Empty, cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShowPopupAsync_ShouldValidateProperBindingContext()
	{
		var selfClosingPopup = ServiceProvider.GetRequiredService<MockSelfClosingPopup>() ?? throw new InvalidOperationException();
		var popupInstance = ServiceProvider.GetRequiredService<MockSelfClosingPopup>();
		var popupViewModel = ServiceProvider.GetRequiredService<MockPageViewModel>();

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		await navigation.ShowPopupAsync<object?>(selfClosingPopup, PopupOptions.Empty, TestContext.Current.CancellationToken);

		Assert.Same(popupInstance.BindingContext, popupViewModel);
	}
	
	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShowPopupAsyncWithView_ShouldValidateProperBindingContext()
	{
		var view = new Grid();
		var popupInstance = ServiceProvider.GetRequiredService<MockSelfClosingPopup>();
		var popupViewModel = ServiceProvider.GetRequiredService<MockPageViewModel>();

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		var showPopupTask = navigation.ShowPopupAsync<object?>(view, PopupOptions.Empty, TestContext.Current.CancellationToken);
		
		var popupContainer = (PopupContainer)navigation.ModalStack[0];
		await popupContainer.Close(new PopupResult<object?>(null, false), TestContext.Current.CancellationToken);
		
		await showPopupTask;

		Assert.Same(popupInstance.BindingContext, popupViewModel);
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShowPopupAsync_ShouldReturnResultOnceClosed()
	{
		var mockPopup = ServiceProvider.GetRequiredService<MockSelfClosingPopup>();
		var selfClosingPopup = ServiceProvider.GetRequiredService<MockSelfClosingPopup>() ?? throw new InvalidOperationException();

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		var result = await navigation.ShowPopupAsync<object?>(selfClosingPopup, PopupOptions.Empty, CancellationToken.None);

		Assert.Same(mockPopup.Result, result.Result);
		Assert.False(result.WasDismissedByTappingOutsideOfPopup);
	}
	
	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShowPopupAsyncWithView_ShouldReturnResultOnceClosed()
	{
		const int popupResultValue = 2;
		
		var view = new Grid();
		var expectedPopupResult = new PopupResult<int>(popupResultValue, false);

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		var showPopupTask = navigation.ShowPopupAsync<int>(view, PopupOptions.Empty, CancellationToken.None);

		var popupContainer = (PopupContainer)navigation.ModalStack[0];
		await popupContainer.Close(expectedPopupResult, TestContext.Current.CancellationToken);
		
		var actualPopupResult = await showPopupTask;

		Assert.Same(expectedPopupResult, actualPopupResult);
		Assert.False(expectedPopupResult.WasDismissedByTappingOutsideOfPopup);
		Assert.Equal(popupResultValue, expectedPopupResult.Result);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_ShouldThrowArgumentNullException_WhenViewIsNull()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		await Assert.ThrowsAsync<ArgumentNullException>(() => navigation.ShowPopupAsync(null, PopupOptions.Empty, CancellationToken.None));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}
	
	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_ShouldThrowArgumentNullException_WhenNavigationIsNull()
	{
		var selfClosingPopup = ServiceProvider.GetRequiredService<MockSelfClosingPopup>() ?? throw new InvalidOperationException();
		
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		await Assert.ThrowsAsync<ArgumentNullException>(() => PopupExtensions.ShowPopupAsync((INavigation?)null, selfClosingPopup, PopupOptions.Empty, CancellationToken.None));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_ShouldReturnDefaultResult_WhenPopupIsClosedWithoutResult()
	{
		var selfClosingPopup = ServiceProvider.GetRequiredService<MockSelfClosingPopup>() ?? throw new InvalidOperationException();
		var mockPopup = ServiceProvider.GetRequiredService<MockSelfClosingPopup>();

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		var result = await navigation.ShowPopupAsync<object?>(selfClosingPopup, PopupOptions.Empty, CancellationToken.None);

		Assert.Equal(mockPopup.Result, result.Result);
	}
}