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
	public Task TryShowPopupAsyncOnINavigationReturnsADismissedResultWhenOtherCodePopsTheModalPage() =>
		RunInContentPageAsync(page => PopupTryMethodsOnPageScenarios.TryShowPopupAsyncOnINavigationReturnsADismissedResultWhenOtherCodePopsTheModalPage(page));

	[Fact]
	public Task TryShowPopupAsyncThroughIPopupServiceReturnsTheResultOfTryClosePopupAsync() =>
		RunInContentPageAsync(page => PopupTryMethodsOnPageScenarios.TryShowPopupAsyncThroughIPopupServiceReturnsTheResultOfTryClosePopupAsync(page, MauiContext.Services.GetRequiredService<IPopupService>()),
			PopupTryMethodsOnPageScenarios.RegisterPopup);

	[Fact]
	public Task TryClosePopupAsyncClosesOnlyThePopupOfItsOwnWindow() =>
		RunInTwoWindowsAsync(PopupTryMethodsOnPageScenarios.TryClosePopupAsyncClosesOnlyThePopupOfItsOwnWindow);
}