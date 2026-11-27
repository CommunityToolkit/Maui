using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Window = Microsoft.Maui.Controls.Window;

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

	public enum PopupRemoval
	{
		CloseAsync,
		OtherCodePopsTheModalPage,
		ShellPopsThePopupFromAModalNavigationPage,
		PageOfTheWindowIsReplaced,
		ShellSwitchesToAnotherShellElement
	}

	// A Shell window is one of the places where .NET MAUI leaves the Border of a removed PopupPage connected; the same popup is then shown in a window that outlives the first one.
	[Theory]
	[InlineData(PopupRemoval.CloseAsync)]
	[InlineData(PopupRemoval.OtherCodePopsTheModalPage)]
	[InlineData(PopupRemoval.ShellPopsThePopupFromAModalNavigationPage)]
	[InlineData(PopupRemoval.PageOfTheWindowIsReplaced)]
	[InlineData(PopupRemoval.ShellSwitchesToAnotherShellElement)]
	public async Task ShowingAPopupInAnotherWindowSucceedsAfterTheShellWindowThatRemovedAPopupWasClosed(PopupRemoval removal)
	{
		SetupBuilder();
		var shell = PopupDismissedByNavigationScenarios.CreateShell(out var firstPage);
		var pageOfOtherWindow = new ContentPage();
		var popup = new Popup { Padding = 0, Content = new Label { Text = "Popup" } };
		var objectDisposedExceptions = new ConcurrentQueue<string>();

		void HandleFirstChanceException(object? sender, FirstChanceExceptionEventArgs e)
		{
			if (e.Exception is ObjectDisposedException objectDisposedException)
			{
				objectDisposedExceptions.Enqueue(objectDisposedException.ToString());
			}
		}

		AppDomain.CurrentDomain.FirstChanceException += HandleFirstChanceException;
		try
		{
			await RunInWindowAsync(shell, firstPage, window => ShowAndRemoveAPopupInAShellWindow(shell, window, popup, removal));
			await RunInWindowAsync(pageOfOtherWindow, pageOfOtherWindow, _ => PopupTryMethodsOnPageScenarios.ShowAndCloseAPopupOnAPage(pageOfOtherWindow, popup));
			await Task.Delay(TimeSpan.FromSeconds(1));
		}
		finally
		{
			AppDomain.CurrentDomain.FirstChanceException -= HandleFirstChanceException;
		}

		Assert.Empty(objectDisposedExceptions);
	}

	static async Task ShowAndRemoveAPopupInAShellWindow(Shell shell, Window window, Popup popup, PopupRemoval removal)
	{
		const int timeout = 5000;
		var modalNavigationPage = removal is PopupRemoval.ShellPopsThePopupFromAModalNavigationPage ? new NavigationPage(new ContentPage { Title = "Modal" }) : null;
		if (modalNavigationPage is not null)
		{
			await shell.Navigation.PushModalAsync(modalNavigationPage, false);
		}

		var hasOpened = false;
		void HandlePopupOpened(object? sender, EventArgs e) => hasOpened = true;
		popup.Opened += HandlePopupOpened;

		var showPopupTask = shell.ShowPopupAsync(popup, new PopupOptions(), token: CancellationToken.None);
		await AssertionExtensions.AssertEventually(() => hasOpened && (modalNavigationPage?.Navigation.NavigationStack ?? shell.Navigation.ModalStack).OfType<PopupPage>().Any(), timeout: timeout, message: "The popup was never shown.");
		popup.Opened -= HandlePopupOpened;

		switch (removal)
		{
			case PopupRemoval.CloseAsync:
				await shell.ClosePopupAsync().WaitAsync(TimeSpan.FromMilliseconds(timeout));
				break;
			case PopupRemoval.OtherCodePopsTheModalPage:
				await shell.Navigation.PopModalAsync(false);
				break;
			case PopupRemoval.ShellPopsThePopupFromAModalNavigationPage:
				await shell.GoToAsync("..");
				break;
			case PopupRemoval.PageOfTheWindowIsReplaced:
				window.Page = new ContentPage();
				break;
			case PopupRemoval.ShellSwitchesToAnotherShellElement:
				await shell.GoToAsync("//second");
				break;
			default:
				throw new NotSupportedException($"{removal} is not a supported removal.");
		}

		await showPopupTask.WaitAsync(TimeSpan.FromMilliseconds(timeout));
	}
}