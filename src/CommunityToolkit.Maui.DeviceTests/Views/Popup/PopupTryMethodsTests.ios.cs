namespace CommunityToolkit.Maui.DeviceTests;

public partial class PopupTryMethodsTests
{
	[Fact]
	public Task TryShowPopupAsyncReturnsTheResultOfTryClosePopupAsync() =>
		RunInShellAsync((shell, host) => PopupTryMethodsScenarios.TryShowPopupAsyncReturnsTheResultOfTryClosePopupAsync(shell, isPresented => AssertPopupIsPresentedAsync(host, isPresented)));

	[Fact]
	public Task TryShowPopupAsyncReturnsADismissedResultWhenShellSwitchesToAnotherShellElement() =>
		RunInShellAsync((shell, _) => PopupTryMethodsScenarios.TryShowPopupAsyncReturnsADismissedResultWhenShellSwitchesToAnotherShellElement(shell));

	[Fact]
	public Task TryClosePopupAsyncReturnsPopupBlockedExceptionWhenAModalPageIsAboveThePopup() =>
		RunInShellAsync((shell, _) => PopupTryMethodsScenarios.TryClosePopupAsyncReturnsPopupBlockedExceptionWhenAModalPageIsAboveThePopup(shell));

	[Fact]
	public Task TryShowPopupAsyncReturnsOperationCanceledExceptionAndKeepsThePopupShownWhenTheTokenIsCanceledWhileItIsShown() =>
		RunInShellAsync((shell, host) =>
			PopupTryMethodsScenarios.TryShowPopupAsyncReturnsOperationCanceledExceptionAndKeepsThePopupShownWhenTheTokenIsCanceledWhileItIsShown(shell, isPresented => AssertPopupIsPresentedAsync(host, isPresented)));

	[Fact]
	public Task TryShowPopupAsyncOnAPageReturnsTheResultOfTryClosePopupAsync() =>
		RunInContentPageAsync(page => PopupTryMethodsOnPageScenarios.TryShowPopupAsyncOnAPageReturnsTheResultOfTryClosePopupAsync(page));

	[Fact]
	public Task TryShowPopupAsyncOnINavigationReturnsADismissedResultWhenOtherCodePopsTheModalPage() =>
		RunInContentPageAsync(page => PopupTryMethodsOnPageScenarios.TryShowPopupAsyncOnINavigationReturnsADismissedResultWhenOtherCodePopsTheModalPage(page));

	Task RunInContentPageAsync(Func<ContentPage, Task> scenario)
	{
		SetupBuilder();
		var page = new ContentPage();

		return RunInMauiWindowAsync(page, isPagePresentedModally: false, page, _ => scenario(page));
	}
}
