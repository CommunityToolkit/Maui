using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.DeviceTests;

// A ContentPage is the root page of the Window: PopupTryMethodsTests hosts that Window. The Shell scenarios never reach the Page and INavigation overloads of TryShowPopupAsync.
static class PopupTryMethodsOnPageScenarios
{
	// IPopupService creates the popup from the service provider, so its type must be registered: PopupTryMethodsTests does it with RegisterPopup.
	public sealed class RegisteredPopup : Popup
	{
		public RegisteredPopup()
		{
			Padding = 0;
			Content = new Label { Text = "Popup" };
		}
	}

	public static void RegisterPopup(MauiAppBuilder builder) => builder.Services.AddTransientPopup<RegisteredPopup>();

	const int timeout = 5000;

	public static async Task TryShowPopupAsyncOnAPageReturnsTheResultOfTryClosePopupAsync(ContentPage page)
	{
		const string expectedResult = "Closed";

		var showPopupTask = page.TryShowPopupAsync<string>(CreatePopup(), CreateOptions(), token: CancellationToken.None);
		await WaitForPopupPages(page, 1);

		var closeResultOrException = await page.TryClosePopupAsync(expectedResult).WaitAsync(TimeSpan.FromMilliseconds(timeout));
		var showResultOrException = await showPopupTask.WaitAsync(TimeSpan.FromMilliseconds(timeout));

		Assert.Equal(expectedResult, Assert.IsType<IPopupResult<string>>(closeResultOrException.Value, exactMatch: false).Result);
		var showResult = Assert.IsType<IPopupResult<string>>(showResultOrException.Value, exactMatch: false);
		Assert.Equal(expectedResult, showResult.Result);
		Assert.False(showResult.WasDismissedByTappingOutsideOfPopup);
		Assert.Empty(page.Navigation.ModalStack);
	}

	// A modal page that is being popped has already left the modal stack when OnDisappearing runs, so only a wait for the pop shows the popup on the page below it.
	public static async Task TryShowPopupAsyncOnAPageShowsThePopupOnThePageBelowWhenCalledInOnDisappearingOfAModalPageBeingPopped(ContentPage page)
	{
		const string expectedResult = "Closed";
		Task<PopupResultOrException<string>>? showPopupTask = null;
		var modalPage = new ContentPage();
		modalPage.Disappearing += (_, _) => showPopupTask = modalPage.TryShowPopupAsync<string>(CreatePopup(), CreateOptions(), token: CancellationToken.None);

		await page.Navigation.PushModalAsync(modalPage, false);
		await page.Navigation.PopModalAsync(false);
		await WaitForPopupPages(page, 1);

		Assert.NotNull(showPopupTask);
		Assert.False(showPopupTask.IsCompleted);

		var closeResultOrException = await page.TryClosePopupAsync(expectedResult).WaitAsync(TimeSpan.FromMilliseconds(timeout));
		var showResultOrException = await showPopupTask.WaitAsync(TimeSpan.FromMilliseconds(timeout));

		Assert.Equal(expectedResult, Assert.IsType<IPopupResult<string>>(closeResultOrException.Value, exactMatch: false).Result);
		var showResult = Assert.IsType<IPopupResult<string>>(showResultOrException.Value, exactMatch: false);
		Assert.Equal(expectedResult, showResult.Result);
		Assert.False(showResult.WasDismissedByTappingOutsideOfPopup);
		Assert.Empty(page.Navigation.ModalStack);
	}

	public static async Task TryShowPopupAsyncOnINavigationReturnsADismissedResultWhenOtherCodePopsTheModalPage(ContentPage page)
	{
		var popup = CreatePopup();
		var hasOpened = false;
		popup.Opened += (_, _) => hasOpened = true;

		var showPopupTask = page.Navigation.TryShowPopupAsync(popup, CreateOptions(), token: CancellationToken.None);
		await AssertionExtensions.AssertEventually(() => page.Navigation.ModalStack.OfType<PopupPage>().Count() == 1 && hasOpened, timeout: timeout, message: "The popup was never shown.");

		await page.Navigation.PopModalAsync(false);
		var resultOrException = await showPopupTask.WaitAsync(TimeSpan.FromMilliseconds(timeout));

		Assert.True(Assert.IsType<IPopupResult>(resultOrException.Value, exactMatch: false).WasDismissedByTappingOutsideOfPopup);
		Assert.Empty(page.Navigation.ModalStack);
	}

