using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class PopupTryMethodsTests
{
	static Task NoNativeStateToAssertAsync(bool isPresented) => Task.CompletedTask;

	[Fact]
	public Task TryShowPopupAsyncReturnsTheResultOfTryClosePopupAsync()
	{
		SetupBuilder();
		var shell = PopupDismissedByNavigationScenarios.CreateShell(out var firstPage);

		return RunInWindowAsync(shell, firstPage, hostShell: true, _ => PopupTryMethodsScenarios.TryShowPopupAsyncReturnsTheResultOfTryClosePopupAsync(shell, NoNativeStateToAssertAsync));
	}

	[Fact]
	public Task TryShowPopupAsyncReturnsADismissedResultWhenShellSwitchesToAnotherShellElement()
	{
		SetupBuilder();
		var shell = PopupDismissedByNavigationScenarios.CreateShell(out var firstPage);

		return RunInWindowAsync(shell, firstPage, hostShell: true, _ => PopupTryMethodsScenarios.TryShowPopupAsyncReturnsADismissedResultWhenShellSwitchesToAnotherShellElement(shell, isOpenedRaised: false));
	}

	[Fact]
	public Task TryClosePopupAsyncReturnsPopupBlockedExceptionWhenAModalPageIsAboveThePopup()
	{
		SetupBuilder();
		var shell = PopupDismissedByNavigationScenarios.CreateShell(out var firstPage);

		return RunInWindowAsync(shell, firstPage, hostShell: true, _ => PopupTryMethodsScenarios.TryClosePopupAsyncReturnsPopupBlockedExceptionWhenAModalPageIsAboveThePopup(shell));
	}

	[Fact]
	public Task TryShowPopupAsyncReturnsOperationCanceledExceptionAndKeepsThePopupShownWhenTheTokenIsCanceledWhileItIsShown()
	{
		SetupBuilder();
		var shell = PopupDismissedByNavigationScenarios.CreateShell(out var firstPage);

		return RunInWindowAsync(shell, firstPage, hostShell: true, _ =>
			PopupTryMethodsScenarios.TryShowPopupAsyncReturnsOperationCanceledExceptionAndKeepsThePopupShownWhenTheTokenIsCanceledWhileItIsShown(shell, NoNativeStateToAssertAsync));
	}

	[Fact]
	public Task TryShowPopupAsyncOnAPageReturnsTheResultOfTryClosePopupAsync()
	{
		SetupBuilder();
		var page = new ContentPage();

		return RunInWindowAsync(page, page, hostShell: false, _ => PopupTryMethodsOnPageScenarios.TryShowPopupAsyncOnAPageReturnsTheResultOfTryClosePopupAsync(page));
	}

	[Fact]
	public Task TryShowPopupAsyncOnINavigationReturnsADismissedResultWhenOtherCodePopsTheModalPage()
	{
		SetupBuilder();
		var page = new ContentPage();

		return RunInWindowAsync(page, page, hostShell: false, _ => PopupTryMethodsOnPageScenarios.TryShowPopupAsyncOnINavigationReturnsADismissedResultWhenOtherCodePopsTheModalPage(page));
	}

	[Fact]
	public Task TryShowPopupAsyncThroughIPopupServiceReturnsTheResultOfTryClosePopupAsync()
	{
		SetupBuilder(PopupTryMethodsOnPageScenarios.RegisterPopup);
		var page = new ContentPage();

		return RunInWindowAsync(page, page, hostShell: false, _ =>
			PopupTryMethodsOnPageScenarios.TryShowPopupAsyncThroughIPopupServiceReturnsTheResultOfTryClosePopupAsync(page, MauiContext.Services.GetRequiredService<IPopupService>()));
	}
}