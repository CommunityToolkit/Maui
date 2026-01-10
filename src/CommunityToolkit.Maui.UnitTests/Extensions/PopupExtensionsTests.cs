using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.UnitTests.Mocks;
using CommunityToolkit.Maui.UnitTests.Services;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Shapes;
using Nito.AsyncEx;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions;

public class PopupExtensionsTests : BaseViewTest
{
	const string shellParameterViewModelTextValue = "Hello World";
	static readonly Color shellParameterBackgroundColorValue = Colors.Green;

	readonly INavigation navigation;

	readonly IDictionary<string, object> shellParameters = new Dictionary<string, object>
	{
		{ nameof(View.BackgroundColor), shellParameterBackgroundColorValue },
		{ nameof(ViewModelWithIQueryAttributable.Text), shellParameterViewModelTextValue }
	}.AsReadOnly();

	public PopupExtensionsTests()
	{
		var page = new MockPage(new MockPageViewModel());
		Assert.NotNull(Application.Current);

		Application.Current.Windows[0].Page = page;
		navigation = page.Navigation;
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ClosePopup_TokenExpired_ShouldThrowOperationCancelledException()
	{
		// Arrange
		var cts = new CancellationTokenSource();

		// Act
		await cts.CancelAsync();

		// Assert
		await Assert.ThrowsAsync<OperationCanceledException>(() => navigation.ClosePopupAsync(cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ClosePopup_NoExistingPopup_ShouldThrowPopupNotFoundException()
	{
		// Arrange

		// Act

		// Assert
		await Assert.ThrowsAsync<PopupNotFoundException>(() => navigation.ClosePopupAsync(TestContext.Current.CancellationToken));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ClosePopup_PopupBlocked_ShouldThrowPopupBlockedException()
	{
		// Arrange

		// Act
		navigation.ShowPopup(new Button());
		await navigation.PushModalAsync(new ContentPage());

		// Assert
		await Assert.ThrowsAsync<PopupBlockedException>(() => navigation.ClosePopupAsync(TestContext.Current.CancellationToken));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ClosePopup_NullPage_ShouldThrowArgumentNullException()
	{
		// Arrange

		// Act

		// Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		await Assert.ThrowsAsync<ArgumentNullException>(() => PopupExtensions.ClosePopupAsync((Page?)null, TestContext.Current.CancellationToken));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ClosePopup_NullNavigation_ShouldThrowArgumentNullException()
	{
		// Arrange

		// Act

		// Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		await Assert.ThrowsAsync<ArgumentNullException>(() => PopupExtensions.ClosePopupAsync((INavigation?)null, TestContext.Current.CancellationToken));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ClosePopupT_NullPage_ShouldThrowArgumentNullException()
	{
		// Arrange

		// Act

		// Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		await Assert.ThrowsAsync<ArgumentNullException>(() => PopupExtensions.ClosePopupAsync((Page?)null, 2, TestContext.Current.CancellationToken));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ClosePopupT_NullNavigation_ShouldThrowArgumentNullException()
	{
		// Arrange

		// Act

		// Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		await Assert.ThrowsAsync<ArgumentNullException>(() => PopupExtensions.ClosePopupAsync((INavigation?)null, 2, TestContext.Current.CancellationToken));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_WithPopupType_ShowsPopupAndClosesPopup()
	{
		// Arrange
		var selfClosingPopup = ServiceProvider.GetRequiredService<ShortLivedSelfClosingPopup>() ?? throw new InvalidOperationException();

		// Act
		navigation.ShowPopup(selfClosingPopup);

		// Assert
		Assert.Single(navigation.ModalStack);
		Assert.IsType<PopupPage>(navigation.ModalStack[0]);

		// Act
		await navigation.ClosePopupAsync(TestContext.Current.CancellationToken);

		// Assert
		Assert.Empty(navigation.ModalStack);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_Shell_WithPopupType_ShowsPopupAndClosesPopup()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var shellNavigation = Shell.Current.Navigation;

		// Act
		shell.ShowPopup(new Popup());

		// Assert
		Assert.Single(shellNavigation.ModalStack);
		Assert.IsType<PopupPage>(shellNavigation.ModalStack[0]);

		// Act
		await navigation.ClosePopupAsync(TestContext.Current.CancellationToken);

		// Assert
		Assert.Empty(navigation.ModalStack);
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
		Assert.IsType<PopupPage>(navigation.ModalStack[0]);
	}

	[Fact]
	public void ShowPopupAsync_WithViewType_SetsCorrectDefaults()
	{
		// Arrange
		Border popupBorder;
		PopupPage popupPage;
		Popup autogeneratedPopup;
		var label = new Label();

		// Act
		navigation.ShowPopup(label);

		popupPage = (PopupPage)navigation.ModalStack[0];
		popupBorder = popupPage.Content.PopupBorder;
		autogeneratedPopup = (Popup)(popupPage.Content.PopupBorder.Content ?? throw new InvalidOperationException("Border Content cannot be null"));

		// Assert
		Assert.Equal(new Thickness(30), popupBorder.Margin);
		Assert.Equal(LayoutOptions.Center, popupBorder.VerticalOptions);
		Assert.Equal(LayoutOptions.Center, popupBorder.HorizontalOptions);
		Assert.Equal(new Thickness(15), autogeneratedPopup.Padding);
		Assert.Equal(Colors.White, autogeneratedPopup.BackgroundColor);
		Assert.True(autogeneratedPopup.CanBeDismissedByTappingOutsideOfPopup);
	}

	[Fact]
	public void ShowPopupAsync_Shell_WithViewType_ShowsPopup()
	{
		// Arrange
		var viewWithQueryable = new ViewWithIQueryAttributable(new ViewModelWithIQueryAttributable());
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var shellNavigation = Shell.Current.Navigation;

		// Act
		shell.ShowPopup(viewWithQueryable, shellParameters: shellParameters);

		// Assert
		Assert.Single(shellNavigation.ModalStack);
		Assert.IsType<PopupPage>(shellNavigation.ModalStack[0]);
		Assert.Equal(shellParameterBackgroundColorValue, viewWithQueryable.BackgroundColor);
		Assert.Equal(shellParameterViewModelTextValue, viewWithQueryable.BindingContext.Text);
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public async Task ShowPopupAsync_AwaitingShowPopupAsync_EnsurePreviousPopupClosed()
	{
		// Arrange
		var selfClosingPopup = ServiceProvider.GetRequiredService<ShortLivedSelfClosingPopup>() ?? throw new InvalidOperationException();
		var longLivedSelfClosingPopup = ServiceProvider.GetRequiredService<LongLivedSelfClosingPopup>() ?? throw new InvalidOperationException();

		// Act
		await navigation.ShowPopupAsync<object?>(longLivedSelfClosingPopup, PopupOptions.Empty, TestContext.Current.CancellationToken);
		await navigation.ShowPopupAsync<object?>(selfClosingPopup, PopupOptions.Empty, TestContext.Current.CancellationToken);

		// Assert
		Assert.Empty(navigation.ModalStack);
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public async Task ShowPopupAsync_Shell_AwaitingShowPopupAsync_EnsurePreviousPopupClosed()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var shellNavigation = Shell.Current.Navigation;
		var selfClosingPopup = ServiceProvider.GetRequiredService<ShortLivedSelfClosingPopup>() ?? throw new InvalidOperationException();
		var longLivedSelfClosingPopup = ServiceProvider.GetRequiredService<LongLivedSelfClosingPopup>() ?? throw new InvalidOperationException();

		// Act
		await shell.ShowPopupAsync<object?>(longLivedSelfClosingPopup, PopupOptions.Empty, shellParameters, TestContext.Current.CancellationToken);
		await shell.ShowPopupAsync<object?>(selfClosingPopup, PopupOptions.Empty, shellParameters, TestContext.Current.CancellationToken);

		// Assert
		Assert.Empty(shellNavigation.ModalStack);
		Assert.Equal(shellParameterBackgroundColorValue, selfClosingPopup.BackgroundColor);
	}

	[Fact]
	public void ShowPopup_NavigationModalStackCountIncreases()
	{
		// Arrange
		var selfClosingPopup = ServiceProvider.GetRequiredService<ShortLivedSelfClosingPopup>() ?? throw new InvalidOperationException();
		Assert.Empty(navigation.ModalStack);

		// Act
		navigation.ShowPopup(selfClosingPopup, PopupOptions.Empty);

		// Assert
		Assert.Single(navigation.ModalStack);
	}

	[Fact]
	public void ShowPopup_Shell_NavigationModalStackCountIncreases()
	{
		// Arrange
		var viewWithQueryable = new ViewWithIQueryAttributable(new ViewModelWithIQueryAttributable());
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var shellNavigation = Shell.Current.Navigation;
		Assert.Empty(shellNavigation.ModalStack);

		// Act
		shell.ShowPopup(viewWithQueryable, shellParameters: shellParameters);

		// Assert
		Assert.Single(shellNavigation.ModalStack);
		Assert.Equal(shellParameterBackgroundColorValue, viewWithQueryable.BackgroundColor);
		Assert.Equal(shellParameterViewModelTextValue, viewWithQueryable.BindingContext.Text);
	}

	[Fact]
	public void ShowPopupWithView_NavigationModalStackCountIncreases()
	{
		// Arrange
		var viewWithQueryable = new ViewWithIQueryAttributable(new ViewModelWithIQueryAttributable());
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var shellNavigation = Shell.Current.Navigation;
		Assert.Empty(shellNavigation.ModalStack);

		// Act
		navigation.ShowPopup(viewWithQueryable, PopupOptions.Empty);

		// Assert
		Assert.Single(navigation.ModalStack);
	}

	[Fact]
	public void ShowPopupWithView_Shell_NavigationModalStackCountIncreases()
	{
		// Arrange
		var viewWithQueryable = new ViewWithIQueryAttributable(new ViewModelWithIQueryAttributable());
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var shellNavigation = Shell.Current.Navigation;
		Assert.Empty(shellNavigation.ModalStack);

		// Act
		shell.ShowPopup(viewWithQueryable, PopupOptions.Empty, shellParameters);

		// Assert
		Assert.Single(shellNavigation.ModalStack);
		Assert.Equal(shellParameterBackgroundColorValue, viewWithQueryable.BackgroundColor);
		Assert.Equal(shellParameterViewModelTextValue, viewWithQueryable.BindingContext.Text);
	}

	[Fact]
	public void ShowPopup_MultiplePopupsDisplayed()
	{
		// Arrange
		var selfClosingPopup = ServiceProvider.GetRequiredService<ShortLivedSelfClosingPopup>() ?? throw new InvalidOperationException();
		var longLivedSelfClosingPopup = ServiceProvider.GetRequiredService<LongLivedSelfClosingPopup>() ?? throw new InvalidOperationException();

		// Act
		navigation.ShowPopup(longLivedSelfClosingPopup, PopupOptions.Empty);
		navigation.ShowPopup(selfClosingPopup, PopupOptions.Empty);

		// Assert
		Assert.Equal(2, navigation.ModalStack.Count);
	}

	[Fact]
	public void ShowPopup_Shell_MultiplePopupsDisplayed()
	{
		// Arrange
		var selfClosingPopup = ServiceProvider.GetRequiredService<ShortLivedSelfClosingPopup>() ?? throw new InvalidOperationException();
		var longLivedSelfClosingPopup = ServiceProvider.GetRequiredService<LongLivedSelfClosingPopup>() ?? throw new InvalidOperationException();
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var shellNavigation = Shell.Current.Navigation;

		// Act
		shell.ShowPopup(longLivedSelfClosingPopup, PopupOptions.Empty, shellParameters);
		shell.ShowPopup(selfClosingPopup, PopupOptions.Empty, shellParameters);

		// Assert
		Assert.Equal(2, shellNavigation.ModalStack.Count);
		Assert.Equal(shellParameterBackgroundColorValue, selfClosingPopup.BackgroundColor);
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
	public void ShowPopupView_Shell_MultiplePopupsDisplayed()
	{
		// Arrange
		var viewWithQueryable = new ViewWithIQueryAttributable(new ViewModelWithIQueryAttributable());
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var shellNavigation = Shell.Current.Navigation;

		// Act
		shell.ShowPopup(viewWithQueryable, PopupOptions.Empty, shellParameters);
		shell.ShowPopup(viewWithQueryable, PopupOptions.Empty, shellParameters);

		// Assert
		Assert.Equal(2, shellNavigation.ModalStack.Count);
		Assert.Equal(shellParameterBackgroundColorValue, viewWithQueryable.BackgroundColor);
		Assert.Equal(shellParameterViewModelTextValue, viewWithQueryable.BindingContext.Text);
	}


	[Fact]
	public void ShowPopupAsync_WithCustomOptions_AppliesOptions()
	{
		// Arrange
		var onTappingOutsideOfPopup = () => { };

		var selfClosingPopup = ServiceProvider.GetRequiredService<ShortLivedSelfClosingPopup>() ?? throw new InvalidOperationException();
		var options = new PopupOptions
		{
			PageOverlayColor = Colors.Red,
			CanBeDismissedByTappingOutsideOfPopup = false,
			Shape = new Ellipse(),
			OnTappingOutsideOfPopup = onTappingOutsideOfPopup
		};

		// Act
		navigation.ShowPopup(selfClosingPopup, options);

		var popupPage = (PopupPage)navigation.ModalStack[0];
		var popupPageContent = popupPage.Content;
		var border = popupPageContent.PopupBorder;
		var popup = (Popup)(border.Content ?? throw new InvalidOperationException("Content cannot be null"));

		// Assert
		Assert.NotNull(popup);

		// Verify BindingContexts
		Assert.Equal(selfClosingPopup.BindingContext, popupPage.BindingContext);
		Assert.Equal(popup.BindingContext, popupPage.BindingContext);
		Assert.Equal(popupPageContent.BindingContext, border.BindingContext);

		// Verify View options Binding to Popup
		Assert.Equal(selfClosingPopup.BindingContext, popup.BindingContext);
		Assert.Equal(selfClosingPopup.Background, popup.Background);
		Assert.Equal(selfClosingPopup.BackgroundColor, popup.BackgroundColor);
		Assert.Equal(selfClosingPopup.Margin, popup.Margin);
		Assert.Equal(selfClosingPopup.VerticalOptions, popup.VerticalOptions);
		Assert.Equal(selfClosingPopup.HorizontalOptions, popup.HorizontalOptions);

		// Verify View options Binding to Border
		Assert.Equal(selfClosingPopup.BindingContext, border.BindingContext);
		Assert.Equal(selfClosingPopup.Background, border.Background);
		Assert.Equal(selfClosingPopup.BackgroundColor, border.BackgroundColor);
		Assert.Equal(selfClosingPopup.Margin, border.Margin);
		Assert.Equal(selfClosingPopup.VerticalOptions, border.VerticalOptions);
		Assert.Equal(selfClosingPopup.HorizontalOptions, border.HorizontalOptions);

		// Verify Border Bindings to Border
		Assert.Equal(popup.BindingContext, border.BindingContext);
		Assert.Equal(popup.Margin, border.Margin);
		Assert.Equal(popup.Background, border.Background);
		Assert.Equal(popup.BackgroundColor, border.BackgroundColor);
		Assert.Equal(popup.HorizontalOptions, border.HorizontalOptions);
		Assert.Equal(popup.VerticalOptions, border.VerticalOptions);

		// Verify Border Bindings to PopupOptions
		Assert.Equal(options.Shadow, border.Shadow);
		Assert.Equal(options.Shape.Stroke, border.Stroke);
		Assert.Equal(options.Shape, border.StrokeShape);

		// Verify PopupPage Bindings to PopupOptions
		Assert.Equal(options.PageOverlayColor, popupPage.BackgroundColor);
	}

	[Fact]
	public void ShowPopupAsync_Shell_WithCustomOptions_AppliesOptions()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var shellNavigation = Shell.Current.Navigation;
		var onTappingOutsideOfPopup = () => { };

		var selfClosingPopup = ServiceProvider.GetRequiredService<ShortLivedSelfClosingPopup>() ?? throw new InvalidOperationException();
		var options = new PopupOptions
		{
			PageOverlayColor = Colors.Red,
			CanBeDismissedByTappingOutsideOfPopup = false,
			Shape = new Ellipse(),
			OnTappingOutsideOfPopup = onTappingOutsideOfPopup
		};

		// Act
		shell.ShowPopup(selfClosingPopup, options, shellParameters);

		var popupPage = (PopupPage)shellNavigation.ModalStack[0];
		var popupPageContent = popupPage.Content;
		var border = popupPageContent.PopupBorder;
		var popup = (ShortLivedSelfClosingPopup)(border.Content ?? throw new InvalidOperationException("Content cannot be null"));

		// Assert
		Assert.NotNull(popup);

		// Verify BindingContexts
		Assert.Equal(selfClosingPopup.BindingContext, popupPage.BindingContext);
		Assert.Equal(popup.BindingContext, popupPage.BindingContext);
		Assert.Equal(popupPageContent.BindingContext, border.BindingContext);

		// Verify View options Binding to Popup
		Assert.Equal(selfClosingPopup.BindingContext, popup.BindingContext);
		Assert.Equal(selfClosingPopup.Background, popup.Background);
		Assert.Equal(selfClosingPopup.BackgroundColor, popup.BackgroundColor);
		Assert.Equal(selfClosingPopup.Margin, popup.Margin);
		Assert.Equal(selfClosingPopup.VerticalOptions, popup.VerticalOptions);
		Assert.Equal(selfClosingPopup.HorizontalOptions, popup.HorizontalOptions);

		// Verify View options Binding to Border
		Assert.Equal(selfClosingPopup.BindingContext, border.BindingContext);
		Assert.Equal(selfClosingPopup.Background, border.Background);
		Assert.Equal(selfClosingPopup.BackgroundColor, border.BackgroundColor);
		Assert.Equal(selfClosingPopup.Margin, border.Margin);
		Assert.Equal(selfClosingPopup.VerticalOptions, border.VerticalOptions);
		Assert.Equal(selfClosingPopup.HorizontalOptions, border.HorizontalOptions);

		// Verify Border Bindings to Border
		Assert.Equal(popup.BindingContext, border.BindingContext);
		Assert.Equal(popup.Margin, border.Margin);
		Assert.Equal(popup.Background, border.Background);
		Assert.Equal(popup.BackgroundColor, border.BackgroundColor);
		Assert.Equal(popup.HorizontalOptions, border.HorizontalOptions);
		Assert.Equal(popup.VerticalOptions, border.VerticalOptions);

		// Verify Border Bindings to PopupOptions
		Assert.Equal(options.Shadow, border.Shadow);
		Assert.Equal(options.Shape.Stroke, border.Stroke);
		Assert.Equal(options.Shape, border.StrokeShape);

		// Verify PopupPage Bindings to PopupOptions
		Assert.Equal(options.PageOverlayColor, popupPage.BackgroundColor);

		// Verify IQueryAttributable Propagates through Popup
		Assert.Equal(shellParameterBackgroundColorValue, selfClosingPopup.BackgroundColor);
	}

	[Fact]
	public void ShowPopupAsyncWithView_WithCustomOptions_AppliesOptions()
	{
		// Arrange
		var onTappingOutsideOfPopup = () => { };

		var view = new Grid
		{
			Margin = 20,
			BackgroundColor = Colors.Orange,
			HorizontalOptions = LayoutOptions.End,
			VerticalOptions = LayoutOptions.Start,
		};
		var options = new PopupOptions
		{
			PageOverlayColor = Colors.Red,
			CanBeDismissedByTappingOutsideOfPopup = false,
			Shape = new Ellipse(),
			OnTappingOutsideOfPopup = onTappingOutsideOfPopup
		};

		// Act
		navigation.ShowPopup(view, options);

		var popupPage = (PopupPage)navigation.ModalStack[0];
		var popupPageContent = popupPage.Content;
		var border = popupPageContent.PopupBorder;
		var popup = (Popup)(border.Content ?? throw new InvalidCastException());

		// Assert
		Assert.NotNull(popup);

		// Verify BindingContexts
		Assert.Equal(view.BindingContext, popupPage.BindingContext);
		Assert.Equal(popup.BindingContext, popupPage.BindingContext);
		Assert.Equal(popupPageContent.BindingContext, border.BindingContext);

		// Verify View options Binding to Popup
		Assert.Equal(view.BindingContext, popup.BindingContext);
		Assert.Equal(view.Background, popup.Background);
		Assert.Equal(view.BackgroundColor, popup.BackgroundColor);
		Assert.Equal(view.Margin, popup.Margin);
		Assert.Equal(view.VerticalOptions, popup.VerticalOptions);
		Assert.Equal(view.HorizontalOptions, popup.HorizontalOptions);

		// Verify View options Binding to Border
		Assert.Equal(view.BindingContext, border.BindingContext);
		Assert.Equal(view.Background, border.Background);
		Assert.Equal(view.BackgroundColor, border.BackgroundColor);
		Assert.Equal(view.Margin, border.Margin);
		Assert.Equal(view.VerticalOptions, border.VerticalOptions);
		Assert.Equal(view.HorizontalOptions, border.HorizontalOptions);

		// Verify Popup Bindings to Border
		Assert.Equal(popup.BindingContext, border.BindingContext);
		Assert.Equal(popup.Margin, border.Margin);
		Assert.Equal(popup.Background, border.Background);
		Assert.Equal(popup.BackgroundColor, border.BackgroundColor);
		Assert.Equal(popup.HorizontalOptions, border.HorizontalOptions);
		Assert.Equal(popup.VerticalOptions, border.VerticalOptions);

		// Verify Border Bindings to PopupOptions
		Assert.Equal(options.Shadow, border.Shadow);
		Assert.Equal(options.Shape.Stroke, border.Stroke);
		Assert.Equal(options.Shape, border.StrokeShape);

		// Verify PopupPage Bindings to PopupOptions
		Assert.Equal(options.PageOverlayColor, popupPage.BackgroundColor);
	}

	[Fact]
	public void ShowPopupAsyncWithView_Shell_WithCustomOptions_AppliesOptions()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var shellNavigation = Shell.Current.Navigation;
		var onTappingOutsideOfPopup = () => { };

		var viewWithQueryable = new ViewWithIQueryAttributable(new ViewModelWithIQueryAttributable())
		{
			HorizontalOptions = LayoutOptions.End,
			VerticalOptions = LayoutOptions.Start,
			BackgroundColor = Colors.Red,
			Background = new LinearGradientBrush(),
			Margin = 6,
			Padding = 12
		};

		var options = new PopupOptions
		{
			PageOverlayColor = Colors.Red,
			CanBeDismissedByTappingOutsideOfPopup = false,
			Shape = new Ellipse(),
			OnTappingOutsideOfPopup = onTappingOutsideOfPopup
		};

		// Act
		shell.ShowPopup(viewWithQueryable, options, shellParameters);

		var popupPage = (PopupPage)shellNavigation.ModalStack[0];
		var popupPageContent = popupPage.Content;
		var border = popupPageContent.PopupBorder;
		var popup = (Popup)(border.Content ?? throw new InvalidCastException());

		// Assert
		Assert.NotNull(popup);

		// Verify BindingContexts
		Assert.Equal(viewWithQueryable.BindingContext, popupPage.BindingContext);
		Assert.Equal(popup.BindingContext, popupPage.BindingContext);
		Assert.Equal(popupPageContent.BindingContext, border.BindingContext);

		// Verify View options Binding to Popup
		Assert.Equal(viewWithQueryable.BindingContext, popup.BindingContext);
		Assert.Equal(viewWithQueryable.Background, popup.Background);
		Assert.Equal(viewWithQueryable.BackgroundColor, popup.BackgroundColor);
		Assert.Equal(viewWithQueryable.Margin, popup.Margin);

		// Verify View options Binding to Border
		Assert.Equal(viewWithQueryable.BindingContext, border.BindingContext);
		Assert.Equal(viewWithQueryable.Background, border.Background);
		Assert.Equal(viewWithQueryable.BackgroundColor, border.BackgroundColor);
		Assert.Equal(viewWithQueryable.VerticalOptions, border.VerticalOptions);
		Assert.Equal(viewWithQueryable.HorizontalOptions, border.HorizontalOptions);

		// Verify Border Bindings to Popup
		Assert.Equal(popup.BindingContext, border.BindingContext);
		Assert.Equal(popup.Margin, border.Margin);
		Assert.Equal(popup.Background, border.Background);
		Assert.Equal(popup.BackgroundColor, border.BackgroundColor);
		Assert.Equal(popup.HorizontalOptions, border.HorizontalOptions);
		Assert.Equal(popup.VerticalOptions, border.VerticalOptions);

		// Verify Border Bindings to PopupOptions
		Assert.Equal(options.Shadow, border.Shadow);
		Assert.Equal(options.Shape.Stroke, border.Stroke);
		Assert.Equal(options.Shape, border.StrokeShape);

		// Verify PopupPage Bindings to PopupOptions
		Assert.Equal(options.PageOverlayColor, popupPage.BackgroundColor);

		// Verify IQueryAttributable Propagates through Popup
		Assert.Equal(shellParameterBackgroundColorValue, viewWithQueryable.BackgroundColor);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_CancellationTokenExpired()
	{
		// Arrange
		var selfClosingPopup = ServiceProvider.GetRequiredService<ShortLivedSelfClosingPopup>() ?? throw new InvalidOperationException();
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Act
		await Task.Delay(100, TestContext.Current.CancellationToken); // Ensure CancellationToken has expired

		// Assert
		await Assert.ThrowsAsync<OperationCanceledException>(() => navigation.ShowPopupAsync(selfClosingPopup, PopupOptions.Empty, cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_Shell_CancellationTokenExpired()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var shellNavigation = Shell.Current.Navigation;
		var selfClosingPopup = ServiceProvider.GetRequiredService<ShortLivedSelfClosingPopup>() ?? throw new InvalidOperationException();

		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Act
		await Task.Delay(100, TestContext.Current.CancellationToken); // Ensure CancellationToken has expired

		// Assert
		await Assert.ThrowsAsync<OperationCanceledException>(() => shell.ShowPopupAsync(selfClosingPopup, PopupOptions.Empty, shellParameters, cts.Token));
		Assert.NotEqual(shellParameterBackgroundColorValue, selfClosingPopup.BackgroundColor);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsyncWithView_CancellationTokenExpired()
	{
		// Arrange
		var view = new Grid();
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Act
		await Task.Delay(100, TestContext.Current.CancellationToken); // Ensure CancellationToken has expired

		// Assert
		await Assert.ThrowsAsync<OperationCanceledException>(() => navigation.ShowPopupAsync(view, PopupOptions.Empty, cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsyncWithView_Shell_CancellationTokenExpired()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var shellNavigation = Shell.Current.Navigation;
		var view = new ViewWithIQueryAttributable(new ViewModelWithIQueryAttributable());

		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Act
		await Task.Delay(100, TestContext.Current.CancellationToken); // Ensure CancellationToken has expired

		// Assert
		await Assert.ThrowsAsync<OperationCanceledException>(() => shell.ShowPopupAsync(view, PopupOptions.Empty, shellParameters, cts.Token));
		Assert.NotEqual(shellParameterBackgroundColorValue, view.BackgroundColor);
		Assert.NotEqual(shellParameterViewModelTextValue, view.BindingContext.Text);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_CancellationTokenCanceled()
	{
		// Arrange
		var selfClosingPopup = ServiceProvider.GetRequiredService<ShortLivedSelfClosingPopup>() ?? throw new InvalidOperationException();
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Act
		await cts.CancelAsync(); // Ensure CancellationToken has expired

		// Assert
		await Assert.ThrowsAsync<OperationCanceledException>(() => navigation.ShowPopupAsync(selfClosingPopup, PopupOptions.Empty, cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_Shell_CancellationTokenCanceled()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var shellNavigation = Shell.Current.Navigation;
		var selfClosingPopup = ServiceProvider.GetRequiredService<ShortLivedSelfClosingPopup>() ?? throw new InvalidOperationException();

		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Act
		await cts.CancelAsync();

		// Assert
		await Assert.ThrowsAsync<OperationCanceledException>(() => navigation.ShowPopupAsync(selfClosingPopup, PopupOptions.Empty, cts.Token));
		Assert.NotEqual(shellParameterBackgroundColorValue, selfClosingPopup.BackgroundColor);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsyncWithView_CancellationTokenCanceled()
	{
		// Arrange
		var view = new Grid();
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Act
		await cts.CancelAsync(); // Ensure CancellationToken has expired

		// Assert
		await Assert.ThrowsAsync<OperationCanceledException>(() => navigation.ShowPopupAsync(view, PopupOptions.Empty, cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsyncWithView_Shell_CancellationTokenCanceled()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var shellNavigation = Shell.Current.Navigation;
		var view = new ViewWithIQueryAttributable(new ViewModelWithIQueryAttributable());

		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Act
		await cts.CancelAsync();

		// Assert
		await Assert.ThrowsAsync<OperationCanceledException>(() => shell.ShowPopupAsync(view, PopupOptions.Empty, shellParameters, cts.Token));
		Assert.NotEqual(shellParameterBackgroundColorValue, view.BackgroundColor);
		Assert.NotEqual(shellParameterViewModelTextValue, view.BindingContext.Text);
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShowPopupAsync_ShouldValidateProperBindingContext()
	{
		// Arrange
		var selfClosingPopup = ServiceProvider.GetRequiredService<ShortLivedSelfClosingPopup>() ?? throw new InvalidOperationException();
		var popupInstance = ServiceProvider.GetRequiredService<ShortLivedSelfClosingPopup>();

		// Act
		await navigation.ShowPopupAsync<object?>(selfClosingPopup, PopupOptions.Empty, TestContext.Current.CancellationToken);

		// Assert
		Assert.NotNull(popupInstance.BindingContext);
		Assert.IsType<ShortLivedMockPageViewModel>(popupInstance.BindingContext);
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShowPopupAsync_Shell_ShouldValidateProperBindingContext()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var shellNavigation = Shell.Current.Navigation;
		var selfClosingPopup = ServiceProvider.GetRequiredService<ShortLivedSelfClosingPopup>() ?? throw new InvalidOperationException();
		var popupInstance = ServiceProvider.GetRequiredService<ShortLivedSelfClosingPopup>();

		// Act
		await shell.ShowPopupAsync<object?>(selfClosingPopup, PopupOptions.Empty, shellParameters, TestContext.Current.CancellationToken);

		// Assert
		Assert.NotNull(popupInstance.BindingContext);
		Assert.IsType<ShortLivedMockPageViewModel>(popupInstance.BindingContext);
		Assert.Equal(shellParameterBackgroundColorValue, selfClosingPopup.BackgroundColor);
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShowPopupAsyncWithView_ShouldValidateProperBindingContext()
	{
		// Arrange
		var view = new Grid();
		var popupInstance = ServiceProvider.GetRequiredService<ShortLivedSelfClosingPopup>();

		// Act
		var showPopupTask = navigation.ShowPopupAsync<object?>(view, PopupOptions.Empty, TestContext.Current.CancellationToken);

		var popupPage = (PopupPage)navigation.ModalStack[0];

		await popupPage.CloseAsync(new PopupResult<object?>(null, false), TestContext.Current.CancellationToken);
		await showPopupTask;

		// Assert
		Assert.NotNull(popupInstance.BindingContext);
		Assert.IsType<ShortLivedMockPageViewModel>(popupInstance.BindingContext);
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShowPopupAsyncWithView_Shell_ShouldValidateProperBindingContext()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var shellNavigation = Shell.Current.Navigation;
		var view = new ViewWithIQueryAttributable(new ViewModelWithIQueryAttributable());
		var popupInstance = ServiceProvider.GetRequiredService<ShortLivedSelfClosingPopup>();

		// Act
		var showPopupTask = shell.ShowPopupAsync<object?>(view, PopupOptions.Empty, shellParameters, TestContext.Current.CancellationToken);

		var popupPage = (PopupPage)shellNavigation.ModalStack[0];
		await popupPage.CloseAsync(new PopupResult<object?>(null, false), TestContext.Current.CancellationToken);

		await showPopupTask;

		// Assert
		Assert.NotNull(popupInstance.BindingContext);
		Assert.IsType<ShortLivedMockPageViewModel>(popupInstance.BindingContext);
		Assert.Equal(shellParameterBackgroundColorValue, view.BackgroundColor);
		Assert.Equal(shellParameterViewModelTextValue, view.BindingContext.Text);
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShowPopupAsync_ShouldSuccessfullyCompleteAndReturnResultUnderHeavyGarbageCollection()
	{
		// Arrange
		var mockPopup = ServiceProvider.GetRequiredService<GarbageCollectionHeavySelfClosingPopup>();
		var selfClosingPopup = ServiceProvider.GetRequiredService<GarbageCollectionHeavySelfClosingPopup>() ?? throw new InvalidOperationException();

		// Act
		var result = await navigation.ShowPopupAsync<object?>(selfClosingPopup, PopupOptions.Empty, TestContext.Current.CancellationToken);

		// Assert
		Assert.Same(mockPopup.Result, result.Result);
		Assert.False(result.WasDismissedByTappingOutsideOfPopup);
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShowPopupAsync_ShouldReturnResultOnceClosed()
	{
		// Arrange
		var mockPopup = ServiceProvider.GetRequiredService<ShortLivedSelfClosingPopup>();
		var selfClosingPopup = ServiceProvider.GetRequiredService<ShortLivedSelfClosingPopup>() ?? throw new InvalidOperationException();

		// Act
		var result = await navigation.ShowPopupAsync<object?>(selfClosingPopup, PopupOptions.Empty, TestContext.Current.CancellationToken);

		// Assert
		Assert.Same(mockPopup.Result, result.Result);
		Assert.False(result.WasDismissedByTappingOutsideOfPopup);
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShowPopupAsync_Shell_ShouldReturnResultOnceClosed()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var shellNavigation = Shell.Current.Navigation;
		var mockPopup = ServiceProvider.GetRequiredService<ShortLivedSelfClosingPopup>();
		var selfClosingPopup = ServiceProvider.GetRequiredService<ShortLivedSelfClosingPopup>() ?? throw new InvalidOperationException();

		// Act
		var result = await shell.ShowPopupAsync<object?>(selfClosingPopup, PopupOptions.Empty, shellParameters, TestContext.Current.CancellationToken);

		// Assert
		Assert.Same(mockPopup.Result, result.Result);
		Assert.False(result.WasDismissedByTappingOutsideOfPopup);
		Assert.Equal(shellParameterBackgroundColorValue, selfClosingPopup.BackgroundColor);
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShowPopupAsyncWithView_ShouldReturnResultOnceClosed()
	{
		// Arrange
		const int popupResultValue = 2;

		var view = new Grid();
		var expectedPopupResult = new PopupResult<int>(popupResultValue, false);

		// Act
		var showPopupTask = navigation.ShowPopupAsync<int>(view, PopupOptions.Empty, TestContext.Current.CancellationToken);

		var popupPage = (PopupPage)navigation.ModalStack[0];

		await popupPage.CloseAsync(expectedPopupResult, TestContext.Current.CancellationToken);
		var actualPopupResult = await showPopupTask;

		// Assert
		Assert.Same(expectedPopupResult, actualPopupResult);
		Assert.False(expectedPopupResult.WasDismissedByTappingOutsideOfPopup);
		Assert.Equal(popupResultValue, expectedPopupResult.Result);
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShowPopupAsyncWithView_Shell_ShouldReturnResultOnceClosed()
	{
		// Arrange
		const int popupResultValue = 2;
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var shellNavigation = Shell.Current.Navigation;

		var view = new ViewWithIQueryAttributable(new ViewModelWithIQueryAttributable());
		var expectedPopupResult = new PopupResult<int>(popupResultValue, false);

		// Act
		var showPopupTask = shell.ShowPopupAsync<int>(view, PopupOptions.Empty, shellParameters, TestContext.Current.CancellationToken);

		var popupPage = (PopupPage)shellNavigation.ModalStack[0];

		await popupPage.CloseAsync(expectedPopupResult, TestContext.Current.CancellationToken);
		var actualPopupResult = await showPopupTask;

		// Assert
		Assert.Same(expectedPopupResult, actualPopupResult);
		Assert.False(expectedPopupResult.WasDismissedByTappingOutsideOfPopup);
		Assert.Equal(popupResultValue, expectedPopupResult.Result);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_ShouldThrowArgumentNullException_WhenViewIsNull()
	{
		// Arrange

		// Act/Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		await Assert.ThrowsAsync<ArgumentNullException>(() => navigation.ShowPopupAsync(null, PopupOptions.Empty, TestContext.Current.CancellationToken));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_Shell_ShouldThrowArgumentNullException_WhenViewIsNull()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var shellNavigation = Shell.Current.Navigation;

		// Act/Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		await Assert.ThrowsAsync<ArgumentNullException>(() => shell.ShowPopupAsync(null, PopupOptions.Empty, shellParameters, TestContext.Current.CancellationToken));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_ShouldThrowArgumentNullException_WhenNavigationIsNull()
	{
		// Arrange
		var selfClosingPopup = ServiceProvider.GetRequiredService<ShortLivedSelfClosingPopup>() ?? throw new InvalidOperationException();

		// Act / Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		await Assert.ThrowsAsync<ArgumentNullException>(() => PopupExtensions.ShowPopupAsync((INavigation?)null, selfClosingPopup, PopupOptions.Empty, TestContext.Current.CancellationToken));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_Shell_ShouldThrowArgumentNullException_WhenNavigationIsNull()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var shellNavigation = Shell.Current.Navigation;
		var selfClosingPopup = ServiceProvider.GetRequiredService<ShortLivedSelfClosingPopup>() ?? throw new InvalidOperationException();

		// Act/Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		await Assert.ThrowsAsync<ArgumentNullException>(() => PopupExtensions.ShowPopupAsync((Shell?)null, selfClosingPopup, PopupOptions.Empty, shellParameters, TestContext.Current.CancellationToken));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_ShouldReturnNullResult_WhenPopupIsClosedWithoutResult()
	{
		// Arrange

		// Act
		var showPopupTask = navigation.ShowPopupAsync<object?>(new Popup(), PopupOptions.Empty, TestContext.Current.CancellationToken);

		var popupPage = (PopupPage)navigation.ModalStack.Last();
		await popupPage.CloseAsync(new PopupResult(true), TestContext.Current.CancellationToken);
		var result = await showPopupTask;

		// Assert
		Assert.Null(result.Result);
		Assert.True(result.WasDismissedByTappingOutsideOfPopup);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_Shell_ShouldReturnNullResult_WhenPopupIsClosedWithoutResult()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var shellNavigation = Shell.Current.Navigation;

		// Act
		var showPopupTask = shell.ShowPopupAsync<object?>(new Popup(), PopupOptions.Empty, shellParameters, TestContext.Current.CancellationToken);

		var popupPage = (PopupPage)shellNavigation.ModalStack.Last();
		await popupPage.CloseAsync(new PopupResult(true), TestContext.Current.CancellationToken);
		var result = await showPopupTask;

		// Assert
		Assert.Null(result.Result);
		Assert.True(result.WasDismissedByTappingOutsideOfPopup);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_ReferenceTypeShouldReturnNull_WhenPopupTapGestureRecognizerCommandIsExecuted()
	{
		// Arrange
		var popupClosedTCS = new TaskCompletionSource<IPopupResult>();

		// Act
		var showPopupTask = navigation.ShowPopupAsync<bool?>(new Popup<bool>(), token: TestContext.Current.CancellationToken);
		var popupPage = (PopupPage)navigation.ModalStack[0];
		popupPage.PopupClosed += HandlePopupClosed;

		try
		{
			// Run using AsyncContext to catch Exception thrown by fire-and-forget ICommand.Execute
			AsyncContext.Run(() => Assert.True(popupPage.TryExecuteTapOutsideOfPopupCommand()));
		}
		catch (PopupNotFoundException) // PopupNotFoundException is expected here because `ShowPopup` was never called
		{
		}

		var popupClosedResult = await popupClosedTCS.Task;
		var showPopupResult = await showPopupTask;

		// Assert
		Assert.True(popupClosedResult.WasDismissedByTappingOutsideOfPopup);
		Assert.Null(showPopupResult.Result);
		Assert.True(showPopupResult.WasDismissedByTappingOutsideOfPopup);

		void HandlePopupClosed(object? sender, IPopupResult e)
		{
			popupClosedTCS.SetResult(e);
		}
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_Shell_ReferenceTypeShouldReturnNull_WhenPopupTapGestureRecognizerCommandIsExecuted()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var shellNavigation = Shell.Current.Navigation;
		var popupClosedTCS = new TaskCompletionSource<IPopupResult>();

		// Act
		var showPopupTask = shell.ShowPopupAsync<bool?>(new Popup<bool>(), token: TestContext.Current.CancellationToken);
		var popupPage = (PopupPage)shellNavigation.ModalStack[0];
		popupPage.PopupClosed += HandlePopupClosed;

		try
		{
			// Run using AsyncContext to catch Exception thrown by fire-and-forget ICommand.Execute
			AsyncContext.Run(() => Assert.True(popupPage.TryExecuteTapOutsideOfPopupCommand()));
		}
		catch (PopupNotFoundException) // PopupNotFoundException is expected here because `ShowPopup` was never called
		{
		}

		var popupClosedResult = await popupClosedTCS.Task;
		var showPopupResult = await showPopupTask;

		// Assert
		Assert.True(popupClosedResult.WasDismissedByTappingOutsideOfPopup);
		Assert.Null(showPopupResult.Result);
		Assert.True(showPopupResult.WasDismissedByTappingOutsideOfPopup);

		void HandlePopupClosed(object? sender, IPopupResult e)
		{
			popupClosedTCS.SetResult(e);
		}
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_NullableValueTypeShouldReturnResult_WhenPopupIsClosedByTappingOutsidePopup()
	{
		// Arrange
		var popupClosedTCS = new TaskCompletionSource<IPopupResult>();

		// Act
		var showPopupTask = navigation.ShowPopupAsync<bool?>(new Popup<bool>(), token: TestContext.Current.CancellationToken);
		var popupPage = (PopupPage)navigation.ModalStack[0];
		popupPage.PopupClosed += HandlePopupClosed;

		try
		{
			// Run using AsyncContext to catch Exception thrown by fire-and-forget ICommand.Execute
			AsyncContext.Run(() => Assert.True(popupPage.TryExecuteTapOutsideOfPopupCommand()));
		}
		catch (PopupNotFoundException) // PopupNotFoundException is expected here because `ShowPopup` was never called
		{
		}

		var popupClosedResult = await popupClosedTCS.Task;
		var showPopupResult = await showPopupTask;

		// Assert
		Assert.True(popupClosedResult.WasDismissedByTappingOutsideOfPopup);
		Assert.Null(showPopupResult.Result);
		Assert.True(showPopupResult.WasDismissedByTappingOutsideOfPopup);

		void HandlePopupClosed(object? sender, IPopupResult e)
		{
			popupClosedTCS.SetResult(e);
		}
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_Shell_NullableValueTypeShouldReturnResult_WhenPopupIsClosedByTappingOutsidePopup()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var shellNavigation = Shell.Current.Navigation;
		var popupClosedTCS = new TaskCompletionSource<IPopupResult>();

		// Act
		var showPopupTask = shell.ShowPopupAsync<bool?>(new Popup<bool>(), token: TestContext.Current.CancellationToken);
		var popupPage = (PopupPage)shellNavigation.ModalStack[0];
		popupPage.PopupClosed += HandlePopupClosed;

		try
		{
			// Run using AsyncContext to catch Exception thrown by fire-and-forget ICommand.Execute
			AsyncContext.Run(() => Assert.True(popupPage.TryExecuteTapOutsideOfPopupCommand()));
		}
		catch (PopupNotFoundException) // PopupNotFoundException is expected here because `ShowPopup` was never called
		{
		}

		var popupClosedResult = await popupClosedTCS.Task;
		var showPopupResult = await showPopupTask;

		// Assert
		Assert.True(popupClosedResult.WasDismissedByTappingOutsideOfPopup);
		Assert.Null(showPopupResult.Result);
		Assert.True(showPopupResult.WasDismissedByTappingOutsideOfPopup);

		void HandlePopupClosed(object? sender, IPopupResult e)
		{
			popupClosedTCS.SetResult(e);
		}
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_ValueTypeShouldReturnResult_WhenPopupIsClosedByTappingOutsidePopup()
	{
		// Arrange
		var popupClosedTCS = new TaskCompletionSource<IPopupResult>();

		// Act
		var showPopupTask = navigation.ShowPopupAsync<bool>(new Popup<bool>(), token: TestContext.Current.CancellationToken);
		var popupPage = (PopupPage)navigation.ModalStack[0];
		popupPage.PopupClosed += HandlePopupClosed;

		try
		{
			// Run using AsyncContext to catch Exception thrown by fire-and-forget ICommand.Execute
			AsyncContext.Run(() => Assert.True(popupPage.TryExecuteTapOutsideOfPopupCommand()));
		}
		catch (PopupNotFoundException) // PopupNotFoundException is expected here because `ShowPopup` was never called
		{
		}

		var popupClosedResult = await popupClosedTCS.Task;
		var showPopupResult = await showPopupTask;

		// Assert
		Assert.True(popupClosedResult.WasDismissedByTappingOutsideOfPopup);
		Assert.True(showPopupResult.WasDismissedByTappingOutsideOfPopup);
		Assert.ThrowsAny<PopupResultException>(() => (bool?)showPopupResult.Result);
		Assert.ThrowsAny<InvalidCastException>(() => (bool?)showPopupResult.Result);

		void HandlePopupClosed(object? sender, IPopupResult e)
		{
			popupClosedTCS.SetResult(e);
		}
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_Shell_ValueTypeShouldReturnResult_WhenPopupIsClosedByTappingOutsidePopup()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var shellNavigation = Shell.Current.Navigation;
		var popupClosedTCS = new TaskCompletionSource<IPopupResult>();

		// Act
		var showPopupTask = shell.ShowPopupAsync<bool>(new Popup<bool>(), token: TestContext.Current.CancellationToken);
		var popupPage = (PopupPage)shellNavigation.ModalStack[0];
		popupPage.PopupClosed += HandlePopupClosed;

		try
		{
			// Run using AsyncContext to catch Exception thrown by fire-and-forget ICommand.Execute
			AsyncContext.Run(() => Assert.True(popupPage.TryExecuteTapOutsideOfPopupCommand()));
		}
		catch (PopupNotFoundException) // PopupNotFoundException is expected here because `ShowPopup` was never called
		{
		}

		var popupClosedResult = await popupClosedTCS.Task;
		var showPopupResult = await showPopupTask;

		// Assert
		Assert.True(popupClosedResult.WasDismissedByTappingOutsideOfPopup);
		Assert.True(showPopupResult.WasDismissedByTappingOutsideOfPopup);
		Assert.ThrowsAny<PopupResultException>(() => (bool?)showPopupResult.Result);
		Assert.ThrowsAny<InvalidCastException>(() => (bool?)showPopupResult.Result);

		void HandlePopupClosed(object? sender, IPopupResult e)
		{
			popupClosedTCS.SetResult(e);
		}
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ClosePopupAsync_ShouldClosePopupUsingNavigationAndReturnResult()
	{
		// Arrange

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		// Act
		page.ShowPopup(new MockPopup());

		// Assert
		Assert.Single(page.Navigation.ModalStack);
		Assert.IsType<PopupPage>(page.Navigation.ModalStack[0]);

		// Act
		var popupResult = await page.ClosePopupAsync(TestContext.Current.CancellationToken);

		// Assert
		Assert.Empty(page.Navigation.ModalStack);
		Assert.False(popupResult.WasDismissedByTappingOutsideOfPopup);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ClosePopupAsync_ShouldClosePopupUsingPageAndReturnResult()
	{
		// Arrange
		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		// Act
		page.ShowPopup(new MockPopup());

		// Assert
		Assert.Single(page.Navigation.ModalStack);
		Assert.IsType<PopupPage>(page.Navigation.ModalStack[0]);

		// Act
		var popupResult = await page.ClosePopupAsync(page, TestContext.Current.CancellationToken);

		// Assert
		Assert.Empty(page.Navigation.ModalStack);
		Assert.False(popupResult.WasDismissedByTappingOutsideOfPopup);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ClosePopupAsyncT_ShouldClosePopupUsingNavigationAndReturnResult()
	{
		// Arrange
		const int expectedResult = 2;
		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		// Act
		page.ShowPopup(new Popup());

		// Assert
		Assert.Single(page.Navigation.ModalStack);
		Assert.IsType<PopupPage>(page.Navigation.ModalStack[0]);

		// Act
		var popupResult = await page.ClosePopupAsync(expectedResult, TestContext.Current.CancellationToken);

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

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		// Act
		page.ShowPopup(new MockPopup());

		// Assert
		Assert.Single(page.Navigation.ModalStack);
		Assert.IsType<PopupPage>(page.Navigation.ModalStack[0]);

		// Act
		var popupResult = await page.ClosePopupAsync(expectedResult, TestContext.Current.CancellationToken);

		// Assert
		Assert.Empty(page.Navigation.ModalStack);
		Assert.Equal(expectedResult, popupResult.Result);
		Assert.False(popupResult.WasDismissedByTappingOutsideOfPopup);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_TaskShouldCompleteWhenCloseAsyncIsCalled()
	{
		// Arrange
		const int expectedResult = 2;
		Task<IPopupResult> showPopupAsyncTask;

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		// Act
		showPopupAsyncTask = page.ShowPopupAsync(new MockPopup(), token: TestContext.Current.CancellationToken);

		// Assert
		Assert.Single(page.Navigation.ModalStack);
		Assert.IsType<PopupPage>(page.Navigation.ModalStack[0]);

		// Act
		var popupResult = await page.ClosePopupAsync(expectedResult, TestContext.Current.CancellationToken);
		await showPopupAsyncTask;

		// Assert
		Assert.Empty(page.Navigation.ModalStack);
		Assert.Equal(expectedResult, popupResult.Result);
		Assert.False(popupResult.WasDismissedByTappingOutsideOfPopup);
	}
}

sealed class ViewWithIQueryAttributable : Button, IQueryAttributable
{
	public ViewWithIQueryAttributable(ViewModelWithIQueryAttributable viewModel)
	{
		base.BindingContext = viewModel;
	}

	public new ViewModelWithIQueryAttributable BindingContext => (ViewModelWithIQueryAttributable)base.BindingContext;

	void IQueryAttributable.ApplyQueryAttributes(IDictionary<string, object> query)
	{
		BackgroundColor = (Color)query[nameof(BackgroundColor)];
	}
}

sealed class ViewModelWithIQueryAttributable : IQueryAttributable
{
	public string? Text { get; set; }

	void IQueryAttributable.ApplyQueryAttributes(IDictionary<string, object> query)
	{
		Text = (string)query[nameof(Text)];
	}
}