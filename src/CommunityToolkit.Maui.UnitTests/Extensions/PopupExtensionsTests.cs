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
	public async Task ClosePopup_TokenExpired_ShouldThrowOperationCanceledException()
	{
		// Arrange
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

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

	[Fact]
	public void ShowPopup_NullPage_ShouldThrowArgumentNullException()
	{
		// Arrange / Act / Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => PopupExtensions.ShowPopup((Page?)null, new Grid(), PopupOptions.Empty));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact]
	public async Task ShowPopupAsync_NullNavigation_ShouldThrowArgumentNullException()
	{
		// Arrange / Act / Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		await Assert.ThrowsAsync<ArgumentNullException>(() => PopupExtensions.ShowPopupAsync((INavigation?)null, new Grid(), PopupOptions.Empty, TestContext.Current.CancellationToken));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact]
	public async Task ShowPopupAsync_NullView_ShouldThrowArgumentNullException()
	{
		// Arrange / Act / Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		await Assert.ThrowsAsync<ArgumentNullException>(() => navigation.ShowPopupAsync((View?)null, PopupOptions.Empty, TestContext.Current.CancellationToken));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact]
	public async Task ShowPopupAsync_Shell_NullShell_ShouldThrowArgumentNullException()
	{
		// Arrange / Act / Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		await Assert.ThrowsAsync<ArgumentNullException>(() => PopupExtensions.ShowPopupAsync((Shell?)null, new Grid(), PopupOptions.Empty, shellParameters, TestContext.Current.CancellationToken));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact]
	public async Task ShowPopupAsync_Shell_NullView_ShouldThrowArgumentNullException()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		// Act / Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		await Assert.ThrowsAsync<ArgumentNullException>(() => shell.ShowPopupAsync((View?)null, PopupOptions.Empty, shellParameters, TestContext.Current.CancellationToken));
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
		var modalPage = Assert.Single(navigation.ModalStack);
		Assert.IsType<PopupPage>(modalPage);

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
		var modalPage = Assert.Single(shellNavigation.ModalStack);
		Assert.IsType<PopupPage>(modalPage);

		// Act
		await shellNavigation.ClosePopupAsync(TestContext.Current.CancellationToken);

		// Assert
		Assert.Empty(shellNavigation.ModalStack);
	}

	[Fact]
	public void ShowPopupAsync_WithViewType_ShowsPopup()
	{
		// Arrange
		var view = new Grid();

		// Act
		navigation.ShowPopup(view);

		// Assert
		var modalPage = Assert.Single(navigation.ModalStack);
		Assert.IsType<PopupPage>(modalPage);
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
		Assert.Null(autogeneratedPopup.BackgroundColor);
		Assert.Equal(Colors.White, popupBorder.BackgroundColor);
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
		var modalPage = Assert.Single(shellNavigation.ModalStack);
		Assert.IsType<PopupPage>(modalPage);
		Assert.Equal(shellParameterBackgroundColorValue, viewWithQueryable.BackgroundColor);
		Assert.Equal(shellParameterViewModelTextValue, viewWithQueryable.BindingContext.Text);
	}

	[Fact]
	public void ShowPopupAsync_Shell_WithViewType_BackgroundColorFromShellParameters_ShowsColorOnBorder()
	{
		var viewWithQueryable = new ViewWithIQueryAttributable(new ViewModelWithIQueryAttributable());
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var shellNavigation = Shell.Current.Navigation;

		// ViewWithIQueryAttributable sets its BackgroundColor when Shell applies the parameters, after the PopupPage created the Popup that wraps it
		shell.ShowPopup(viewWithQueryable, shellParameters: shellParameters);

		var popupPage = Assert.IsType<PopupPage>(Assert.Single(shellNavigation.ModalStack));
		var popupBorder = popupPage.Content.PopupBorder;
		var popup = Assert.IsType<Popup>(popupBorder.Content, exactMatch: false);
		Assert.Equal(shellParameterBackgroundColorValue, popup.BackgroundColor);
		Assert.Equal(shellParameterBackgroundColorValue, popupBorder.BackgroundColor);
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

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopup_NavigationModalStackCountIncreases()
	{
		// Arrange
		var selfClosingPopup = ServiceProvider.GetRequiredService<ShortLivedSelfClosingPopup>() ?? throw new InvalidOperationException();
		Assert.Empty(navigation.ModalStack);

		// Act
		navigation.ShowPopup(selfClosingPopup, PopupOptions.Empty);

		// Assert
		Assert.Single(navigation.ModalStack);

		await selfClosingPopup.WaitUntilClosedAsync(TestContext.Current.CancellationToken);
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

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShowPopup_MultiplePopupsDisplayed()
	{
		// Arrange
		var selfClosingPopup = ServiceProvider.GetRequiredService<ShortLivedSelfClosingPopup>() ?? throw new InvalidOperationException();
		var longLivedSelfClosingPopup = ServiceProvider.GetRequiredService<LongLivedSelfClosingPopup>() ?? throw new InvalidOperationException();

		// Act
		navigation.ShowPopup(longLivedSelfClosingPopup, PopupOptions.Empty);
		navigation.ShowPopup(selfClosingPopup, PopupOptions.Empty);

		// Assert
		Assert.Equal(2, navigation.ModalStack.Count);

		await selfClosingPopup.WaitUntilClosedAsync(TestContext.Current.CancellationToken);
		await longLivedSelfClosingPopup.WaitUntilClosedAsync(TestContext.Current.CancellationToken);
	}

	[Fact(Timeout = (int)TestDuration.Medium)]
	public async Task ShowPopup_Shell_MultiplePopupsDisplayed()
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

		await selfClosingPopup.WaitUntilClosedAsync(TestContext.Current.CancellationToken);
		await longLivedSelfClosingPopup.WaitUntilClosedAsync(TestContext.Current.CancellationToken);
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


	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_WithCustomOptions_AppliesOptions()
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

		await selfClosingPopup.WaitUntilClosedAsync(TestContext.Current.CancellationToken);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_Shell_WithCustomOptions_AppliesOptions()
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

		await selfClosingPopup.WaitUntilClosedAsync(TestContext.Current.CancellationToken);
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
		await WaitForCancellationAsync(cts.Token, TestContext.Current.CancellationToken);

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
		await WaitForCancellationAsync(cts.Token, TestContext.Current.CancellationToken);

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
		await WaitForCancellationAsync(cts.Token, TestContext.Current.CancellationToken);

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
		await WaitForCancellationAsync(cts.Token, TestContext.Current.CancellationToken);

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
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

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

		using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

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
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

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

		using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

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

		var popupPage = (PopupPage)navigation.ModalStack[^1];
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

		var popupPage = (PopupPage)shellNavigation.ModalStack[^1];
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

		var popupClosedResult = await popupClosedTCS.Task.WaitAsync(TestContext.Current.CancellationToken);
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

		var popupClosedResult = await popupClosedTCS.Task.WaitAsync(TestContext.Current.CancellationToken);
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

		var popupClosedResult = await popupClosedTCS.Task.WaitAsync(TestContext.Current.CancellationToken);
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

		var popupClosedResult = await popupClosedTCS.Task.WaitAsync(TestContext.Current.CancellationToken);
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

		var popupClosedResult = await popupClosedTCS.Task.WaitAsync(TestContext.Current.CancellationToken);
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

		var popupClosedResult = await popupClosedTCS.Task.WaitAsync(TestContext.Current.CancellationToken);
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
	public async Task TryShowPopupAsync_ShouldReturnPopupResult_WhenPopupIsClosed()
	{
		// Arrange
		var showPopupTask = navigation.TryShowPopupAsync(new Popup(), PopupOptions.Empty, TestContext.Current.CancellationToken);
		var popupPage = (PopupPage)navigation.ModalStack.Last();

		// Act
		await popupPage.CloseAsync(new PopupResult(false), TestContext.Current.CancellationToken);
		var result = await showPopupTask;

		// Assert
		var popupResult = Assert.IsType<IPopupResult>(result.Value, exactMatch: false);
		Assert.False(popupResult.WasDismissedByTappingOutsideOfPopup);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryShowPopupAsync_ShouldMatchExhaustively_WhenPopupIsDismissedByTappingOutsideOfPopup()
	{
		// Arrange
		var showPopupTask = navigation.TryShowPopupAsync<string>(new Popup(), PopupOptions.Empty, TestContext.Current.CancellationToken);
		var popupPage = (PopupPage)navigation.ModalStack.Last();

		// Act
		await popupPage.CloseAsync(new PopupResult(true), TestContext.Current.CancellationToken);
		var message = await showPopupTask switch
		{
			IPopupResult<string> { WasDismissedByTappingOutsideOfPopup: true } => "Dismissed",
			IPopupResult<string> popupResult => $"Closed with {popupResult.Result}",
			OperationCanceledException => "Canceled",
			Exception exception => $"Failed: {exception.Message}"
		};

		// Assert
		Assert.Equal("Dismissed", message);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryShowPopupAsync_Page_ShouldReturnPopupResult_WhenPopupIsClosed()
	{
		// Arrange
		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		var showPopupTask = page.TryShowPopupAsync(new Popup(), PopupOptions.Empty, TestContext.Current.CancellationToken);
		var popupPage = (PopupPage)navigation.ModalStack.Last();

		// Act
		await popupPage.CloseAsync(new PopupResult(false), TestContext.Current.CancellationToken);
		var result = await showPopupTask;

		// Assert
		var popupResult = Assert.IsType<IPopupResult>(result.Value, exactMatch: false);
		Assert.False(popupResult.WasDismissedByTappingOutsideOfPopup);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryShowPopupAsync_Shell_ShouldReturnPopupResult_WhenPopupIsClosed()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var shellNavigation = Shell.Current.Navigation;
		var showPopupTask = shell.TryShowPopupAsync(new Popup(), PopupOptions.Empty, shellParameters, TestContext.Current.CancellationToken);
		var popupPage = (PopupPage)shellNavigation.ModalStack.Last();

		// Act
		await popupPage.CloseAsync(new PopupResult(false), TestContext.Current.CancellationToken);
		var result = await showPopupTask;

		// Assert
		var popupResult = Assert.IsType<IPopupResult>(result.Value, exactMatch: false);
		Assert.False(popupResult.WasDismissedByTappingOutsideOfPopup);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryShowPopupAsyncT_ShouldReturnTypedPopupResult_WhenPopupIsClosed()
	{
		// Arrange
		const int expectedResult = 7;
		var showPopupTask = navigation.TryShowPopupAsync<int>(new Grid(), PopupOptions.Empty, TestContext.Current.CancellationToken);
		var popupPage = (PopupPage)navigation.ModalStack.Last();

		// Act
		await popupPage.CloseAsync(new PopupResult<int>(expectedResult, false), TestContext.Current.CancellationToken);
		var result = await showPopupTask;

		// Assert
		var popupResult = Assert.IsType<IPopupResult<int>>(result.Value, exactMatch: false);
		Assert.Equal(expectedResult, popupResult.Result);
		Assert.False(popupResult.WasDismissedByTappingOutsideOfPopup);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryShowPopupAsyncT_Page_ShouldReturnTypedPopupResult_WhenPopupIsClosed()
	{
		// Arrange
		const int expectedResult = 11;

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		var showPopupTask = page.TryShowPopupAsync<int>(new Grid(), PopupOptions.Empty, TestContext.Current.CancellationToken);
		var popupPage = (PopupPage)navigation.ModalStack.Last();

		// Act
		await popupPage.CloseAsync(new PopupResult<int>(expectedResult, false), TestContext.Current.CancellationToken);
		var result = await showPopupTask;

		// Assert
		var popupResult = Assert.IsType<IPopupResult<int>>(result.Value, exactMatch: false);
		Assert.Equal(expectedResult, popupResult.Result);
		Assert.False(popupResult.WasDismissedByTappingOutsideOfPopup);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryShowPopupAsyncT_Shell_ShouldReturnTypedPopupResult_WhenPopupIsClosed()
	{
		// Arrange
		const int expectedResult = 13;

		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var shellNavigation = Shell.Current.Navigation;
		var showPopupTask = shell.TryShowPopupAsync<int>(new ViewWithIQueryAttributable(new ViewModelWithIQueryAttributable()), PopupOptions.Empty, shellParameters, TestContext.Current.CancellationToken);
		var popupPage = (PopupPage)shellNavigation.ModalStack.Last();

		// Act
		await popupPage.CloseAsync(new PopupResult<int>(expectedResult, false), TestContext.Current.CancellationToken);
		var result = await showPopupTask;

		// Assert
		var popupResult = Assert.IsType<IPopupResult<int>>(result.Value, exactMatch: false);
		Assert.Equal(expectedResult, popupResult.Result);
		Assert.False(popupResult.WasDismissedByTappingOutsideOfPopup);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryShowPopupAsync_ShouldReturnOperationCanceledException_WhenTokenIsCanceled()
	{
		// Arrange
		using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		await cancellationTokenSource.CancelAsync();

		// Act
		var result = await navigation.TryShowPopupAsync(new Popup(), PopupOptions.Empty, cancellationTokenSource.Token);

		// Assert
		Assert.IsType<OperationCanceledException>(result.Value, exactMatch: false);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryShowPopupAsync_Page_ShouldReturnOperationCanceledException_WhenTokenIsCanceled()
	{
		// Arrange
		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		await cancellationTokenSource.CancelAsync();

		// Act
		var result = await page.TryShowPopupAsync(new Popup(), PopupOptions.Empty, cancellationTokenSource.Token);

		// Assert
		Assert.IsType<OperationCanceledException>(result.Value, exactMatch: false);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryShowPopupAsync_Shell_ShouldReturnOperationCanceledException_WhenTokenIsCanceled()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		await cancellationTokenSource.CancelAsync();

		// Act
		var result = await shell.TryShowPopupAsync(new Popup(), PopupOptions.Empty, shellParameters, cancellationTokenSource.Token);

		// Assert
		Assert.IsType<OperationCanceledException>(result.Value, exactMatch: false);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryShowPopupAsyncT_ShouldReturnOperationCanceledException_WhenTokenIsCanceled()
	{
		// Arrange
		using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		await cancellationTokenSource.CancelAsync();

		// Act
		var result = await navigation.TryShowPopupAsync<int>(new Grid(), PopupOptions.Empty, cancellationTokenSource.Token);

		// Assert
		Assert.IsType<OperationCanceledException>(result.Value, exactMatch: false);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryShowPopupAsyncT_Page_ShouldReturnOperationCanceledException_WhenTokenIsCanceled()
	{
		// Arrange
		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		await cancellationTokenSource.CancelAsync();

		// Act
		var result = await page.TryShowPopupAsync<int>(new Grid(), PopupOptions.Empty, cancellationTokenSource.Token);

		// Assert
		Assert.IsType<OperationCanceledException>(result.Value, exactMatch: false);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryShowPopupAsyncT_Shell_ShouldReturnOperationCanceledException_WhenTokenIsCanceled()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		await cancellationTokenSource.CancelAsync();

		// Act
		var result = await shell.TryShowPopupAsync<int>(new ViewWithIQueryAttributable(new ViewModelWithIQueryAttributable()), PopupOptions.Empty, shellParameters, cancellationTokenSource.Token);

		// Assert
		Assert.IsType<OperationCanceledException>(result.Value, exactMatch: false);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public void TryShowPopupAsync_NullNavigation_ShouldThrowArgumentNullException()
	{
		// Act / Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => { _ = PopupExtensions.TryShowPopupAsync((INavigation?)null, new Popup(), PopupOptions.Empty, TestContext.Current.CancellationToken); });
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public void TryShowPopupAsync_NullView_ShouldThrowArgumentNullException()
	{
		// Act / Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => { _ = navigation.TryShowPopupAsync((View?)null, PopupOptions.Empty, TestContext.Current.CancellationToken); });
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public void TryShowPopupAsync_Page_NullPage_ShouldThrowArgumentNullException()
	{
		// Act / Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => { _ = PopupExtensions.TryShowPopupAsync((Page?)null, new Popup(), PopupOptions.Empty, TestContext.Current.CancellationToken); });
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public void TryShowPopupAsync_Page_NullView_ShouldThrowArgumentNullException()
	{
		// Arrange
		var page = GetWindowPage();

		// Act / Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => { _ = page.TryShowPopupAsync((View?)null, PopupOptions.Empty, TestContext.Current.CancellationToken); });
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public void TryShowPopupAsync_Shell_NullShell_ShouldThrowArgumentNullException()
	{
		// Act / Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => { _ = PopupExtensions.TryShowPopupAsync((Shell?)null, new Popup(), PopupOptions.Empty, shellParameters, TestContext.Current.CancellationToken); });
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public void TryShowPopupAsync_Shell_NullView_ShouldThrowArgumentNullException()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		// Act / Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => { _ = shell.TryShowPopupAsync((View?)null, PopupOptions.Empty, shellParameters, TestContext.Current.CancellationToken); });
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public void TryShowPopupAsyncT_NullNavigation_ShouldThrowArgumentNullException()
	{
		// Act / Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => { _ = PopupExtensions.TryShowPopupAsync<int>((INavigation?)null, new Popup(), PopupOptions.Empty, TestContext.Current.CancellationToken); });
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public void TryShowPopupAsyncT_NullView_ShouldThrowArgumentNullException()
	{
		// Act / Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => { _ = navigation.TryShowPopupAsync<int>((View?)null, PopupOptions.Empty, TestContext.Current.CancellationToken); });
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public void TryShowPopupAsyncT_Page_NullPage_ShouldThrowArgumentNullException()
	{
		// Act / Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => { _ = PopupExtensions.TryShowPopupAsync<int>((Page?)null, new Popup(), PopupOptions.Empty, TestContext.Current.CancellationToken); });
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public void TryShowPopupAsyncT_Page_NullView_ShouldThrowArgumentNullException()
	{
		// Arrange
		var page = GetWindowPage();

		// Act / Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => { _ = page.TryShowPopupAsync<int>((View?)null, PopupOptions.Empty, TestContext.Current.CancellationToken); });
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public void TryShowPopupAsyncT_Shell_NullShell_ShouldThrowArgumentNullException()
	{
		// Act / Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => { _ = PopupExtensions.TryShowPopupAsync<int>((Shell?)null, new Popup(), PopupOptions.Empty, shellParameters, TestContext.Current.CancellationToken); });
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public void TryShowPopupAsyncT_Shell_NullView_ShouldThrowArgumentNullException()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		// Act / Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => { _ = shell.TryShowPopupAsync<int>((View?)null, PopupOptions.Empty, shellParameters, TestContext.Current.CancellationToken); });
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryClosePopupAsync_ShouldClosePopupUsingNavigationAndReturnPopupResult()
	{
		// Arrange
		navigation.ShowPopup(new Popup());

		// Act
		var result = await navigation.TryClosePopupAsync(TestContext.Current.CancellationToken);

		// Assert
		Assert.Empty(navigation.ModalStack);
		var popupResult = Assert.IsType<IPopupResult>(result.Value, exactMatch: false);
		Assert.False(popupResult.WasDismissedByTappingOutsideOfPopup);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryClosePopupAsync_Page_ShouldClosePopupUsingPageAndReturnPopupResult()
	{
		// Arrange
		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		page.ShowPopup(new Popup());

		// Act
		var result = await page.TryClosePopupAsync(TestContext.Current.CancellationToken);

		// Assert
		Assert.Empty(page.Navigation.ModalStack);
		var popupResult = Assert.IsType<IPopupResult>(result.Value, exactMatch: false);
		Assert.False(popupResult.WasDismissedByTappingOutsideOfPopup);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryClosePopupAsyncT_ShouldClosePopupUsingNavigationAndReturnTypedPopupResult()
	{
		// Arrange
		const int expectedResult = 21;
		navigation.ShowPopup(new Popup());

		// Act
		var result = await navigation.TryClosePopupAsync(expectedResult, TestContext.Current.CancellationToken);

		// Assert
		Assert.Empty(navigation.ModalStack);
		var popupResult = Assert.IsType<IPopupResult<int>>(result.Value, exactMatch: false);
		Assert.Equal(expectedResult, popupResult.Result);
		Assert.False(popupResult.WasDismissedByTappingOutsideOfPopup);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryClosePopupAsyncT_Page_ShouldClosePopupUsingPageAndReturnTypedPopupResult()
	{
		// Arrange
		const int expectedResult = 34;

		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		page.ShowPopup(new Popup());

		// Act
		var result = await page.TryClosePopupAsync(expectedResult, TestContext.Current.CancellationToken);

		// Assert
		Assert.Empty(page.Navigation.ModalStack);
		var popupResult = Assert.IsType<IPopupResult<int>>(result.Value, exactMatch: false);
		Assert.Equal(expectedResult, popupResult.Result);
		Assert.False(popupResult.WasDismissedByTappingOutsideOfPopup);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryClosePopupAsync_ShouldReturnPopupNotFoundException_WhenNoPopupExists()
	{
		// Arrange

		// Act
		var result = await navigation.TryClosePopupAsync(TestContext.Current.CancellationToken);

		// Assert
		Assert.IsType<PopupNotFoundException>(result.Value);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryClosePopupAsync_Page_ShouldReturnPopupNotFoundException_WhenNoPopupExists()
	{
		// Arrange
		if (Application.Current?.Windows[0].Page is not Page page)
		{
			throw new InvalidOperationException("Page cannot be null");
		}

		// Act
		var result = await page.TryClosePopupAsync(TestContext.Current.CancellationToken);

		// Assert
		Assert.IsType<PopupNotFoundException>(result.Value);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryClosePopupAsync_ShouldReturnOperationCanceledException_WhenTokenIsCanceled()
	{
		// Arrange
		using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		await cancellationTokenSource.CancelAsync();

		// Act
		var result = await navigation.TryClosePopupAsync(cancellationTokenSource.Token);

		// Assert
		Assert.IsType<OperationCanceledException>(result.Value, exactMatch: false);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryClosePopupAsyncT_ShouldReturnPopupNotFoundException_WhenNoPopupExists()
	{
		// Arrange
		const int inputResult = 55;

		// Act
		var result = await navigation.TryClosePopupAsync(inputResult, TestContext.Current.CancellationToken);

		// Assert
		Assert.IsType<PopupNotFoundException>(result.Value);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryClosePopupAsyncT_ShouldReturnOperationCanceledException_WhenTokenIsCanceled()
	{
		// Arrange
		const int inputResult = 89;
		using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		await cancellationTokenSource.CancelAsync();

		// Act
		var result = await navigation.TryClosePopupAsync(inputResult, cancellationTokenSource.Token);

		// Assert
		Assert.IsType<OperationCanceledException>(result.Value, exactMatch: false);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public void TryClosePopupAsync_NullNavigation_ShouldThrowArgumentNullException()
	{
		// Act / Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => { _ = PopupExtensions.TryClosePopupAsync((INavigation?)null, TestContext.Current.CancellationToken); });
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public void TryClosePopupAsync_Page_NullPage_ShouldThrowArgumentNullException()
	{
		// Act / Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => { _ = PopupExtensions.TryClosePopupAsync((Page?)null, TestContext.Current.CancellationToken); });
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public void TryClosePopupAsyncT_NullNavigation_ShouldThrowArgumentNullException()
	{
		// Act / Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => { _ = PopupExtensions.TryClosePopupAsync((INavigation?)null, 5, TestContext.Current.CancellationToken); });
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public void TryClosePopupAsyncT_Page_NullPage_ShouldThrowArgumentNullException()
	{
		// Act / Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		Assert.Throws<ArgumentNullException>(() => { _ = PopupExtensions.TryClosePopupAsync((Page?)null, 5, TestContext.Current.CancellationToken); });
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryClosePopupAsync_ShouldReturnPopupBlockedException_WhenPopupIsBlocked()
	{
		// Arrange
		navigation.ShowPopup(new Button());
		await navigation.PushModalAsync(new ContentPage());

		// Act
		var result = await navigation.TryClosePopupAsync(TestContext.Current.CancellationToken);

		// Assert
		Assert.IsType<PopupBlockedException>(result.Value);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ClosePopupAsync_ShouldThrowPopupBlockedException_WhenTheTopModalPageIsAContentPageAndNoPopupExists()
	{
		// Arrange
		await navigation.PushModalAsync(new ContentPage());

		// Act and Assert
		await Assert.ThrowsAsync<PopupBlockedException>(() => navigation.ClosePopupAsync(TestContext.Current.CancellationToken));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryClosePopupAsync_ShouldReturnPopupBlockedException_WhenTheTopModalPageIsAContentPageAndNoPopupExists()
	{
		// Arrange
		await navigation.PushModalAsync(new ContentPage());

		// Act
		var result = await navigation.TryClosePopupAsync(TestContext.Current.CancellationToken);

		// Assert
		Assert.IsType<PopupBlockedException>(result.Value);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryClosePopupAsyncT_ShouldReturnPopupBlockedException_WhenPopupIsBlocked()
	{
		// Arrange
		const int inputResult = 2;
		navigation.ShowPopup(new Button());
		await navigation.PushModalAsync(new ContentPage());

		// Act
		var result = await navigation.TryClosePopupAsync(inputResult, TestContext.Current.CancellationToken);

		// Assert
		Assert.IsType<PopupBlockedException>(result.Value);
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
		var modalPage = Assert.Single(page.Navigation.ModalStack);
		Assert.IsType<PopupPage>(modalPage);

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
		var modalPage = Assert.Single(page.Navigation.ModalStack);
		Assert.IsType<PopupPage>(modalPage);

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
		var modalPage = Assert.Single(page.Navigation.ModalStack);
		Assert.IsType<PopupPage>(modalPage);

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
		var modalPage = Assert.Single(page.Navigation.ModalStack);
		Assert.IsType<PopupPage>(modalPage);

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
		var modalPage = Assert.Single(page.Navigation.ModalStack);
		Assert.IsType<PopupPage>(modalPage);

		// Act
		var popupResult = await page.ClosePopupAsync(expectedResult, TestContext.Current.CancellationToken);
		await showPopupAsyncTask;

		// Assert
		Assert.Empty(page.Navigation.ModalStack);
		Assert.Equal(expectedResult, popupResult.Result);
		Assert.False(popupResult.WasDismissedByTappingOutsideOfPopup);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public void ShowPopup_INavigation_SemaphoreReleasedAfterDisplay_AllowsAdditionalPopups()
	{
		// Arrange
		Assert.Empty(navigation.ModalStack);

		// Act - The first ShowPopup acquires the semaphore, pushes the popup and releases the semaphore
		navigation.ShowPopup(new Grid());

		// Assert
		Assert.Single(navigation.ModalStack);

		// Act - The second ShowPopup can only display if the semaphore was released after the first push
		TestContext.Current.CancellationToken.ThrowIfCancellationRequested();
		navigation.ShowPopup(new Grid());

		// Assert
		Assert.Equal(2, navigation.ModalStack.Count);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_INavigation_SemaphoreReleasedAfterPush_AllowsAdditionalPopupWhileFirstIsOpen()
	{
		// Arrange
		Assert.Empty(navigation.ModalStack);

		// Act - Display the first popup but do not close it; the semaphore is released after the push, not after the popup closes
		var firstShowPopupTask = navigation.ShowPopupAsync<object?>(new Popup(), PopupOptions.Empty, TestContext.Current.CancellationToken);

		// Assert
		Assert.Single(navigation.ModalStack);

		// Act - Display a second popup while the first is still open; this proves the semaphore was released after the first push
		var secondShowPopupTask = navigation.ShowPopupAsync<object?>(new Popup(), PopupOptions.Empty, TestContext.Current.CancellationToken);

		// Assert
		Assert.Equal(2, navigation.ModalStack.Count);

		// Act - Close both popups and ensure both awaiting tasks complete
		await navigation.ClosePopupAsync(TestContext.Current.CancellationToken);
		await secondShowPopupTask;
		await navigation.ClosePopupAsync(TestContext.Current.CancellationToken);
		await firstShowPopupTask;

		// Assert
		Assert.Empty(navigation.ModalStack);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_INavigation_SemaphoreReleasedBeforeResultIsReturned_AllowsNextPopupToDisplayImmediately()
	{
		using var nextPopupShown = new ManualResetEventSlim();
		var popup = new Popup();

		// Popup.Closed is raised after ShowPopupAsync's result is set: blocking it until the next popup is shown keeps the close in progress without depending on timing
		popup.Closed += (_, _) => nextPopupShown.Wait(TestContext.Current.CancellationToken);

		var showPopupTask = navigation.ShowPopupAsync(popup, PopupOptions.Empty, TestContext.Current.CancellationToken);

		// Closed on its own thread, like a popup that closes itself, because the Closed handler blocks it
		var closePopupTask = Task.Run(() => popup.CloseAsync(TestContext.Current.CancellationToken), TestContext.Current.CancellationToken);

		List<Page> modalStackAfterShowPopup;
		try
		{
			await showPopupTask;
			navigation.ShowPopup(new Grid());
			modalStackAfterShowPopup = [.. navigation.ModalStack];
		}
		finally
		{
			nextPopupShown.Set();
		}

		await closePopupTask;

		var modalPage = Assert.Single(modalStackAfterShowPopup);
		Assert.IsType<PopupPage>(modalPage);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public void ShowPopup_Shell_SemaphoreReleasedAfterDisplay_NullParameters_AllowsAdditionalPopups()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var shellNavigation = Shell.Current.Navigation;
		Assert.Empty(shellNavigation.ModalStack);

		// Act - Exercises the `shellParameters is null` branch which acquires and releases the semaphore around GoToAsync
		shell.ShowPopup(new Popup());

		// Assert
		Assert.Single(shellNavigation.ModalStack);

		// Act - The second display proves the semaphore was released by the null-parameters branch
		TestContext.Current.CancellationToken.ThrowIfCancellationRequested();
		shell.ShowPopup(new Popup());

		// Assert
		Assert.Equal(2, shellNavigation.ModalStack.Count);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public void ShowPopup_Shell_SemaphoreReleasedAfterDisplay_WithParameters_AllowsAdditionalPopups()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var shellNavigation = Shell.Current.Navigation;
		Assert.Empty(shellNavigation.ModalStack);

		var firstView = new ViewWithIQueryAttributable(new ViewModelWithIQueryAttributable());
		var secondView = new ViewWithIQueryAttributable(new ViewModelWithIQueryAttributable());

		// Act - Exercises the `shellParameters is not null` branch which acquires and releases the semaphore around GoToAsync
		shell.ShowPopup(firstView, PopupOptions.Empty, shellParameters);

		// Assert
		Assert.Single(shellNavigation.ModalStack);

		// Act - The second display proves the semaphore was released by the with-parameters branch
		TestContext.Current.CancellationToken.ThrowIfCancellationRequested();
		shell.ShowPopup(secondView, PopupOptions.Empty, shellParameters);

		// Assert
		Assert.Equal(2, shellNavigation.ModalStack.Count);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_INavigation_SemaphoreReleasedAfterCancellation_AllowsSubsequentPopup()
	{
		// Arrange
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		await cts.CancelAsync();

		// Act - A canceled token forces ShowPopupAsync to throw; the semaphore must not be leaked
		await Assert.ThrowsAsync<OperationCanceledException>(() => navigation.ShowPopupAsync(new Popup(), PopupOptions.Empty, cts.Token));

		// Assert - Canceling must not leave a popup on the modal stack
		Assert.Empty(navigation.ModalStack);

		// Act - A subsequent display can only succeed if the semaphore was released after cancellation
		navigation.ShowPopup(new Grid());

		// Assert
		Assert.Single(navigation.ModalStack);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_Shell_SemaphoreReleasedAfterCancellation_WithParameters_AllowsSubsequentPopup()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var shellNavigation = Shell.Current.Navigation;
		var view = new ViewWithIQueryAttributable(new ViewModelWithIQueryAttributable());

		using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		await cts.CancelAsync();

		// Act - A canceled token forces ShowPopupAsync to throw; the semaphore must not be leaked
		await Assert.ThrowsAsync<OperationCanceledException>(() => shell.ShowPopupAsync(view, PopupOptions.Empty, shellParameters, cts.Token));

		// Assert - Canceling must not leave a popup on the modal stack
		Assert.Empty(shellNavigation.ModalStack);

		// Act - A subsequent display can only succeed if the semaphore was released after cancellation
		shell.ShowPopup(new Popup());

		// Assert
		Assert.Single(shellNavigation.ModalStack);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_Shell_SemaphoreReleasedAfterCancellation_NullParameters_AllowsSubsequentPopup()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var shellNavigation = Shell.Current.Navigation;
		var view = new ViewWithIQueryAttributable(new ViewModelWithIQueryAttributable());

		using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		await cts.CancelAsync();

		// Act - A canceled token (with null shellParameters) forces ShowPopupAsync to throw; the semaphore must not be leaked
		await Assert.ThrowsAsync<OperationCanceledException>(() => shell.ShowPopupAsync(view, PopupOptions.Empty, shellParameters: null, cts.Token));

		// Assert - Canceling must not leave a popup on the modal stack
		Assert.Empty(shellNavigation.ModalStack);

		// Act - A subsequent display can only succeed if the semaphore was released after cancellation
		shell.ShowPopup(new Popup());

		// Assert
		Assert.Single(shellNavigation.ModalStack);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_INavigation_CancellationLeavesModalStackEmptyAndSemaphoreReleased()
	{
		// Arrange
		var cts = new CancellationTokenSource();
		await cts.CancelAsync();

		// Act - Cancel the awaitable display
		await Assert.ThrowsAsync<OperationCanceledException>(() => navigation.ShowPopupAsync(new Popup(), PopupOptions.Empty, cts.Token));

		// Assert - The canceled call must leave a clean modal stack
		Assert.Empty(navigation.ModalStack);

		// Act - The awaitable path must also recover; the semaphore was released by the finally block
		var showPopupTask = navigation.ShowPopupAsync<object?>(new Popup(), PopupOptions.Empty, TestContext.Current.CancellationToken);

		// Assert
		Assert.Single(navigation.ModalStack);

		// Act - Close the popup and ensure the task completes
		await navigation.ClosePopupAsync(TestContext.Current.CancellationToken);
		await showPopupTask;

		// Assert
		Assert.Empty(navigation.ModalStack);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopup_INavigation_SemaphoreSerializesMultipleDisplays_AllDisplayed()
	{
		// Arrange
		const int popupCount = 5;
		Assert.Empty(navigation.ModalStack);

		// Act - Each display must acquire the semaphore, push the popup and release the semaphore.
		// If the semaphore were not released after each push, the later popups would never be displayed.
		for (var i = 0; i < popupCount; i++)
		{
			navigation.ShowPopup(new Grid());
		}

		await WaitForModalStackCountAsync(popupCount, TestContext.Current.CancellationToken);

		// Assert
		Assert.Equal(popupCount, navigation.ModalStack.Count);

		async Task WaitForModalStackCountAsync(int expectedCount, CancellationToken token)
		{
			while (navigation.ModalStack.Count < expectedCount)
			{
				token.ThrowIfCancellationRequested();
				await Task.Delay(10, token);
			}
		}
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_NonNullableValueType_ReturnsDefault_WhenClosedWithoutTypedResult()
	{
		// Arrange
		var showPopupTask = navigation.ShowPopupAsync<int>(new Popup(), PopupOptions.Empty, TestContext.Current.CancellationToken);
		var popupPage = (PopupPage)navigation.ModalStack[^1];

		// Act
		await popupPage.CloseAsync(new PopupResult(false), TestContext.Current.CancellationToken);
		var result = await showPopupTask;

		// Assert
		Assert.False(result.WasDismissedByTappingOutsideOfPopup);
		Assert.Equal(0, result.Result);
	}

	[Theory(Timeout = (int)TestDuration.Short)]
	[InlineData(RootPageType.Shell)]
	[InlineData(RootPageType.ContentPage)]
	[InlineData(RootPageType.NavigationPage)]
	public void ShowPopupAsync_Page_DisplaysPopupOnPageBelow_WhenCalledInOnDisappearingOfModalPageBeingPopped(RootPageType rootPageType)
	{
		// AsyncContext is single-threaded, like the UI thread of an app.
		AsyncContext.Run(async () =>
		{
			var token = TestContext.Current.CancellationToken;
			var popup = new Popup();
			var popupOpenedTask = GetOpenedTask(popup);

			Task<IPopupResult>? showPopupTask = null;
			var (window, modalPage) = await PushModalPageThatShowsPopupWhenItDisappearsAsync(rootPageType, page => showPopupTask = page.ShowPopupAsync(popup, PopupOptions.Empty, token));

			await modalPage.Navigation.PopModalAsync();
			await WaitForPopupToOpenAsync(popupOpenedTask, token);

			var popupPage = Assert.IsType<PopupPage>(Assert.Single(window.Navigation.ModalStack));
			Assert.Same(popup, popupPage.Content.PopupBorder.Content);
			Assert.NotNull(showPopupTask);
			Assert.False(showPopupTask.IsCompleted);

			await popup.CloseAsync(token);
			var popupResult = await showPopupTask.WaitAsync(token);

			Assert.Empty(window.Navigation.ModalStack);
			Assert.False(popupResult.WasDismissedByTappingOutsideOfPopup);
		});
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public void ShowPopup_Page_DisplaysPopupOnPageBelow_WhenCalledInOnDisappearingOfModalPageBeingPopped()
	{
		// AsyncContext is single-threaded, like the UI thread of an app.
		AsyncContext.Run(async () =>
		{
			var token = TestContext.Current.CancellationToken;
			var popup = new Popup();
			var popupOpenedTask = GetOpenedTask(popup);

			var (window, modalPage) = await PushModalPageThatShowsPopupWhenItDisappearsAsync(RootPageType.Shell, page => page.ShowPopup(popup, PopupOptions.Empty));

			await modalPage.Navigation.PopModalAsync();
			await WaitForPopupToOpenAsync(popupOpenedTask, token);

			var popupPage = Assert.IsType<PopupPage>(Assert.Single(window.Navigation.ModalStack));
			Assert.Same(popup, popupPage.Content.PopupBorder.Content);

			await popup.CloseAsync(token);

			Assert.Empty(window.Navigation.ModalStack);
		});
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public void ShowPopupAsyncT_Page_DisplaysPopupOnPageBelow_WhenCalledInOnDisappearingOfModalPageBeingPopped()
	{
		// AsyncContext is single-threaded, like the UI thread of an app.
		AsyncContext.Run(async () =>
		{
			const string expectedResult = "Closed";
			var token = TestContext.Current.CancellationToken;
			var popup = new Popup<string>();
			var popupOpenedTask = GetOpenedTask(popup);

			Task<IPopupResult<string>>? showPopupTask = null;
			var (window, modalPage) = await PushModalPageThatShowsPopupWhenItDisappearsAsync(RootPageType.Shell, page => showPopupTask = page.ShowPopupAsync<string>(popup, PopupOptions.Empty, token));

			await modalPage.Navigation.PopModalAsync();
			await WaitForPopupToOpenAsync(popupOpenedTask, token);

			var popupPage = Assert.IsType<PopupPage>(Assert.Single(window.Navigation.ModalStack));
			Assert.Same(popup, popupPage.Content.PopupBorder.Content);
			Assert.NotNull(showPopupTask);
			Assert.False(showPopupTask.IsCompleted);

			await popup.CloseAsync(expectedResult, token);
			var popupResult = await showPopupTask.WaitAsync(token);

			Assert.Empty(window.Navigation.ModalStack);
			Assert.Equal(expectedResult, popupResult.Result);
			Assert.False(popupResult.WasDismissedByTappingOutsideOfPopup);
		});
	}

	[Theory(Timeout = (int)TestDuration.Short)]
	[InlineData(RootPageType.Shell)]
	[InlineData(RootPageType.ContentPage)]
	[InlineData(RootPageType.NavigationPage)]
	public void TryShowPopupAsync_Page_DisplaysPopupOnPageBelow_WhenCalledInOnDisappearingOfModalPageBeingPopped(RootPageType rootPageType)
	{
		// AsyncContext is single-threaded, like the UI thread of an app.
		AsyncContext.Run(async () =>
		{
			var token = TestContext.Current.CancellationToken;
			var popup = new Popup();
			var popupOpenedTask = GetOpenedTask(popup);

			Task<PopupResultOrException>? showPopupTask = null;
			var (window, modalPage) = await PushModalPageThatShowsPopupWhenItDisappearsAsync(rootPageType, page => showPopupTask = page.TryShowPopupAsync(popup, PopupOptions.Empty, token));

			await modalPage.Navigation.PopModalAsync();
			await WaitForPopupToOpenAsync(popupOpenedTask, token);

			var popupPage = Assert.IsType<PopupPage>(Assert.Single(window.Navigation.ModalStack));
			Assert.Same(popup, popupPage.Content.PopupBorder.Content);
			Assert.NotNull(showPopupTask);
			Assert.False(showPopupTask.IsCompleted);

			await popup.CloseAsync(token);
			var result = await showPopupTask.WaitAsync(token);

			Assert.Empty(window.Navigation.ModalStack);
			var popupResult = Assert.IsType<IPopupResult>(result.Value, exactMatch: false);
			Assert.False(popupResult.WasDismissedByTappingOutsideOfPopup);
		});
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public void TryShowPopupAsyncT_Page_DisplaysPopupOnPageBelow_WhenCalledInOnDisappearingOfModalPageBeingPopped()
	{
		// AsyncContext is single-threaded, like the UI thread of an app.
		AsyncContext.Run(async () =>
		{
			const string expectedResult = "Closed";
			var token = TestContext.Current.CancellationToken;
			var popup = new Popup<string>();
			var popupOpenedTask = GetOpenedTask(popup);

			Task<PopupResultOrException<string>>? showPopupTask = null;
			var (window, modalPage) = await PushModalPageThatShowsPopupWhenItDisappearsAsync(RootPageType.Shell, page => showPopupTask = page.TryShowPopupAsync<string>(popup, PopupOptions.Empty, token));

			await modalPage.Navigation.PopModalAsync();
			await WaitForPopupToOpenAsync(popupOpenedTask, token);

			var popupPage = Assert.IsType<PopupPage>(Assert.Single(window.Navigation.ModalStack));
			Assert.Same(popup, popupPage.Content.PopupBorder.Content);
			Assert.NotNull(showPopupTask);
			Assert.False(showPopupTask.IsCompleted);

			await popup.CloseAsync(expectedResult, token);
			var result = await showPopupTask.WaitAsync(token);

			Assert.Empty(window.Navigation.ModalStack);
			var popupResult = Assert.IsType<IPopupResult<string>>(result.Value, exactMatch: false);
			Assert.Equal(expectedResult, popupResult.Result);
			Assert.False(popupResult.WasDismissedByTappingOutsideOfPopup);
		});
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryShowPopupAsync_ShouldReturnException_WhenPopupInitializationThrows()
	{
		// Arrange
		var expectedException = new InvalidOperationException("Popup initialization failed");
		new Options().SetPopupDefaults(new DefaultPopupSettings { OnInitializing = _ => throw expectedException });

		// Act
		var result = await navigation.TryShowPopupAsync(new Popup(), PopupOptions.Empty, TestContext.Current.CancellationToken);

		// Assert
		Assert.Same(expectedException, result.Value);
		Assert.Empty(navigation.ModalStack);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryShowPopupAsync_Page_ShouldReturnException_WhenPopupInitializationThrows()
	{
		// Arrange
		var expectedException = new InvalidOperationException("Popup initialization failed");
		new Options().SetPopupDefaults(new DefaultPopupSettings { OnInitializing = _ => throw expectedException });
		var page = GetWindowPage();

		// Act
		var result = await page.TryShowPopupAsync(new Popup(), PopupOptions.Empty, TestContext.Current.CancellationToken);

		// Assert
		Assert.Same(expectedException, result.Value);
		Assert.Empty(page.Navigation.ModalStack);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryShowPopupAsync_Shell_ShouldReturnException_WhenPopupInitializationThrows()
	{
		// Arrange
		var expectedException = new InvalidOperationException("Popup initialization failed");
		new Options().SetPopupDefaults(new DefaultPopupSettings { OnInitializing = _ => throw expectedException });
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		// Act
		var result = await shell.TryShowPopupAsync(new Popup(), PopupOptions.Empty, shellParameters, TestContext.Current.CancellationToken);

		// Assert
		Assert.Same(expectedException, result.Value);
		Assert.Empty(shell.Navigation.ModalStack);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryShowPopupAsyncT_ShouldReturnException_WhenPopupInitializationThrows()
	{
		// Arrange
		var expectedException = new InvalidOperationException("Popup initialization failed");
		new Options().SetPopupDefaults(new DefaultPopupSettings { OnInitializing = _ => throw expectedException });

		// Act
		var result = await navigation.TryShowPopupAsync<int>(new Grid(), PopupOptions.Empty, TestContext.Current.CancellationToken);

		// Assert
		Assert.Same(expectedException, result.Value);
		Assert.Empty(navigation.ModalStack);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryShowPopupAsyncT_Page_ShouldReturnException_WhenPopupInitializationThrows()
	{
		// Arrange
		var expectedException = new InvalidOperationException("Popup initialization failed");
		new Options().SetPopupDefaults(new DefaultPopupSettings { OnInitializing = _ => throw expectedException });
		var page = GetWindowPage();

		// Act
		var result = await page.TryShowPopupAsync<int>(new Grid(), PopupOptions.Empty, TestContext.Current.CancellationToken);

		// Assert
		Assert.Same(expectedException, result.Value);
		Assert.Empty(page.Navigation.ModalStack);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryShowPopupAsyncT_Shell_ShouldReturnException_WhenPopupInitializationThrows()
	{
		// Arrange
		var expectedException = new InvalidOperationException("Popup initialization failed");
		new Options().SetPopupDefaults(new DefaultPopupSettings { OnInitializing = _ => throw expectedException });
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		// Act
		var result = await shell.TryShowPopupAsync<int>(new Grid(), PopupOptions.Empty, shellParameters, TestContext.Current.CancellationToken);

		// Assert
		Assert.Same(expectedException, result.Value);
		Assert.Empty(shell.Navigation.ModalStack);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryShowPopupAsync_ShouldReturnOperationCanceledExceptionAndKeepPopupOpen_WhenTokenIsCanceledWhilePopupIsOpen()
	{
		// Arrange
		using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		var showPopupTask = navigation.TryShowPopupAsync(new Popup(), PopupOptions.Empty, cancellationTokenSource.Token);
		Assert.IsType<PopupPage>(Assert.Single(navigation.ModalStack));
		Assert.False(showPopupTask.IsCompleted);

		// Act
		await cancellationTokenSource.CancelAsync();
		var result = await showPopupTask;

		// Assert
		Assert.IsType<OperationCanceledException>(result.Value, exactMatch: false);
		Assert.IsType<PopupPage>(Assert.Single(navigation.ModalStack));

		var closeResult = await navigation.TryClosePopupAsync(TestContext.Current.CancellationToken);
		var popupResult = Assert.IsType<IPopupResult>(closeResult.Value, exactMatch: false);
		Assert.False(popupResult.WasDismissedByTappingOutsideOfPopup);
		Assert.Empty(navigation.ModalStack);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryShowPopupAsync_Shell_ShouldReturnOperationCanceledExceptionAndKeepPopupOpen_WhenTokenIsCanceledWhilePopupIsOpen()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;
		var shellNavigation = shell.Navigation;

		using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		var showPopupTask = shell.TryShowPopupAsync(new Popup(), PopupOptions.Empty, shellParameters, cancellationTokenSource.Token);
		Assert.IsType<PopupPage>(Assert.Single(shellNavigation.ModalStack));
		Assert.False(showPopupTask.IsCompleted);

		// Act
		await cancellationTokenSource.CancelAsync();
		var result = await showPopupTask;

		// Assert
		Assert.IsType<OperationCanceledException>(result.Value, exactMatch: false);
		Assert.IsType<PopupPage>(Assert.Single(shellNavigation.ModalStack));

		var closeResult = await shell.TryClosePopupAsync(TestContext.Current.CancellationToken);
		var popupResult = Assert.IsType<IPopupResult>(closeResult.Value, exactMatch: false);
		Assert.False(popupResult.WasDismissedByTappingOutsideOfPopup);
		Assert.Empty(shellNavigation.ModalStack);
		Assert.DoesNotContain("PopupPage", shell.CurrentState.Location.OriginalString, StringComparison.Ordinal);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryShowPopupAsyncT_ShouldReturnOperationCanceledExceptionAndKeepPopupOpen_WhenTokenIsCanceledWhilePopupIsOpen()
	{
		// Arrange
		using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		var showPopupTask = navigation.TryShowPopupAsync<int>(new Grid(), PopupOptions.Empty, cancellationTokenSource.Token);
		Assert.IsType<PopupPage>(Assert.Single(navigation.ModalStack));
		Assert.False(showPopupTask.IsCompleted);

		// Act
		await cancellationTokenSource.CancelAsync();
		var result = await showPopupTask;

		// Assert
		Assert.IsType<OperationCanceledException>(result.Value, exactMatch: false);
		Assert.IsType<PopupPage>(Assert.Single(navigation.ModalStack));

		var closeResult = await navigation.TryClosePopupAsync(TestContext.Current.CancellationToken);
		var popupResult = Assert.IsType<IPopupResult>(closeResult.Value, exactMatch: false);
		Assert.False(popupResult.WasDismissedByTappingOutsideOfPopup);
		Assert.Empty(navigation.ModalStack);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryShowPopupAsyncT_Shell_ShouldReturnOperationCanceledExceptionAndKeepPopupOpen_WhenTokenIsCanceledWhilePopupIsOpen()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;
		var shellNavigation = shell.Navigation;

		using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		var showPopupTask = shell.TryShowPopupAsync<int>(new Grid(), PopupOptions.Empty, shellParameters, cancellationTokenSource.Token);
		Assert.IsType<PopupPage>(Assert.Single(shellNavigation.ModalStack));
		Assert.False(showPopupTask.IsCompleted);

		// Act
		await cancellationTokenSource.CancelAsync();
		var result = await showPopupTask;

		// Assert
		Assert.IsType<OperationCanceledException>(result.Value, exactMatch: false);
		Assert.IsType<PopupPage>(Assert.Single(shellNavigation.ModalStack));

		var closeResult = await shell.TryClosePopupAsync(TestContext.Current.CancellationToken);
		var popupResult = Assert.IsType<IPopupResult>(closeResult.Value, exactMatch: false);
		Assert.False(popupResult.WasDismissedByTappingOutsideOfPopup);
		Assert.Empty(shellNavigation.ModalStack);
		Assert.DoesNotContain("PopupPage", shell.CurrentState.Location.OriginalString, StringComparison.Ordinal);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryClosePopupAsync_Page_ShouldReturnPopupBlockedException_WhenPopupIsBlocked()
	{
		// Arrange
		var page = GetWindowPage();
		page.ShowPopup(new Button());
		await page.Navigation.PushModalAsync(new ContentPage());

		// Act
		var result = await page.TryClosePopupAsync(TestContext.Current.CancellationToken);

		// Assert
		Assert.IsType<PopupBlockedException>(result.Value);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryClosePopupAsync_Page_ShouldReturnOperationCanceledException_WhenTokenIsCanceled()
	{
		// Arrange
		var page = GetWindowPage();
		using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		await cancellationTokenSource.CancelAsync();

		// Act
		var result = await page.TryClosePopupAsync(cancellationTokenSource.Token);

		// Assert
		Assert.IsType<OperationCanceledException>(result.Value, exactMatch: false);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryClosePopupAsyncT_Page_ShouldReturnPopupNotFoundException_WhenNoPopupExists()
	{
		// Arrange
		var page = GetWindowPage();

		// Act
		var result = await page.TryClosePopupAsync(1, TestContext.Current.CancellationToken);

		// Assert
		Assert.IsType<PopupNotFoundException>(result.Value);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryClosePopupAsyncT_Page_ShouldReturnPopupBlockedException_WhenPopupIsBlocked()
	{
		// Arrange
		var page = GetWindowPage();
		page.ShowPopup(new Button());
		await page.Navigation.PushModalAsync(new ContentPage());

		// Act
		var result = await page.TryClosePopupAsync(1, TestContext.Current.CancellationToken);

		// Assert
		Assert.IsType<PopupBlockedException>(result.Value);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryClosePopupAsyncT_Page_ShouldReturnOperationCanceledException_WhenTokenIsCanceled()
	{
		// Arrange
		var page = GetWindowPage();
		using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		await cancellationTokenSource.CancelAsync();

		// Act
		var result = await page.TryClosePopupAsync(1, cancellationTokenSource.Token);

		// Assert
		Assert.IsType<OperationCanceledException>(result.Value, exactMatch: false);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryShowPopupAsync_ShouldReturnDismissedResult_WhenPopupIsDismissedByTappingOutsideOfPopup()
	{
		// Arrange
		var showPopupTask = navigation.TryShowPopupAsync(new Popup(), PopupOptions.Empty, TestContext.Current.CancellationToken);
		var popupPage = (PopupPage)navigation.ModalStack.Last();

		// Act
		await popupPage.CloseAsync(new PopupResult(true), TestContext.Current.CancellationToken);
		var result = await showPopupTask;

		// Assert
		var popupResult = Assert.IsType<IPopupResult>(result.Value, exactMatch: false);
		Assert.True(popupResult.WasDismissedByTappingOutsideOfPopup);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryShowPopupAsync_Page_ShouldReturnDismissedResult_WhenPopupIsDismissedByTappingOutsideOfPopup()
	{
		// Arrange
		var page = GetWindowPage();
		var showPopupTask = page.TryShowPopupAsync(new Popup(), PopupOptions.Empty, TestContext.Current.CancellationToken);
		var popupPage = (PopupPage)page.Navigation.ModalStack.Last();

		// Act
		await popupPage.CloseAsync(new PopupResult(true), TestContext.Current.CancellationToken);
		var result = await showPopupTask;

		// Assert
		var popupResult = Assert.IsType<IPopupResult>(result.Value, exactMatch: false);
		Assert.True(popupResult.WasDismissedByTappingOutsideOfPopup);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryShowPopupAsync_Shell_ShouldReturnDismissedResult_WhenPopupIsDismissedByTappingOutsideOfPopup()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var showPopupTask = shell.TryShowPopupAsync(new Popup(), PopupOptions.Empty, shellParameters, TestContext.Current.CancellationToken);
		var popupPage = (PopupPage)shell.Navigation.ModalStack.Last();

		// Act
		await popupPage.CloseAsync(new PopupResult(true), TestContext.Current.CancellationToken);
		var result = await showPopupTask;

		// Assert
		var popupResult = Assert.IsType<IPopupResult>(result.Value, exactMatch: false);
		Assert.True(popupResult.WasDismissedByTappingOutsideOfPopup);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryShowPopupAsyncT_Page_ShouldReturnDismissedResult_WhenPopupIsDismissedByTappingOutsideOfPopup()
	{
		// Arrange
		var page = GetWindowPage();
		var showPopupTask = page.TryShowPopupAsync<string>(new Popup<string>(), PopupOptions.Empty, TestContext.Current.CancellationToken);
		var popupPage = (PopupPage)page.Navigation.ModalStack.Last();

		// Act
		await popupPage.CloseAsync(new PopupResult(true), TestContext.Current.CancellationToken);
		var result = await showPopupTask;

		// Assert
		var popupResult = Assert.IsType<IPopupResult<string>>(result.Value, exactMatch: false);
		Assert.True(popupResult.WasDismissedByTappingOutsideOfPopup);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryShowPopupAsyncT_Shell_ShouldReturnDismissedResult_WhenPopupIsDismissedByTappingOutsideOfPopup()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var showPopupTask = shell.TryShowPopupAsync<string>(new Popup<string>(), PopupOptions.Empty, shellParameters, TestContext.Current.CancellationToken);
		var popupPage = (PopupPage)shell.Navigation.ModalStack.Last();

		// Act
		await popupPage.CloseAsync(new PopupResult(true), TestContext.Current.CancellationToken);
		var result = await showPopupTask;

		// Assert
		var popupResult = Assert.IsType<IPopupResult<string>>(result.Value, exactMatch: false);
		Assert.True(popupResult.WasDismissedByTappingOutsideOfPopup);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryShowPopupAsyncT_ShouldReturnDefaultResult_WhenPopupIsClosedWithResultOfAnotherType()
	{
		// Arrange
		var popup = new Popup<string>();
		var showPopupTask = navigation.TryShowPopupAsync<int>(popup, PopupOptions.Empty, TestContext.Current.CancellationToken);

		// Act
		await popup.CloseAsync("42", TestContext.Current.CancellationToken);
		var result = await showPopupTask;

		// Assert
		var popupResult = Assert.IsType<IPopupResult<int>>(result.Value, exactMatch: false);
		Assert.False(popupResult.WasDismissedByTappingOutsideOfPopup);
		Assert.Equal(0, popupResult.Result);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryShowPopupAsyncT_Shell_ShouldReturnDefaultResult_WhenPopupIsClosedWithResultOfAnotherType()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var popup = new Popup<string>();
		var showPopupTask = shell.TryShowPopupAsync<int>(popup, PopupOptions.Empty, shellParameters, TestContext.Current.CancellationToken);

		// Act
		await popup.CloseAsync("42", TestContext.Current.CancellationToken);
		var result = await showPopupTask;

		// Assert
		var popupResult = Assert.IsType<IPopupResult<int>>(result.Value, exactMatch: false);
		Assert.False(popupResult.WasDismissedByTappingOutsideOfPopup);
		Assert.Equal(0, popupResult.Result);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsyncT_ShouldReturnDefaultResult_WhenPopupIsClosedWithResultOfAnotherType()
	{
		// Arrange
		var popup = new Popup<string>();
		var showPopupTask = navigation.ShowPopupAsync<int>(popup, PopupOptions.Empty, TestContext.Current.CancellationToken);

		// Act
		await popup.CloseAsync("42", TestContext.Current.CancellationToken);
		var result = await showPopupTask;

		// Assert
		Assert.False(result.WasDismissedByTappingOutsideOfPopup);
		Assert.Equal(0, result.Result);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsyncT_Shell_ShouldReturnDefaultResult_WhenPopupIsClosedWithResultOfAnotherType()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var popup = new Popup<string>();
		var showPopupTask = shell.ShowPopupAsync<int>(popup, PopupOptions.Empty, shellParameters, TestContext.Current.CancellationToken);

		// Act
		await popup.CloseAsync("42", TestContext.Current.CancellationToken);
		var result = await showPopupTask;

		// Assert
		Assert.False(result.WasDismissedByTappingOutsideOfPopup);
		Assert.Equal(0, result.Result);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsyncT_ShouldReturnNullResult_WhenPopupOfDerivedTypeIsAwaitedAsBaseType()
	{
		// Arrange
		var popup = new Popup<string>();
		var showPopupTask = navigation.ShowPopupAsync<object>(popup, PopupOptions.Empty, TestContext.Current.CancellationToken);

		// Act
		await popup.CloseAsync("hello", TestContext.Current.CancellationToken);
		var result = await showPopupTask;

		// Assert
		Assert.False(result.WasDismissedByTappingOutsideOfPopup);
		Assert.Null(result.Result);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsyncT_Shell_ShouldReturnNullResult_WhenPopupOfDerivedTypeIsAwaitedAsBaseType()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		var popup = new Popup<string>();
		var showPopupTask = shell.ShowPopupAsync<object>(popup, PopupOptions.Empty, shellParameters, TestContext.Current.CancellationToken);

		// Act
		await popup.CloseAsync("hello", TestContext.Current.CancellationToken);
		var result = await showPopupTask;

		// Assert
		Assert.False(result.WasDismissedByTappingOutsideOfPopup);
		Assert.Null(result.Result);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryShowPopupAsync_Shell_WithoutOptions_ShouldUseShellRoute()
	{
		// Arrange
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		// Act
		var showPopupTask = shell.TryShowPopupAsync(new Popup(), token: TestContext.Current.CancellationToken);

		// Assert
		Assert.IsType<PopupPage>(Assert.Single(shell.Navigation.ModalStack));
		Assert.Matches("/PopupPage[0-9a-f-]{36}$", shell.CurrentState.Location.OriginalString);

		var closeResult = await shell.TryClosePopupAsync(TestContext.Current.CancellationToken);
		Assert.IsType<IPopupResult>(closeResult.Value, exactMatch: false);
		Assert.IsType<IPopupResult>((await showPopupTask).Value, exactMatch: false);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryShowPopupAsync_Shell_WithShellParametersAndWithoutOptions_ShouldApplyShellParameters()
	{
		// Arrange
		var viewWithQueryable = new ViewWithIQueryAttributable(new ViewModelWithIQueryAttributable());
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));

		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shell;

		// Act
		var showPopupTask = shell.TryShowPopupAsync(viewWithQueryable, shellParameters: shellParameters, token: TestContext.Current.CancellationToken);

		// Assert
		Assert.IsType<PopupPage>(Assert.Single(shell.Navigation.ModalStack));
		Assert.Equal(shellParameterBackgroundColorValue, viewWithQueryable.BackgroundColor);
		Assert.Equal(shellParameterViewModelTextValue, viewWithQueryable.BindingContext.Text);

		await shell.TryClosePopupAsync(TestContext.Current.CancellationToken);
		Assert.IsType<IPopupResult>((await showPopupTask).Value, exactMatch: false);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryShowPopupAsync_Navigation_WithoutOptions_ShouldReturnPopupResult()
	{
		// Arrange
		var showPopupTask = navigation.TryShowPopupAsync(new Popup(), token: TestContext.Current.CancellationToken);
		var popupPage = (PopupPage)navigation.ModalStack.Last();

		// Act
		await popupPage.CloseAsync(new PopupResult(false), TestContext.Current.CancellationToken);
		var result = await showPopupTask;

		// Assert
		var popupResult = Assert.IsType<IPopupResult>(result.Value, exactMatch: false);
		Assert.False(popupResult.WasDismissedByTappingOutsideOfPopup);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task TryShowPopupAsync_Page_WithoutOptions_ShouldReturnPopupResult()
	{
		// Arrange
		var page = GetWindowPage();
		var showPopupTask = page.TryShowPopupAsync(new Popup(), token: TestContext.Current.CancellationToken);
		var popupPage = (PopupPage)page.Navigation.ModalStack.Last();

		// Act
		await popupPage.CloseAsync(new PopupResult(false), TestContext.Current.CancellationToken);
		var result = await showPopupTask;

		// Assert
		var popupResult = Assert.IsType<IPopupResult>(result.Value, exactMatch: false);
		Assert.False(popupResult.WasDismissedByTappingOutsideOfPopup);
	}

	public enum ClosePopupMethod
	{
		PageExtension,
		PageExtensionWithResult,
		NavigationExtension,
		NavigationExtensionWithResult,
		TryPageExtension,
		TryPageExtensionWithResult,
		TryNavigationExtension,
		TryNavigationExtensionWithResult,
		PopupServiceWithPage,
		PopupServiceWithPageAndResult,
		PopupServiceWithNavigation,
		PopupServiceWithNavigationAndResult,
		TryPopupServiceWithPage,
		TryPopupServiceWithPageAndResult,
		TryPopupServiceWithNavigation,
		TryPopupServiceWithNavigationAndResult
	}

	public static TheoryData<ClosePopupMethod> ClosePopupMethods { get; } = [.. Enum.GetValues<ClosePopupMethod>()];

	[Theory(Timeout = (int)TestDuration.Medium)]
	[MemberData(nameof(ClosePopupMethods))]
	public async Task ClosePopup_TwoWindowsWithoutShell_ShouldCloseOnlyPopupOfWindowThatWasPassedIn(ClosePopupMethod closePopupMethod)
	{
		// Arrange
		var (_, pageB) = OpenSecondWindow(new MockPage(new MockPageViewModel()));
		var showPopupBTask = pageB.ShowPopupAsync<string>(new Grid(), PopupOptions.Empty, TestContext.Current.CancellationToken);
		Assert.IsType<PopupPage>(Assert.Single(pageB.Navigation.ModalStack));

		// Act
		var result = await ClosePopupAsync(closePopupMethod, pageB, "b", TestContext.Current.CancellationToken);

		// Assert
		Assert.IsType<IPopupResult>(result.Value, exactMatch: false);
		Assert.Empty(pageB.Navigation.ModalStack);
		Assert.IsType<IPopupResult<string>>(await showPopupBTask, exactMatch: false);
	}

	[Theory(Timeout = (int)TestDuration.Medium)]
	[MemberData(nameof(ClosePopupMethods))]
	public async Task ClosePopup_ActivatedShellInOtherWindowWithPopup_ShouldCloseOnlyPopupOfWindowThatWasPassedIn(ClosePopupMethod closePopupMethod)
	{
		// Arrange
		var shellA = ActivateShellInFirstWindow();
		var (_, pageB) = OpenSecondWindow(new MockPage(new MockPageViewModel()));

		var showPopupATask = shellA.ShowPopupAsync<string>(new Grid(), PopupOptions.Empty, null, TestContext.Current.CancellationToken);
		var showPopupBTask = pageB.ShowPopupAsync<string>(new Grid(), PopupOptions.Empty, TestContext.Current.CancellationToken);
		Assert.Single(shellA.Navigation.ModalStack);
		Assert.Single(pageB.Navigation.ModalStack);

		// Act
		var result = await ClosePopupAsync(closePopupMethod, pageB, "b", TestContext.Current.CancellationToken);

		// Assert
		Assert.IsType<IPopupResult>(result.Value, exactMatch: false);
		Assert.Empty(pageB.Navigation.ModalStack);
		Assert.IsType<IPopupResult<string>>(await showPopupBTask, exactMatch: false);
		Assert.False(showPopupATask.IsCompleted);
		Assert.IsType<PopupPage>(Assert.Single(shellA.Navigation.ModalStack));

		await shellA.TryClosePopupAsync(TestContext.Current.CancellationToken);
		Assert.IsType<IPopupResult<string>>(await showPopupATask, exactMatch: false);
	}

	[Theory(Timeout = (int)TestDuration.Medium)]
	[MemberData(nameof(ClosePopupMethods))]
	public async Task ClosePopup_ActivatedShellInOtherWindowWithoutPopup_ShouldClosePopupOfWindowThatWasPassedIn(ClosePopupMethod closePopupMethod)
	{
		// Arrange
		var shellA = ActivateShellInFirstWindow();
		var (_, pageB) = OpenSecondWindow(new MockPage(new MockPageViewModel()));
		var showPopupBTask = pageB.ShowPopupAsync<string>(new Grid(), PopupOptions.Empty, TestContext.Current.CancellationToken);

		// Act
		var result = await ClosePopupAsync(closePopupMethod, pageB, "b", TestContext.Current.CancellationToken);

		// Assert
		Assert.IsType<IPopupResult>(result.Value, exactMatch: false);
		Assert.Empty(pageB.Navigation.ModalStack);
		Assert.Empty(shellA.Navigation.ModalStack);
		Assert.IsType<IPopupResult<string>>(await showPopupBTask, exactMatch: false);
	}

	[Theory(Timeout = (int)TestDuration.Medium)]
	[MemberData(nameof(ClosePopupMethods))]
	public async Task ClosePopup_ActivatedShellInOtherWindowWithPopupAndNoPopupInWindowThatWasPassedIn_ShouldReturnPopupNotFoundException(ClosePopupMethod closePopupMethod)
	{
		// Arrange
		var shellA = ActivateShellInFirstWindow();
		var (_, pageB) = OpenSecondWindow(new MockPage(new MockPageViewModel()));
		var showPopupATask = shellA.ShowPopupAsync<string>(new Grid(), PopupOptions.Empty, null, TestContext.Current.CancellationToken);

		// Act
		var result = await ClosePopupAsync(closePopupMethod, pageB, "b", TestContext.Current.CancellationToken);

		// Assert
		Assert.IsType<PopupNotFoundException>(result.Value);
		Assert.False(showPopupATask.IsCompleted);
		Assert.IsType<PopupPage>(Assert.Single(shellA.Navigation.ModalStack));

		await shellA.TryClosePopupAsync(TestContext.Current.CancellationToken);
		Assert.IsType<IPopupResult<string>>(await showPopupATask, exactMatch: false);
	}

	[Theory(Timeout = (int)TestDuration.Medium)]
	[MemberData(nameof(ClosePopupMethods))]
	public async Task ClosePopup_TwoShellWindows_ShouldCloseOwnPopupOfEachWindow(ClosePopupMethod closePopupMethod)
	{
		// Arrange
		var shellA = CreateShell();
		Assert.NotNull(Application.Current);
		Application.Current.Windows[0].Page = shellA;
		var (_, shellB) = OpenSecondWindow(CreateShell());

		var showPopupATask = shellA.ShowPopupAsync<string>(new Grid(), PopupOptions.Empty, null, TestContext.Current.CancellationToken);
		var showPopupBTask = shellB.ShowPopupAsync<string>(new Grid(), PopupOptions.Empty, null, TestContext.Current.CancellationToken);
		Assert.Single(shellA.Navigation.ModalStack);
		Assert.Single(shellB.Navigation.ModalStack);

		// Act
		var resultB = await ClosePopupAsync(closePopupMethod, shellB, "b", TestContext.Current.CancellationToken);

		// Assert
		Assert.IsType<IPopupResult>(resultB.Value, exactMatch: false);
		Assert.Empty(shellB.Navigation.ModalStack);
		Assert.IsType<IPopupResult<string>>(await showPopupBTask, exactMatch: false);
		Assert.False(showPopupATask.IsCompleted);
		Assert.Single(shellA.Navigation.ModalStack);

		// Act
		var resultA = await ClosePopupAsync(closePopupMethod, shellA, "a", TestContext.Current.CancellationToken);

		// Assert
		Assert.IsType<IPopupResult>(resultA.Value, exactMatch: false);
		Assert.Empty(shellA.Navigation.ModalStack);
		Assert.IsType<IPopupResult<string>>(await showPopupATask, exactMatch: false);
	}

	static Shell CreateShell()
	{
		var shell = new Shell();
		shell.Items.Add(new MockPage(new MockPageViewModel()));
		return shell;
	}

	static Shell ActivateShellInFirstWindow()
	{
		Assert.NotNull(Application.Current);
		var shell = CreateShell();
		var window = Application.Current.Windows[0];
		window.Page = shell;
		((IWindow)window).Activated();
		return shell;
	}

	static (Window Window, TPage Page) OpenSecondWindow<TPage>(TPage page) where TPage : Page
	{
		Assert.NotNull(Application.Current);
		var window = new Window { Page = page };
		Application.Current.OpenWindow(window);
		Assert.Equal(2, Application.Current.Windows.Count);
		return (window, page);
	}

	static bool ReturnsExceptionInsteadOfThrowing(ClosePopupMethod closePopupMethod) => closePopupMethod.ToString().StartsWith("Try", StringComparison.Ordinal);

	async Task<PopupResultOrException> ClosePopupAsync(ClosePopupMethod closePopupMethod, Page page, string result, CancellationToken token)
	{
		var popupService = ServiceProvider.GetRequiredService<IPopupService>();

		try
		{
			switch (closePopupMethod)
			{
				case ClosePopupMethod.PageExtension:
					return await page.ClosePopupAsync(token);
				case ClosePopupMethod.PageExtensionWithResult:
					return await page.ClosePopupAsync(result, token);
				case ClosePopupMethod.NavigationExtension:
					return await page.Navigation.ClosePopupAsync(token);
				case ClosePopupMethod.NavigationExtensionWithResult:
					return await page.Navigation.ClosePopupAsync(result, token);
				case ClosePopupMethod.TryPageExtension:
					return await page.TryClosePopupAsync(token);
				case ClosePopupMethod.TryPageExtensionWithResult:
					return await page.TryClosePopupAsync(result, token);
				case ClosePopupMethod.TryNavigationExtension:
					return await page.Navigation.TryClosePopupAsync(token);
				case ClosePopupMethod.TryNavigationExtensionWithResult:
					return await page.Navigation.TryClosePopupAsync(result, token);
				case ClosePopupMethod.PopupServiceWithPage:
					return await popupService.ClosePopupAsync(page, token);
				case ClosePopupMethod.PopupServiceWithPageAndResult:
					return await popupService.ClosePopupAsync(page, result, token);
				case ClosePopupMethod.PopupServiceWithNavigation:
					return await popupService.ClosePopupAsync(page.Navigation, token);
				case ClosePopupMethod.PopupServiceWithNavigationAndResult:
					return await popupService.ClosePopupAsync(page.Navigation, result, token);
				case ClosePopupMethod.TryPopupServiceWithPage:
					return await popupService.TryClosePopupAsync(page, token);
				case ClosePopupMethod.TryPopupServiceWithPageAndResult:
					return await popupService.TryClosePopupAsync(page, result, token);
				case ClosePopupMethod.TryPopupServiceWithNavigation:
					return await popupService.TryClosePopupAsync(page.Navigation, token);
				case ClosePopupMethod.TryPopupServiceWithNavigationAndResult:
					return await popupService.TryClosePopupAsync(page.Navigation, result, token);
				default:
					throw new NotSupportedException($"{closePopupMethod} is not supported");
			}
		}
		catch (Exception exception) when (!ReturnsExceptionInsteadOfThrowing(closePopupMethod))
		{
			return exception;
		}
	}

	static Page GetWindowPage()
	{
		return Application.Current?.Windows[0].Page ?? throw new InvalidOperationException("Page cannot be null");
	}

	// Like the Sample's PopupOnDisappearingPage: a page pushed on the navigation stack of the root page (the root page itself for a ContentPage
	// root) pushes the modal page.
	static async Task<(Window Window, Page ModalPage)> PushModalPageThatShowsPopupWhenItDisappearsAsync(RootPageType rootPageType, Action<Page> showPopup)
	{
		var rootNavigation = SetRootPage(rootPageType);
		var window = Application.Current?.Windows[0] ?? throw new InvalidOperationException("Unable to locate Window");

		var page = window.Page ?? throw new InvalidOperationException("Unable to locate Page");
		if (rootPageType is not RootPageType.ContentPage)
		{
			page = new ContentPage();
			await rootNavigation.PushAsync(page);
		}

		var modalPage = new ShowPopupWhenDisappearingPage(showPopup);
		await page.Navigation.PushModalAsync(modalPage);
		Assert.Same(modalPage, Assert.Single(window.Navigation.ModalStack));

		return (window, modalPage);
	}

	static async Task WaitForPopupToOpenAsync(Task popupOpenedTask, CancellationToken token)
	{
		try
		{
			await popupOpenedTask.WaitAsync(TimeSpan.FromSeconds(2), token);
		}
		catch (TimeoutException)
		{
			Assert.Fail("The popup was not displayed after the modal page was popped.");
		}
	}

	static Task GetOpenedTask(Popup popup)
	{
		var popupOpenedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		popup.Opened += HandlePopupOpened;
		return popupOpenedTcs.Task;

		void HandlePopupOpened(object? sender, EventArgs e)
		{
			popup.Opened -= HandlePopupOpened;
			popupOpenedTcs.SetResult();
		}
	}

	sealed class ShowPopupWhenDisappearingPage(Action<Page> showPopup) : ContentPage
	{
		protected override void OnDisappearing()
		{
			base.OnDisappearing();
			showPopup(this);
		}
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