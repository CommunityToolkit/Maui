using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using FluentAssertions;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.Shapes;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class PopupContainerTests : BaseTest
{
	[Fact]
	public void Constructor_ShouldThrowArgumentNullException_WhenPopupIsNull()
	{
		// Arrange
		var popupOptions = new MockPopupOptions();
		var taskCompletionSource = new TaskCompletionSource<PopupResult>();

		// Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Action act = () => new PopupContainer(null, popupOptions);
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
		Action act = () => new PopupContainer(view, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		// Assert
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public async Task Close_ShouldSetResultAndPopModalAsync()
	{
		// Arrange
		var tcs = new TaskCompletionSource<IPopupResult>();
		var view = new ContentView();
		var popupOptions = new MockPopupOptions();
		var popupContainer = new PopupContainer(view, popupOptions);
		var expectedResult = new PopupResult(false);

		popupContainer.PopupClosed += HandlePopupClosed;

		// Act
		await popupContainer.Close(expectedResult, CancellationToken.None);
		var actualResult = await tcs.Task;

		// Assert
		actualResult.Should().Be(expectedResult);

		void HandlePopupClosed(object? sender, IPopupResult e)
		{
			tcs.SetResult(e);
		}
	}

	[Fact]
	public void Close_ShouldThrowOperationCanceledException_WhenTokenIsCancelled()
	{
		// Arrange
		var view = new ContentView();
		var popupOptions = new MockPopupOptions();
		var popupContainer = new PopupContainer(view, popupOptions);
		var result = new PopupResult(false);
		var cts = new CancellationTokenSource();
		cts.Cancel();

		// Act
		Func<Task> act = async () => await popupContainer.Close(result, cts.Token);

		// Assert
		act.Should().ThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public void PopupContainerT_Constructor_ShouldThrowArgumentNullException_WhenPopupIsNull()
	{
		// Arrange
		var popupOptions = new MockPopupOptions();

		// Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Action act = () => new PopupContainer<string>(null, popupOptions);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		// Assert
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void PopupContainerT_Constructor_ShouldThrowArgumentNullException_WhenPopupOptionsIsNull()
	{
		// Arrange
		var view = new ContentView();

		// Act
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Action act = () => new PopupContainer<string>(view, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		// Assert
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public async Task PopupContainerT_Close_ShouldSetResultAndPopModalAsync()
	{
		// Arrange
		var view = new ContentView();
		var popupOptions = new MockPopupOptions();
		var taskCompletionSource = new TaskCompletionSource<PopupResult<string>>();
		var popupContainer = new PopupContainer<string>(view, popupOptions);
		var expectedResult = new PopupResult<string>("Test", false);

		popupContainer.PopupClosed += HandlePopupClosed;

		// Act
		await popupContainer.Close(expectedResult, CancellationToken.None);
		var actualResult = await taskCompletionSource.Task;

		// Assert
		actualResult.Should().Be(expectedResult);

		void HandlePopupClosed(object? sender, IPopupResult e)
		{
			taskCompletionSource.SetResult((PopupResult<string>)e);
		}
	}

	[Fact]
	public void PopupContainerT_Close_ShouldThrowOperationCanceledException_WhenTokenIsCancelled()
	{
		// Arrange
		var view = new ContentView();
		var popupOptions = new MockPopupOptions();
		var popupContainer = new PopupContainer<string>(view, popupOptions);
		var result = new PopupResult<string>("Test", false);
		var cts = new CancellationTokenSource();
		cts.Cancel();

		// Act
		Func<Task> act = async () => await popupContainer.Close(result, cts.Token);

		// Assert
		act.Should().ThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public void Constructor_WithViewAndPopupOptions_SetsCorrectProperties()
	{
		// Arrange
		var view = new Label { Text = "Test Popup Content" };
		var popupOptions = new MockPopupOptions
		{
			CanBeDismissedByTappingOutsideOfPopup = true,
			PageOverlayColor = Colors.Red,
			Margin = new Thickness(10),
			Padding = new Thickness(5),
			VerticalOptions = LayoutOptions.Center,
			HorizontalOptions = LayoutOptions.Center,
			Shape = new RoundRectangle { CornerRadius = new CornerRadius(10) },
			BorderStroke = {  }
		};

		// Act
		var popupContainer = new PopupContainer(view, popupOptions);

		// Assert
		Assert.NotNull(popupContainer.Content);
		Assert.IsType<PopupContainer.PopupContainerContent>(popupContainer.Content);

		// Verify iOS platform specific settings
		Assert.Equal(PresentationMode.ModalNotAnimated, Shell.GetPresentationMode(popupContainer));
		Assert.Equal(UIModalPresentationStyle.OverFullScreen, popupContainer.On<iOS>().ModalPresentationStyle());

		// Verify content has tap gesture recognizer attached
		Assert.Single(popupContainer.Content.GestureRecognizers);
		Assert.IsType<TapGestureRecognizer>(popupContainer.Content.GestureRecognizers[0]);

		// Verify PopupContainerContent structure
		var containerContent = popupContainer.Content;
		Assert.Single(containerContent.Children);
		Assert.IsType<Border>(containerContent.Children[0]);

		// Verify content binding context is set correctly
		Assert.Equal(view.BindingContext, containerContent.BindingContext);
	}

	[Fact]
	public void Constructor_WithNullView_ThrowsArgumentNullException()
	{
		// Arrange
		var popupOptions = new MockPopupOptions();

		// Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => new PopupContainer((View?)null, popupOptions));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact]
	public void Constructor_WithNullPopup_ThrowsArgumentNullException()
	{
		// Arrange
		var popupOptions = new MockPopupOptions();

		// Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => new PopupContainer((Popup?)null, popupOptions));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact]
	public void Constructor_WithNullPopupOptions_ThrowsArgumentNullException()
	{
		// Arrange
		var view = new Label();
		IPopupOptions popupOptions = null!;

		// Act & Assert
		Assert.Throws<ArgumentNullException>(() => new PopupContainer(view, popupOptions));
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

		var popupContainer = new PopupContainer(view, popupOptions);

		var tapGestureRecognizer = (TapGestureRecognizer)popupContainer.Content.GestureRecognizers[0];
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

		var popupContainer = new PopupContainer(view, popupOptions);
		var tapGestureRecognizer = (TapGestureRecognizer)popupContainer.Content.GestureRecognizers[0];
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

		var popupContainer = new TestablePopupContainer(view, popupOptions);

		// Act
		var result = popupContainer.TestOnBackButtonPressed();

		// Assert
		Assert.True(result);
	}

	// Helper class for testing protected methods
	sealed class TestablePopupContainer(View view, IPopupOptions popupOptions) : PopupContainer(view, popupOptions)
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
		public Brush? BorderStroke { get; } = null;
		public Action? OnTappingOutsideOfPopup { get; set; }
		public IShape? Shape { get; set; }
		public Thickness Margin { get; set; } = new Thickness(0);
		public Thickness Padding { get; set; } = new Thickness(0);
		public LayoutOptions VerticalOptions { get; set; } = LayoutOptions.Center;
		public LayoutOptions HorizontalOptions { get; set; } = LayoutOptions.Center;
	}
}