using CommunityToolkit.Maui.Core;

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

	[Fact]
	public Task TryShowPopupAsyncThroughIPopupServiceReturnsTheResultOfTryClosePopupAsync() =>
		RunInContentPageAsync(page => PopupTryMethodsOnPageScenarios.TryShowPopupAsyncThroughIPopupServiceReturnsTheResultOfTryClosePopupAsync(page, MauiContext.Services.GetRequiredService<IPopupService>()),
			PopupTryMethodsOnPageScenarios.RegisterPopup);

	// Mac Catalyst shows every window of a test in the one scene of the runner, but each .NET MAUI Window keeps its own modal stack, which is what the lookup of the popup uses
	[Fact]
	public Task TryClosePopupAsyncClosesOnlyThePopupOfItsOwnWindow()
	{
		SetupBuilder();
		var pageOfFirstWindow = new ContentPage();
		var pageOfSecondWindow = new ContentPage();

		return RunInMauiWindowAsync(pageOfFirstWindow, isPagePresentedModally: false, pageOfFirstWindow, _ =>
			RunInMauiWindowAsync(pageOfSecondWindow, isPagePresentedModally: false, pageOfSecondWindow, _ =>
				PopupTryMethodsOnPageScenarios.TryClosePopupAsyncClosesOnlyThePopupOfItsOwnWindow(pageOfFirstWindow, pageOfSecondWindow)));
	}

	Task RunInContentPageAsync(Func<ContentPage, Task> scenario, Action<MauiAppBuilder>? configureBuilder = null)
	{
		SetupBuilder(configureBuilder);
		var page = new ContentPage();

		return RunInMauiWindowAsync(page, isPagePresentedModally: false, page, _ => scenario(page));
	}
}