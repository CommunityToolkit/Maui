using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.DeviceTests;

// Scenarios for TryShowPopupAsync and TryClosePopupAsync, which return a PopupOutcome union instead of throwing. They use .NET MAUI's own navigation of a Shell
// that is the root page of a Window: the platform files of PopupDismissedByNavigationTests host that Window. Opened is not awaited, because a Window without a
// handler (Android) never raises it.
static class PopupTryMethodsScenarios
{
	const int timeout = 5000;

	public static async Task TryShowPopupAsyncReturnsTheResultOfTryClosePopupAsync(Shell shell)
	{
		const string expectedResult = "Closed";

		var showPopupTask = shell.TryShowPopupAsync<string>(CreatePopup(), CreateOptions(), token: CancellationToken.None);
		await WaitForPopupPages(shell, 1, "The popup was never shown.");

		var closeOutcome = await shell.TryClosePopupAsync(expectedResult).WaitAsync(TimeSpan.FromMilliseconds(timeout));
		var showOutcome = await showPopupTask.WaitAsync(TimeSpan.FromMilliseconds(timeout));

		var closeResult = Assert.IsType<IPopupResult<string>>(closeOutcome.Value, exactMatch: false);
		Assert.Equal(expectedResult, closeResult.Result);
		Assert.False(closeResult.WasDismissedByTappingOutsideOfPopup);

		var showResult = Assert.IsType<IPopupResult<string>>(showOutcome.Value, exactMatch: false);
		Assert.Equal(expectedResult, showResult.Result);
		Assert.False(showResult.WasDismissedByTappingOutsideOfPopup);
		Assert.Empty(shell.Navigation.ModalStack);
	}

	public static async Task TryShowPopupAsyncReturnsADismissedResultWhenShellSwitchesToAnotherShellElement(Shell shell)
	{
		var showPopupTask = shell.TryShowPopupAsync(CreatePopup(), CreateOptions(), token: CancellationToken.None);
		await WaitForPopupPages(shell, 1, "The popup was never shown.");

		await shell.GoToAsync("//second");
		var outcome = await showPopupTask.WaitAsync(TimeSpan.FromMilliseconds(timeout));

		var message = outcome switch
		{
			IPopupResult { WasDismissedByTappingOutsideOfPopup: true } => "Dismissed",
			IPopupResult => "Closed",
			Exception exception => $"Failed: {exception}"
		};
		Assert.Equal("Dismissed", message);
		Assert.Empty(shell.Navigation.ModalStack);
	}

	public static async Task TryClosePopupAsyncReturnsPopupBlockedExceptionWhenAModalPageIsAboveThePopup(Shell shell)
	{
		var showPopupTask = shell.TryShowPopupAsync(CreatePopup(), CreateOptions(), token: CancellationToken.None);
		await WaitForPopupPages(shell, 1, "The popup was never shown.");
		var blockingPage = new ContentPage { Title = "Blocking" };
		await shell.Navigation.PushModalAsync(blockingPage, false);

		var blockedOutcome = await shell.TryClosePopupAsync().WaitAsync(TimeSpan.FromMilliseconds(timeout));
		var genericBlockedOutcome = await shell.TryClosePopupAsync(1).WaitAsync(TimeSpan.FromMilliseconds(timeout));

		var exception = Assert.IsType<PopupBlockedException>(blockedOutcome.Value);
		Assert.IsType<PopupBlockedException>(genericBlockedOutcome.Value);
		Assert.Contains(nameof(ContentPage), exception.Message, StringComparison.Ordinal);
		Assert.False(showPopupTask.IsCompleted);
		Assert.Same(blockingPage, shell.Navigation.ModalStack[^1]);

		// Once the blocking page is gone, the same popup can be closed
		await shell.Navigation.PopModalAsync(false);
		var closeOutcome = await shell.TryClosePopupAsync().WaitAsync(TimeSpan.FromMilliseconds(timeout));
		var showOutcome = await showPopupTask.WaitAsync(TimeSpan.FromMilliseconds(timeout));

		Assert.False(Assert.IsType<IPopupResult>(closeOutcome.Value, exactMatch: false).WasDismissedByTappingOutsideOfPopup);
		Assert.False(Assert.IsType<IPopupResult>(showOutcome.Value, exactMatch: false).WasDismissedByTappingOutsideOfPopup);
		Assert.Empty(shell.Navigation.ModalStack);
	}

	public static async Task TryClosePopupAsyncReturnsPopupNotFoundExceptionWhenNoPopupIsShown(Shell shell)
	{
		var outcome = await shell.TryClosePopupAsync().WaitAsync(TimeSpan.FromMilliseconds(timeout));
		var genericOutcome = await shell.TryClosePopupAsync("Unused").WaitAsync(TimeSpan.FromMilliseconds(timeout));

		Assert.IsType<PopupNotFoundException>(outcome.Value);
		Assert.IsType<PopupNotFoundException>(genericOutcome.Value);
		Assert.Empty(shell.Navigation.ModalStack);
	}

	public static async Task TryShowPopupAsyncReturnsOperationCanceledExceptionAndShowsNoPopupWhenTheTokenIsCanceled(Shell shell)
	{
		using var cancellationTokenSource = new CancellationTokenSource();
		await cancellationTokenSource.CancelAsync();

		var outcome = await shell.TryShowPopupAsync(CreatePopup(), CreateOptions(), token: cancellationTokenSource.Token).WaitAsync(TimeSpan.FromMilliseconds(timeout));
		var genericOutcome = await shell.TryShowPopupAsync<int>(CreatePopup(), CreateOptions(), token: cancellationTokenSource.Token).WaitAsync(TimeSpan.FromMilliseconds(timeout));

		Assert.IsType<OperationCanceledException>(outcome.Value, exactMatch: false);
		Assert.IsType<OperationCanceledException>(genericOutcome.Value, exactMatch: false);
		Assert.Empty(shell.Navigation.ModalStack);
	}

	static Task WaitForPopupPages(Shell shell, int count, string message) =>
		AssertionExtensions.AssertEventually(() => shell.Navigation.ModalStack.OfType<PopupPage>().Count() == count, timeout: timeout, message: message);

	static Popup CreatePopup() => new()
	{
		Padding = 0,
		Content = new Label { Text = "Popup" }
	};

	static PopupOptions CreateOptions() => new() { Shape = null };
}