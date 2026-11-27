using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class PopupTryMethodsTests
{
	[Fact]
	public Task TryShowPopupAsyncReturnsTheResultOfTryClosePopupAsync() =>
		RunInShellAsync(shell => PopupTryMethodsScenarios.TryShowPopupAsyncReturnsTheResultOfTryClosePopupAsync(shell, NoNativeStateToAssertAsync));

	[Fact]
	public Task TryShowPopupAsyncReturnsADismissedResultWhenShellSwitchesToAnotherShellElement() =>
		RunInShellAsync(shell => PopupTryMethodsScenarios.TryShowPopupAsyncReturnsADismissedResultWhenShellSwitchesToAnotherShellElement(shell));

	[Fact]
	public Task TryClosePopupAsyncReturnsPopupBlockedExceptionWhenAModalPageIsAboveThePopup() =>
		RunInShellAsync(shell => PopupTryMethodsScenarios.TryClosePopupAsyncReturnsPopupBlockedExceptionWhenAModalPageIsAboveThePopup(shell));

	[Fact]
	public Task TryShowPopupAsyncReturnsOperationCanceledExceptionAndKeepsThePopupShownWhenTheTokenIsCanceledWhileItIsShown() =>
		RunInShellAsync(shell =>
			PopupTryMethodsScenarios.TryShowPopupAsyncReturnsOperationCanceledExceptionAndKeepsThePopupShownWhenTheTokenIsCanceledWhileItIsShown(shell, NoNativeStateToAssertAsync));

	[Fact]
	public Task TryShowPopupAsyncOnAPageReturnsTheResultOfTryClosePopupAsync() =>
		RunInContentPageAsync(page => PopupTryMethodsOnPageScenarios.TryShowPopupAsyncOnAPageReturnsTheResultOfTryClosePopupAsync(page));

	[Fact]
	public Task TryShowPopupAsyncOnAPageShowsThePopupOnThePageBelowWhenCalledInOnDisappearingOfAModalPageBeingPopped() =>
		RunInContentPageAsync(page => PopupTryMethodsOnPageScenarios.TryShowPopupAsyncOnAPageShowsThePopupOnThePageBelowWhenCalledInOnDisappearingOfAModalPageBeingPopped(page));

	[Fact]
	public Task TryShowPopupAsyncOnINavigationReturnsADismissedResultWhenOtherCodePopsTheModalPage() =>
		RunInContentPageAsync(page => PopupTryMethodsOnPageScenarios.TryShowPopupAsyncOnINavigationReturnsADismissedResultWhenOtherCodePopsTheModalPage(page));

	[Fact]
	public Task TryShowPopupAsyncThroughIPopupServiceReturnsTheResultOfTryClosePopupAsync() =>
		RunInContentPageAsync(page => PopupTryMethodsOnPageScenarios.TryShowPopupAsyncThroughIPopupServiceReturnsTheResultOfTryClosePopupAsync(page, MauiContext.Services.GetRequiredService<IPopupService>()),
			PopupTryMethodsOnPageScenarios.RegisterPopup);

	[Fact]
	public Task TryClosePopupAsyncClosesOnlyThePopupOfItsOwnWindow() =>
		RunInTwoWindowsAsync(PopupTryMethodsOnPageScenarios.TryClosePopupAsyncClosesOnlyThePopupOfItsOwnWindow);

	// Every popup shows the same default Shadow; the Border of a closed popup must not keep mapping it against the service scope of a window that was closed
	[Fact]
	public async Task ShowingAPopupInAnotherWindowSucceedsAfterTheWindowThatShowedAPopupWasClosed()
	{
		SetupBuilder();
		var pageOfClosedWindow = new ContentPage();
		var pageOfOtherWindow = new ContentPage();

		await RunInWindowAsync(pageOfClosedWindow, pageOfClosedWindow, _ => PopupTryMethodsOnPageScenarios.ShowAndCloseAPopupOnAPage(pageOfClosedWindow));
		await RunInWindowAsync(pageOfOtherWindow, pageOfOtherWindow, _ => PopupTryMethodsOnPageScenarios.ShowAndCloseAPopupOnAPage(pageOfOtherWindow));
	}
}