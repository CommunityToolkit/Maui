using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.UnitTests.Extensions;
using CommunityToolkit.Maui.UnitTests.Services;
using CommunityToolkit.Maui.Views;
using FluentAssertions;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using Microsoft.Maui.Controls.Shapes;
using Xunit;
using Application = Microsoft.Maui.Controls.Application;
using Page = Microsoft.Maui.Controls.Page;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class PopupPageTests : BaseHandlerTest
{
	[Fact]
	public void Constructor_ShouldThrowArgumentNullException_WhenPopupIsNull()
	{
		// Arrange
		var popupOptions = new MockPopupOptions();
		var taskCompletionSource = new TaskCompletionSource<PopupResult>();

		// Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Action act = () => new PopupPage(null, popupOptions);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		// Assert
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void Constructor_ShouldThrowArgumentNullException_WhenPopupOptionsIsNull()
	{
		// Arrange
		var view = new ContentView();
		var taskCompletionSource = new TaskCompletionSource<PopupResult>();

		// Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Action act = () => new PopupPage(view, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		// Assert
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public async Task Close_ShouldThrowInvalidOperationException_NoPopupPageFound()
	{
		// Arrange
		var view = new ContentView();
		var popupOptions = new MockPopupOptions();
		var popupPage = new PopupPage<string>(view, popupOptions);

		// Act / Assert
		await Assert.ThrowsAsync<PopupNotFoundException>(async () => await popupPage.CloseAsync(new PopupResult(false), CancellationToken.None));
		await Assert.ThrowsAnyAsync<InvalidOperationException>(async () => await popupPage.CloseAsync(new PopupResult(false), CancellationToken.None));
	}

	[Fact]
	public async Task Close_ShouldSetResultAndPopModalAsync()
	{
		// Arrange
		var tcs = new TaskCompletionSource<IPopupResult>();
		var view = new ContentView();
		var popupOptions = new MockPopupOptions();
		var popupPage = new PopupPage(view, popupOptions);
		var expectedResult = new PopupResult(false);

		popupPage.PopupClosed += HandlePopupClosed;

		// Act
		if (Application.Current?.Windows[0].Page?.Navigation is not INavigation navigation)
		{
			throw new InvalidOperationException("Unable to locate Navigation page");
		}

		await navigation.PushModalAsync(popupPage);

		await popupPage.CloseAsync(expectedResult, CancellationToken.None);
		var actualResult = await tcs.Task;

		// Assert
		actualResult.Should().Be(expectedResult);

		void HandlePopupClosed(object? sender, IPopupResult e)
		{
			tcs.SetResult(e);
		}
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task Close_ShouldThrowOperationCanceledException_WhenTokenIsCancelled_NavigationModalStackShouldStillContainPopupPage()
	{
		// Arrange
		PopupPage popupPage;
		var view = new ContentView();
		var result = new PopupResult(false);
		var cts = new CancellationTokenSource();
		if (Application.Current?.Windows[0].Page is not Page mainPage)
		{
			throw new InvalidOperationException("Failed to locate main page");
		}

		// Act
		mainPage.Navigation.ShowPopup(view);
		popupPage = mainPage.Navigation.ModalStack.OfType<PopupPage>().Single();

		await cts.CancelAsync();

		// Assert
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => popupPage.CloseAsync(result, cts.Token));
		Assert.Single(mainPage.Navigation.ModalStack.OfType<PopupPage>());
	}

	[Fact]
	public void PopupPageT_Constructor_ShouldThrowArgumentNullException_WhenPopupIsNull()
	{
		// Arrange
		var popupOptions = new MockPopupOptions();

		// Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Action act = () => new PopupPage<string>(null, popupOptions);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		// Assert
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void PopupPageT_Constructor_ShouldThrowArgumentNullException_WhenPopupOptionsIsNull()
	{
		// Arrange
		var view = new ContentView();

		// Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Action act = () => new PopupPage<string>(view, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		// Assert
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public async Task PopupPageT_Close_ShouldSetResultAndPopModalAsync()
	{
		// Arrange
		var view = new ContentView();
		var popupOptions = new MockPopupOptions();
		var taskCompletionSource = new TaskCompletionSource<PopupResult<string>>();
		var popupPage = new PopupPage<string>(view, popupOptions);
		var expectedResult = new PopupResult<string>("Test", false);

		popupPage.PopupClosed += HandlePopupClosed;

		// Act
		if (Application.Current?.Windows[0].Page?.Navigation is not INavigation navigation)
		{
			throw new InvalidOperationException("Unable to locate Navigation page");
		}

		await navigation.PushModalAsync(popupPage);

		await popupPage.Close(expectedResult, CancellationToken.None);
		var actualResult = await taskCompletionSource.Task;

		// Assert
		actualResult.Should().Be(expectedResult);

		void HandlePopupClosed(object? sender, IPopupResult e)
		{
			taskCompletionSource.SetResult((PopupResult<string>)e);
		}
	}

	[Fact]
	public async Task PopupPageT_CloseAfterAdditionalModalPage_ShouldThrowInvalidOperationException()
	{
		// Arrange
		var view = new ContentView();
		var popupOptions = new MockPopupOptions();
		var popupPage = new PopupPage<string>(view, popupOptions);

		// Act
		if (Application.Current?.Windows[0].Page?.Navigation is not INavigation navigation)
		{
			throw new InvalidOperationException("Unable to locate Navigation page");
		}

		await navigation.PushModalAsync(popupPage);
		await navigation.PushModalAsync(new ContentPage());

		// Assert
		await Assert.ThrowsAsync<PopupBlockedException>(async () => await popupPage.CloseAsync(new PopupResult(false), CancellationToken.None));
		await Assert.ThrowsAnyAsync<InvalidPopupOperationException>(async () => await popupPage.CloseAsync(new PopupResult(false), CancellationToken.None));
		await Assert.ThrowsAnyAsync<InvalidOperationException>(async () => await popupPage.CloseAsync(new PopupResult(false), CancellationToken.None));
	}

	[Fact]
	public void PopupPageT_Close_ShouldThrowOperationCanceledException_WhenTokenIsCancelled()
	{
		// Arrange
		var view = new ContentView();
		var popupOptions = new MockPopupOptions();
		var popupPage = new PopupPage<string>(view, popupOptions);
		var result = new PopupResult<string>("Test", false);
		var cts = new CancellationTokenSource();
		cts.Cancel();

		// Act
		Func<Task> act = async () => await popupPage.Close(result, cts.Token);

		// Assert
		act.Should().ThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public void Constructor_WithViewAndPopupOptions_SetsCorrectProperties()
	{
		// Arrange
		var view = new Label
		{
			Text = "Test Popup Content",
			Margin = new Thickness(10),
			Padding = new Thickness(5),
			VerticalOptions = LayoutOptions.Center,
			HorizontalOptions = LayoutOptions.Center,
		};
		var popupOptions = new MockPopupOptions
		{
			CanBeDismissedByTappingOutsideOfPopup = true,
			PageOverlayColor = Colors.Red,
			Shape = new RoundRectangle { CornerRadius = new CornerRadius(10) },
		};

		// Act
		var popupPage = new PopupPage(view, popupOptions);

		// Assert
		Assert.NotNull(popupPage.Content);
		Assert.IsType<PopupPage.PopupPageLayout>(popupPage.Content);

		// Verify iOS platform specific settings
		Assert.Equal(PresentationMode.ModalNotAnimated, Shell.GetPresentationMode(popupPage));
		Assert.Equal(UIModalPresentationStyle.OverFullScreen, popupPage.On<iOS>().ModalPresentationStyle());

		// Verify content has tap gesture recognizer attached
		Assert.Single(popupPage.Content.GestureRecognizers);
		Assert.IsType<TapGestureRecognizer>(popupPage.Content.GestureRecognizers[0]);

		// Verify PopupPageLayout structure
		var pageContent = popupPage.Content;
		Assert.Single(pageContent.Children);
		Assert.IsType<Border>(pageContent.Children[0]);

		// Verify content binding context is set correctly
		Assert.Equal(view.BindingContext, pageContent.BindingContext);
	}

	[Fact]
	public void Constructor_WithNullView_ThrowsArgumentNullException()
	{
		// Arrange
		var popupOptions = new MockPopupOptions();

		// Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => new PopupPage((View?)null, popupOptions));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact]
	public void Constructor_WithNullPopup_ThrowsArgumentNullException()
	{
		// Arrange
		var popupOptions = new MockPopupOptions();

		// Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => new PopupPage((Popup?)null, popupOptions));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact]
	public void Constructor_WithNullPopupOptions_ThrowsArgumentNullException()
	{
		// Arrange
		var view = new Label();
		IPopupOptions popupOptions = null!;

		// Act & Assert
		Assert.Throws<ArgumentNullException>(() => new PopupPage(view, popupOptions));
	}

	[Fact]
	public async Task TapGestureRecognizer_ShouldClosePopupWhenCanBeDismissedIsTrue()
	{
		// Arrange
		bool actionInvoked = false;
		var actionInvokedTCS = new TaskCompletionSource<bool>();
		var view = new Label { Text = "Test Popup Content" };
		var popupOptions = new MockPopupOptions
		{
			CanBeDismissedByTappingOutsideOfPopup = true,
			OnTappingOutsideOfPopup = () =>
			{
				actionInvoked = true;
				actionInvokedTCS.SetResult(actionInvoked);
			}
		};

		var popupPage = new PopupPage(view, popupOptions);

		var tapGestureRecognizer = (TapGestureRecognizer)popupPage.Content.GestureRecognizers[0];
		var command = tapGestureRecognizer.Command;
		Assert.NotNull(command);

		// Act & Assert
		Assert.True(command.CanExecute(null));
		popupOptions.OnTappingOutsideOfPopup?.Invoke();

		var result = await actionInvokedTCS.Task;

		Assert.True(result);
		Assert.True(actionInvoked);
	}

	[Fact]
	public void TapGestureRecognizer_ShouldNotExecuteWhenCanBeDismissedIsFalse()
	{
		// Arrange
		var view = new Label { Text = "Test Popup Content" };
		var popupOptions = new MockPopupOptions
		{
			CanBeDismissedByTappingOutsideOfPopup = false
		};

		var popupPage = new PopupPage(view, popupOptions);
		var tapGestureRecognizer = (TapGestureRecognizer)popupPage.Content.GestureRecognizers[0];
		var command = tapGestureRecognizer.Command;

		// Act & Assert
		Assert.NotNull(command);
		Assert.False(command.CanExecute(null));
	}

	[Fact]
	public void OnBackButtonPressed_ReturnsTrueWhenCanBeDismissedIsFalse()
	{
		// Arrange
		var view = new Label { Text = "Test Popup Content" };
		var popupOptions = new MockPopupOptions
		{
			CanBeDismissedByTappingOutsideOfPopup = false
		};

		var popupPage = new TestablePopupPage(view, popupOptions);

		// Act
		var result = popupPage.TestOnBackButtonPressed();

		// Assert
		Assert.True(result);
	}

	[Fact]
	public void PopupPage_ShouldInitializeCorrectly_WithValidParameters()
	{
		// Arrange
		var view = new Label { Text = "Test Popup Content" };
		var popupOptions = new MockPopupOptions
		{
			CanBeDismissedByTappingOutsideOfPopup = true,
			PageOverlayColor = Colors.Blue
		};

		// Act
		var popupPage = new PopupPage(view, popupOptions);

		// Assert
		Assert.NotNull(popupPage.Content);
		Assert.Equal(popupOptions.PageOverlayColor, popupPage.BackgroundColor);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task PopupClosedEvent_ShouldTriggerOnce_WhenPopupIsClosed()
	{
		// Arrange
		Assert.NotNull(Application.Current);
		var navigation = Application.Current.Windows[0].Page?.Navigation ?? throw new InvalidOperationException("Unable to locate INavigation");
		var view = new Label { Text = "Test Popup Content" };
		var popupOptions = new MockPopupOptions();
		var eventTriggered = false;


		// Act
		navigation.ShowPopup(view, popupOptions);

		var popupPage = (PopupPage)navigation.ModalStack[0];
		popupPage.PopupClosed += (sender, args) => eventTriggered = true;
		await popupPage.CloseAsync(new PopupResult(false), CancellationToken.None);

		// Assert
		Assert.True(eventTriggered);
	}

	[Fact]
	public void TappingOutside_ShouldNotClosePopup_WhenCanBeDismissedIsFalse()
	{
		// Arrange
		var view = new Label { Text = "Test Popup Content" };
		var popupOptions = new MockPopupOptions
		{
			CanBeDismissedByTappingOutsideOfPopup = false
		};
		var popupPage = new PopupPage(view, popupOptions);

		// Act
		var tapGestureRecognizer = (TapGestureRecognizer)popupPage.Content.GestureRecognizers[0];
		var command = tapGestureRecognizer.Command;

		// Assert
		Assert.NotNull(command);
		Assert.False(command.CanExecute(null));
	}

	[Fact]
	public void BackButton_ShouldNotClosePopup_WhenCanBeDismissedIsFalse()
	{
		// Arrange
		var view = new Label { Text = "Test Popup Content" };
		var popupOptions = new MockPopupOptions
		{
			CanBeDismissedByTappingOutsideOfPopup = false
		};
		var popupPage = new TestablePopupPage(view, popupOptions);

		// Act
		var result = popupPage.TestOnBackButtonPressed();

		// Assert
		Assert.True(result);
	}

	[Fact]
	public async Task Close_ShouldThrowException_WhenCalledOnNonModalPopup()
	{
		// Arrange
		var view = new Label { Text = "Test Popup Content" };
		var popupOptions = new MockPopupOptions();
		var popupPage = new PopupPage(view, popupOptions);

		// Act & Assert
		await Assert.ThrowsAsync<PopupNotFoundException>(async () => await popupPage.CloseAsync(new PopupResult(false), CancellationToken.None));
	}

	[Fact]
	public void PopupPage_ShouldRespectLayoutOptions()
	{
		// Arrange
		var view = new Label
		{
			Text = "Test Popup Content",
			VerticalOptions = LayoutOptions.Start,
			HorizontalOptions = LayoutOptions.End
		};

		// Act
		var popupPage = new PopupPage(view, PopupOptions.Empty);
		var border = (Border)popupPage.Content.Children[0];

		// Assert
		Assert.Equal(LayoutOptions.Start, border.VerticalOptions);
		Assert.Equal(LayoutOptions.End, border.HorizontalOptions);
	}

	// Helper class for testing protected methods
	sealed class TestablePopupPage(View view, IPopupOptions popupOptions) : PopupPage(view, popupOptions)
	{
		public bool TestOnBackButtonPressed()
		{
			return OnBackButtonPressed();
		}
	}

	sealed class MockPopupOptions : IPopupOptions
	{
		public bool CanBeDismissedByTappingOutsideOfPopup { get; set; }
		public Color PageOverlayColor { get; set; } = Colors.Transparent;
		public Action? OnTappingOutsideOfPopup { get; set; }
		public Shape? Shape { get; set; }
		public Shadow? Shadow { get; set; } = null;
	}
}