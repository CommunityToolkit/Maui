using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class PopupTryMethodsTests
{
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
	public Task TryShowPopupAsyncOnAPageShowsThePopupOnThePageBelowWhenCalledInOnDisappearingOfAModalPageBeingPopped()
	{
		SetupBuilder();
		var page = new ContentPage();

		return RunInWindowAsync(page, page, hostShell: false, _ => PopupTryMethodsOnPageScenarios.TryShowPopupAsyncOnAPageShowsThePopupOnThePageBelowWhenCalledInOnDisappearingOfAModalPageBeingPopped(page));
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

	[Fact]
	public Task TryShowPopupAsyncOnINavigationReturnsTheResultOfTryClosePopupAsync()
	{
		SetupBuilder(PopupTryMethodsOnPageScenarios.RegisterPopup);
		var page = new ContentPage();

		return RunInWindowAsync(page, page, hostShell: false, _ => PopupTryMethodsOnPageScenarios.TryShowPopupAsyncOnINavigationReturnsTheResultOfTryClosePopupAsync(page));
	}

	[Fact]
	public Task TryShowPopupAsyncOnAPageWithoutAResultReturnsTheResultOfTryClosePopupAsync()
	{
		SetupBuilder(PopupTryMethodsOnPageScenarios.RegisterPopup);
		var page = new ContentPage();

		return RunInWindowAsync(page, page, hostShell: false, _ => PopupTryMethodsOnPageScenarios.TryShowPopupAsyncOnAPageWithoutAResultReturnsTheResultOfTryClosePopupAsync(page));
	}

	[Fact]
	public Task TryShowPopupAsyncOnINavigationWithoutAResultReturnsTheResultOfTryClosePopupAsync()
	{
		SetupBuilder(PopupTryMethodsOnPageScenarios.RegisterPopup);
		var page = new ContentPage();

		return RunInWindowAsync(page, page, hostShell: false, _ => PopupTryMethodsOnPageScenarios.TryShowPopupAsyncOnINavigationWithoutAResultReturnsTheResultOfTryClosePopupAsync(page));
	}

	[Fact]
	public Task TryShowPopupAsyncWithoutAResultOnAPageShowsThePopupOnThePageBelowWhenCalledInOnDisappearingOfAModalPageBeingPopped()
	{
		SetupBuilder(PopupTryMethodsOnPageScenarios.RegisterPopup);
		var page = new ContentPage();

		return RunInWindowAsync(page, page, hostShell: false, _ => PopupTryMethodsOnPageScenarios.TryShowPopupAsyncWithoutAResultOnAPageShowsThePopupOnThePageBelowWhenCalledInOnDisappearingOfAModalPageBeingPopped(page));
	}

	[Fact]
	public Task TryShowPopupAsyncThroughIPopupServiceOnAPageReturnsTheResultOfTryClosePopupAsync()
	{
		SetupBuilder(PopupTryMethodsOnPageScenarios.RegisterPopup);
		var page = new ContentPage();

		return RunInWindowAsync(page, page, hostShell: false, _ => PopupTryMethodsOnPageScenarios.TryShowPopupAsyncThroughIPopupServiceOnAPageReturnsTheResultOfTryClosePopupAsync(page, MauiContext.Services.GetRequiredService<IPopupService>()));
	}

	[Fact]
	public Task TryShowPopupAsyncThroughIPopupServiceOnINavigationReturnsTheResultOfTryClosePopupAsync()
	{
		SetupBuilder(PopupTryMethodsOnPageScenarios.RegisterPopup);
		var page = new ContentPage();

		return RunInWindowAsync(page, page, hostShell: false, _ => PopupTryMethodsOnPageScenarios.TryShowPopupAsyncThroughIPopupServiceOnINavigationReturnsTheResultOfTryClosePopupAsync(page, MauiContext.Services.GetRequiredService<IPopupService>()));
	}

	[Fact]
	public Task TryShowPopupAsyncWithAResultThroughIPopupServiceOnINavigationReturnsTheResultOfTryClosePopupAsync()
	{
		SetupBuilder(PopupTryMethodsOnPageScenarios.RegisterPopup);
		var page = new ContentPage();

		return RunInWindowAsync(page, page, hostShell: false, _ => PopupTryMethodsOnPageScenarios.TryShowPopupAsyncWithAResultThroughIPopupServiceOnINavigationReturnsTheResultOfTryClosePopupAsync(page, MauiContext.Services.GetRequiredService<IPopupService>()));
	}

	[Fact]
	public Task TryShowPopupAsyncThroughIPopupServiceOnAShellReturnsTheResultOfTryClosePopupAsync()
	{
		SetupBuilder(PopupTryMethodsOnPageScenarios.RegisterPopup);
		var shell = PopupDismissedByNavigationScenarios.CreateShell(out var firstPage);

		return RunInWindowAsync(shell, firstPage, hostShell: true, _ => PopupTryMethodsScenarios.TryShowPopupAsyncThroughIPopupServiceOnAShellReturnsTheResultOfTryClosePopupAsync(shell, MauiContext.Services.GetRequiredService<IPopupService>()));
	}

	[Fact]
	public Task TryShowPopupAsyncWithAResultThroughIPopupServiceOnAShellReturnsTheResultOfTryClosePopupAsync()
	{
		SetupBuilder(PopupTryMethodsOnPageScenarios.RegisterPopup);
		var shell = PopupDismissedByNavigationScenarios.CreateShell(out var firstPage);

		return RunInWindowAsync(shell, firstPage, hostShell: true, _ => PopupTryMethodsScenarios.TryShowPopupAsyncWithAResultThroughIPopupServiceOnAShellReturnsTheResultOfTryClosePopupAsync(shell, MauiContext.Services.GetRequiredService<IPopupService>()));
	}
}