	public static async Task TryShowPopupAsyncThroughIPopupServiceReturnsTheResultOfTryClosePopupAsync(ContentPage page, IPopupService popupService)
	{
		const string expectedResult = "Closed";

		var showPopupTask = popupService.TryShowPopupAsync<RegisteredPopup, string>(page, CreateOptions(), CancellationToken.None);
		await WaitForPopupPages(page, 1);

		var closeResultOrException = await popupService.TryClosePopupAsync(page, expectedResult).WaitAsync(TimeSpan.FromMilliseconds(timeout));
		var showResultOrException = await showPopupTask.WaitAsync(TimeSpan.FromMilliseconds(timeout));

		Assert.Equal(expectedResult, Assert.IsType<IPopupResult<string>>(closeResultOrException.Value, exactMatch: false).Result);
		var showResult = Assert.IsType<IPopupResult<string>>(showResultOrException.Value, exactMatch: false);
		Assert.Equal(expectedResult, showResult.Result);
		Assert.False(showResult.WasDismissedByTappingOutsideOfPopup);
		Assert.Empty(page.Navigation.ModalStack);
	}

	// Closing through the navigation of one window must not close the popup that another window shows: the popup is looked up only in the navigation that was passed in.
	public static async Task TryClosePopupAsyncClosesOnlyThePopupOfItsOwnWindow(ContentPage pageOfFirstWindow, ContentPage pageOfSecondWindow)
	{
		const string expectedResult = "Second";

		var showFirstPopupTask = pageOfFirstWindow.TryShowPopupAsync<string>(CreatePopup(), CreateOptions(), token: CancellationToken.None);
		await WaitForPopupPages(pageOfFirstWindow, 1);
		var showSecondPopupTask = pageOfSecondWindow.TryShowPopupAsync<string>(CreatePopup(), CreateOptions(), token: CancellationToken.None);
		await WaitForPopupPages(pageOfSecondWindow, 1);

		var closeResultOrException = await pageOfSecondWindow.TryClosePopupAsync(expectedResult).WaitAsync(TimeSpan.FromMilliseconds(timeout));
		var showSecondResultOrException = await showSecondPopupTask.WaitAsync(TimeSpan.FromMilliseconds(timeout));

		Assert.Equal(expectedResult, Assert.IsType<IPopupResult<string>>(closeResultOrException.Value, exactMatch: false).Result);
		Assert.Equal(expectedResult, Assert.IsType<IPopupResult<string>>(showSecondResultOrException.Value, exactMatch: false).Result);
		Assert.Empty(pageOfSecondWindow.Navigation.ModalStack);
		Assert.Single(pageOfFirstWindow.Navigation.ModalStack.OfType<PopupPage>());
		Assert.False(showFirstPopupTask.IsCompleted);

		var closeFirstResultOrException = await pageOfFirstWindow.TryClosePopupAsync("First").WaitAsync(TimeSpan.FromMilliseconds(timeout));
		await showFirstPopupTask.WaitAsync(TimeSpan.FromMilliseconds(timeout));

		Assert.Equal("First", Assert.IsType<IPopupResult<string>>(closeFirstResultOrException.Value, exactMatch: false).Result);
		Assert.Empty(pageOfFirstWindow.Navigation.ModalStack);
	}

	static Task WaitForPopupPages(ContentPage page, int count) =>
		AssertionExtensions.AssertEventually(() => page.Navigation.ModalStack.OfType<PopupPage>().Count() == count, timeout: timeout, message: "The popup was never shown.");

	static Popup CreatePopup() => new()
	{
		Padding = 0,
		Content = new Label { Text = "Popup" }
	};

	static PopupOptions CreateOptions() => new() { Shape = null };
}