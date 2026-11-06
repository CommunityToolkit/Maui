using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Xunit;
using Application = Microsoft.Maui.Controls.Application;
using NavigationPage = Microsoft.Maui.Controls.NavigationPage;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class PopupDismissedByNavigationTests : BaseViewTest
{
	[Theory(Timeout = (int)TestDuration.Short)]
	[InlineData(RootPageType.Shell)]
	[InlineData(RootPageType.ContentPage)]
	[InlineData(RootPageType.NavigationPage)]
	public async Task ShowPopupAsync_ShouldCompleteAsDismissedAndRaiseClosedOnce_WhenModalIsPoppedWithoutCloseAsync(RootPageType rootPageType)
	{
		var navigation = SetRootPage(rootPageType);
		var popup = new Popup();
		var closedCount = 0;
		popup.Closed += (_, _) => closedCount++;

		var showPopupTask = navigation.ShowPopupAsync(popup, PopupOptions.Empty, TestContext.Current.CancellationToken);
		var popupPage = Assert.IsType<PopupPage>(Assert.Single(navigation.ModalStack));

		await navigation.PopModalAsync(false);
		var result = await showPopupTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

		Assert.False(result.WasDismissedByTappingOutsideOfPopup);
		Assert.Equal(1, closedCount);

		// CloseAsync of a PopupPage that navigation already removed must not report the result a second time
		await Assert.ThrowsAnyAsync<InvalidPopupOperationException>(() => popupPage.CloseAsync(new PopupResult(false), TestContext.Current.CancellationToken));
		Assert.Equal(1, closedCount);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_ShouldCompleteAsDismissed_WhenShellSwitchesToAnotherShellElement()
	{
		var shell = new Shell();
		shell.Items.Add(new ShellContent { Route = "first", Content = new ContentPage() });
		shell.Items.Add(new ShellContent { Route = "second", Content = new ContentPage() });
		Application.Current!.Windows[0].Page = shell;
		var popup = new Popup();
		var closedCount = 0;
		popup.Closed += (_, _) => closedCount++;

		var showPopupTask = shell.ShowPopupAsync(popup, PopupOptions.Empty, token: TestContext.Current.CancellationToken);
		for (var i = 0; i < 100 && shell.Navigation.ModalStack.Count == 0; i++)
		{
			await Task.Delay(10, TestContext.Current.CancellationToken);
		}

		Assert.Single(shell.Navigation.ModalStack);

		await shell.GoToAsync("//second");
		var result = await showPopupTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

		Assert.False(result.WasDismissedByTappingOutsideOfPopup);
		Assert.Empty(shell.Navigation.ModalStack);
		Assert.Equal(1, closedCount);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task ShowPopupAsync_ShouldCompleteBothPopupsAsDismissed_WhenTheModalStackIsPoppedWithoutCloseAsync()
	{
		var navigation = SetRootPage(RootPageType.ContentPage);
		var closedCounts = new int[2];
		var lowerPopup = new Popup();
		var upperPopup = new Popup();
		lowerPopup.Closed += (_, _) => closedCounts[0]++;
		upperPopup.Closed += (_, _) => closedCounts[1]++;

		var lowerTask = navigation.ShowPopupAsync(lowerPopup, PopupOptions.Empty, TestContext.Current.CancellationToken);
		var upperTask = navigation.ShowPopupAsync(upperPopup, PopupOptions.Empty, TestContext.Current.CancellationToken);
		Assert.Equal(2, navigation.ModalStack.Count);

		await navigation.PopModalAsync(false);
		await navigation.PopModalAsync(false);
		await Task.WhenAll(lowerTask, upperTask).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

		Assert.Equal([1, 1], closedCounts);
	}

	[Fact]
	public async Task ShowPopupAsync_ShouldCompleteAsDismissed_WhenModalNavigationPageContainingThePopupIsPopped()
	{
		var navigation = SetRootPage(RootPageType.ContentPage);
		var popupPage = new PopupPage(new Popup(), PopupOptions.Empty);
		var closedCount = 0;
		popupPage.PopupClosed += (_, _) => closedCount++;
		var modalNavigationPage = new NavigationPage(popupPage);

		await navigation.PushModalAsync(modalNavigationPage, false);
		await navigation.PopModalAsync(false);

		Assert.Equal(1, closedCount);
	}

	[Fact]
	public async Task PopupClosed_ShouldNotBeRaisedByTheModalPopOfAnotherPage()
	{
		var navigation = SetRootPage(RootPageType.ContentPage);
		var popupPage = new PopupPage(new Popup(), PopupOptions.Empty);
		var closedCount = 0;
		popupPage.PopupClosed += (_, _) => closedCount++;

		await navigation.PushModalAsync(popupPage, false);
		await navigation.PushModalAsync(new ContentPage(), false);
		await navigation.PopModalAsync(false);

		Assert.Equal(0, closedCount);
		Assert.Same(popupPage, Assert.Single(navigation.ModalStack));
	}

	// The Android QA walk found two popups, shown with IPopupService.ShowPopupAsync<T>(Shell.Current) on top of a modal NavigationPage, that could not be closed.
	// With a Shell as the root page, .NET MAUI pushes a page that is shown modally on top of a modal NavigationPage onto the navigation stack of that NavigationPage.
	[Theory(Timeout = (int)TestDuration.Long)]
	[InlineData(RootPageType.Shell)]
	[InlineData(RootPageType.ContentPage)]
	public async Task CloseAsync_ShouldCloseTwoPopupsOneAfterTheOther_WhenTheyAreShownOnTopOfAModalNavigationPage(RootPageType rootPageType)
	{
		var shown = await ShowTwoPopupsOnTopOfAModalNavigationPageAsync(rootPageType);
		var (navigation, modalNavigationPage, lowerPopup, upperPopup, lowerTask, upperTask) = shown;

		await upperPopup.CloseAsync(TestContext.Current.CancellationToken);
		await upperTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
		Assert.Equal(1, CountPopupPages(navigation, modalNavigationPage));

		await lowerPopup.CloseAsync(TestContext.Current.CancellationToken);
		await lowerTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

		Assert.Equal(0, CountPopupPages(navigation, modalNavigationPage));
		Assert.Same(modalNavigationPage, Assert.Single(navigation.ModalStack));
	}

	[Theory(Timeout = (int)TestDuration.Long)]
	[InlineData(RootPageType.Shell)]
	[InlineData(RootPageType.ContentPage)]
	public async Task ClosePopupAsync_ShouldCloseTwoPopupsOneAfterTheOther_WhenTheyAreShownOnTopOfAModalNavigationPage(RootPageType rootPageType)
	{
		var (navigation, modalNavigationPage, _, _, lowerTask, upperTask) = await ShowTwoPopupsOnTopOfAModalNavigationPageAsync(rootPageType);

		await navigation.ClosePopupAsync(TestContext.Current.CancellationToken);
		await upperTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
		await navigation.ClosePopupAsync(TestContext.Current.CancellationToken);
		await lowerTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

		Assert.Equal(0, CountPopupPages(navigation, modalNavigationPage));
		Assert.Same(modalNavigationPage, Assert.Single(navigation.ModalStack));
	}

	static async Task<(INavigation Navigation, NavigationPage ModalNavigationPage, Popup LowerPopup, Popup UpperPopup, Task<IPopupResult> LowerTask, Task<IPopupResult> UpperTask)> ShowTwoPopupsOnTopOfAModalNavigationPageAsync(RootPageType rootPageType)
	{
		var navigation = SetRootPage(rootPageType);
		var modalNavigationPage = new NavigationPage(new ContentPage());
		await navigation.PushModalAsync(modalNavigationPage, false);

		var lowerPopup = new Popup();
		var upperPopup = new Popup();
		var lowerTask = navigation.ShowPopupAsync(lowerPopup, PopupOptions.Empty, TestContext.Current.CancellationToken);
		var upperTask = navigation.ShowPopupAsync(upperPopup, PopupOptions.Empty, TestContext.Current.CancellationToken);

		for (var i = 0; i < 200 && CountPopupPages(navigation, modalNavigationPage) < 2; i++)
		{
			await Task.Delay(10, TestContext.Current.CancellationToken);
		}

		Assert.Equal(2, CountPopupPages(navigation, modalNavigationPage));
		return (navigation, modalNavigationPage, lowerPopup, upperPopup, lowerTask, upperTask);
	}

	static int CountPopupPages(INavigation navigation, NavigationPage modalNavigationPage) =>
		navigation.ModalStack.OfType<PopupPage>().Count() + modalNavigationPage.Navigation.NavigationStack.OfType<PopupPage>().Count();
}
