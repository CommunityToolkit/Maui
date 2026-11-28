using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;

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
	public Task TryShowPopupAsyncOnINavigationReturnsTheResultOfTryClosePopupAsync() =>
		RunInContentPageAsync(page => PopupTryMethodsOnPageScenarios.TryShowPopupAsyncOnINavigationReturnsTheResultOfTryClosePopupAsync(page));

	[Fact]
	public Task TryShowPopupAsyncOnAPageWithoutAResultReturnsTheResultOfTryClosePopupAsync() =>
		RunInContentPageAsync(page => PopupTryMethodsOnPageScenarios.TryShowPopupAsyncOnAPageWithoutAResultReturnsTheResultOfTryClosePopupAsync(page));

	[Fact]
	public Task TryShowPopupAsyncOnINavigationWithoutAResultReturnsTheResultOfTryClosePopupAsync() =>
		RunInContentPageAsync(page => PopupTryMethodsOnPageScenarios.TryShowPopupAsyncOnINavigationWithoutAResultReturnsTheResultOfTryClosePopupAsync(page));

	[Fact]
	public Task TryShowPopupAsyncWithoutAResultOnAPageShowsThePopupOnThePageBelowWhenCalledInOnDisappearingOfAModalPageBeingPopped() =>
		RunInContentPageAsync(page => PopupTryMethodsOnPageScenarios.TryShowPopupAsyncWithoutAResultOnAPageShowsThePopupOnThePageBelowWhenCalledInOnDisappearingOfAModalPageBeingPopped(page));

	[Fact]
	public Task TryShowPopupAsyncThroughIPopupServiceOnAPageReturnsTheResultOfTryClosePopupAsync() =>
		RunInContentPageAsync(page => PopupTryMethodsOnPageScenarios.TryShowPopupAsyncThroughIPopupServiceOnAPageReturnsTheResultOfTryClosePopupAsync(page, MauiContext.Services.GetRequiredService<IPopupService>()),
			PopupTryMethodsOnPageScenarios.RegisterPopup);

	[Fact]
	public Task TryShowPopupAsyncThroughIPopupServiceOnINavigationReturnsTheResultOfTryClosePopupAsync() =>
		RunInContentPageAsync(page => PopupTryMethodsOnPageScenarios.TryShowPopupAsyncThroughIPopupServiceOnINavigationReturnsTheResultOfTryClosePopupAsync(page, MauiContext.Services.GetRequiredService<IPopupService>()),
			PopupTryMethodsOnPageScenarios.RegisterPopup);

	[Fact]
	public Task TryShowPopupAsyncWithAResultThroughIPopupServiceOnINavigationReturnsTheResultOfTryClosePopupAsync() =>
		RunInContentPageAsync(page => PopupTryMethodsOnPageScenarios.TryShowPopupAsyncWithAResultThroughIPopupServiceOnINavigationReturnsTheResultOfTryClosePopupAsync(page, MauiContext.Services.GetRequiredService<IPopupService>()),
			PopupTryMethodsOnPageScenarios.RegisterPopup);

	[Fact]
	public Task TryShowPopupAsyncThroughIPopupServiceOnAShellReturnsTheResultOfTryClosePopupAsync() =>
		RunInShellAsync((shell, _) => PopupTryMethodsScenarios.TryShowPopupAsyncThroughIPopupServiceOnAShellReturnsTheResultOfTryClosePopupAsync(shell, MauiContext.Services.GetRequiredService<IPopupService>()), PopupTryMethodsOnPageScenarios.RegisterPopup);

	[Fact]
	public Task TryShowPopupAsyncWithAResultThroughIPopupServiceOnAShellReturnsTheResultOfTryClosePopupAsync() =>
		RunInShellAsync((shell, _) => PopupTryMethodsScenarios.TryShowPopupAsyncWithAResultThroughIPopupServiceOnAShellReturnsTheResultOfTryClosePopupAsync(shell, MauiContext.Services.GetRequiredService<IPopupService>()), PopupTryMethodsOnPageScenarios.RegisterPopup);

	public enum PopupStateWhenTheWindowIsDestroyed
	{
		ClosedWithTryClosePopupAsync,
		PoppedWithPopModalAsync,
		StillOpen
	}

	// .NET MAUI disconnects the handlers of a popped modal page itself on iOS and Mac Catalyst, so only the StillOpen row proves the product disconnects the PopupPage and the Popup at Destroying.
	// The window of the host is built on the MauiContext of the test, so Destroying does not dispose anything here: only the handlers of the PopupPage and the Popup show it.
	[Theory]
	[InlineData(PopupStateWhenTheWindowIsDestroyed.ClosedWithTryClosePopupAsync)]
	[InlineData(PopupStateWhenTheWindowIsDestroyed.PoppedWithPopModalAsync)]
	[InlineData(PopupStateWhenTheWindowIsDestroyed.StillOpen)]
	public async Task DestroyingTheWindowDisconnectsThePopupAndTheSamePopupCanBeShownInAnotherWindow(PopupStateWhenTheWindowIsDestroyed popupState)
	{
		SetupBuilder();
		var page = new ContentPage();
		var pageOfOtherWindow = new ContentPage();
		var popup = new Popup { Padding = 0, Content = new Label { Text = "Popup" } };
		Border? popupBorder = null;

		await RunInMauiWindowAsync(page, isPagePresentedModally: false, page, async _ =>
		{
			// Popup.Opened is raised inside .NET MAUI's NavigatedTo, and the scenario must not go on inside that callback
			var popupOpenedTaskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			popup.Opened += (_, _) => popupOpenedTaskCompletionSource.TrySetResult();
			var showPopupTask = page.TryShowPopupAsync<string>(popup, new PopupOptions(), token: CancellationToken.None);
			await AssertionExtensions.AssertEventually(() => page.Navigation.ModalStack.OfType<PopupPage>().Any(), timeout: 5000, message: "The popup was never shown.");
			// The PopupPage is registered for Window.Destroying only once the popup is opened, which iOS reports after the modal presentation finished.
			await popupOpenedTaskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(10));
			popupBorder = page.Navigation.ModalStack.OfType<PopupPage>().Single().Content.PopupBorder;

			switch (popupState)
			{
				case PopupStateWhenTheWindowIsDestroyed.ClosedWithTryClosePopupAsync:
					await page.TryClosePopupAsync("Closed").WaitAsync(TimeSpan.FromSeconds(5));
					await showPopupTask.WaitAsync(TimeSpan.FromSeconds(5));
					break;
				case PopupStateWhenTheWindowIsDestroyed.PoppedWithPopModalAsync:
					await page.Navigation.PopModalAsync(false);
					await showPopupTask.WaitAsync(TimeSpan.FromSeconds(5));
					break;
				case PopupStateWhenTheWindowIsDestroyed.StillOpen:
					break;
				default:
					throw new NotSupportedException($"{popupState} is not a supported state.");
			}

			if (popupState is PopupStateWhenTheWindowIsDestroyed.StillOpen)
			{
				Assert.NotNull(popupBorder.Handler);
				Assert.NotNull(popup.Handler);
			}
		});

		Assert.NotNull(popupBorder);
		Assert.Null(popupBorder.Handler);
		Assert.Null(popup.Handler);

		await RunInMauiWindowAsync(pageOfOtherWindow, isPagePresentedModally: false, pageOfOtherWindow, _ => PopupTryMethodsOnPageScenarios.ShowAndCloseAPopupOnAPage(pageOfOtherWindow, popup));
	}

	// iOS and Mac Catalyst show every window of a test in the one scene of the runner, but each .NET MAUI Window keeps its own modal stack, which is what the lookup of the popup uses
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