using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.DeviceTests;

// A ContentPage is the root page of the Window: PopupTryMethodsTests hosts that Window. The Shell scenarios never reach the Page and INavigation overloads of TryShowPopupAsync.
static class PopupTryMethodsOnPageScenarios
{
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

	static Task WaitForPopupPages(ContentPage page, int count) =>
		AssertionExtensions.AssertEventually(() => page.Navigation.ModalStack.OfType<PopupPage>().Count() == count, timeout: timeout, message: "The popup was never shown.");

	static Popup CreatePopup() => new()
	{
		Padding = 0,
		Content = new Label { Text = "Popup" }
	};

	static PopupOptions CreateOptions() => new() { Shape = null };
}
