using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Window = Microsoft.Maui.Controls.Window;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class PopupDismissedByNavigationTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public Task TwoPopupsOnTopOfAModalNavigationPageCloseOneAfterTheOther(bool closeWithClosePopupAsync) =>
		RunInShellAsync(shell => PopupDismissedByNavigationScenarios.CloseTwoPopupsOneAfterTheOther(shell, closeWithClosePopupAsync, static (_, _) => Task.CompletedTask));

	[Fact]
	public Task ShowPopupAsyncCompletesWhenShellPopsThePopupFromTheModalNavigationPage() =>
		RunInShellAsync(shell => PopupDismissedByNavigationScenarios.CompleteWhenShellPopsThePopupFromTheModalNavigationPage(shell));

	[Fact]
	public Task ShowPopupAsyncCompletesWhenShellSwitchesToAnotherShellElement() =>
		RunInShellAsync(shell => PopupDismissedByNavigationScenarios.CompleteWhenShellSwitchesToAnotherShellElement(shell));

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public Task CloseThatWaitedWhileAnotherPopupWasShownThrowsPopupBlockedException(bool onTopOfAModalNavigationPage) =>
		RunInShellAsync(shell => PopupDismissedByNavigationScenarios.CloseThatWaitedWhileAnotherPopupWasShownThrowsPopupBlockedException(shell, onTopOfAModalNavigationPage));

	[Fact]
	public Task ShowPopupAsyncCompletesWhenOtherCodePopsTheModalPage() =>
		RunInShellAsync(shell => PopupDismissedByNavigationScenarios.CompleteWhenOtherCodePopsTheModalPage(shell));

	[Fact]
	public Task ShowPopupAsyncCompletesWhenThePageOfTheWindowIsReplaced()
	{
		SetupBuilder();
		var shell = PopupDismissedByNavigationScenarios.CreateShell(out var firstPage);

		return RunInWindowAsync(shell, firstPage, async window =>
		{
			var popup = new Popup();
			var closedCount = 0;
			var hasOpened = false;
			popup.Closed += (_, _) => closedCount++;
			popup.Opened += (_, _) => hasOpened = true;

			var showPopupTask = shell.ShowPopupAsync(popup, new PopupOptions { Shape = null }, token: CancellationToken.None);
			await AssertionExtensions.AssertEventually(() => shell.Navigation.ModalStack.OfType<PopupPage>().Count() == 1 && hasOpened, timeout: 5000, message: "The popup was never shown.");

			window.Page = new ContentPage();
			var result = await showPopupTask.WaitAsync(TimeSpan.FromSeconds(5));

			Assert.True(result.WasDismissedByTappingOutsideOfPopup);
			Assert.Equal(1, closedCount);
		});
	}

	[Fact]
	public Task ClosedHandlerOfAPopupRemovedByNavigationCanShowAnotherPopup() =>
		RunInShellAsync(async shell =>
		{
			var firstPopup = new Popup();
			var secondPopup = new Popup();
			var hasFirstOpened = false;
			var hasSecondOpened = false;
			Task<IPopupResult>? secondShowPopupTask = null;
			firstPopup.Opened += (_, _) => hasFirstOpened = true;
			secondPopup.Opened += (_, _) => hasSecondOpened = true;
			firstPopup.Closed += (_, _) => secondShowPopupTask = shell.ShowPopupAsync(secondPopup, new PopupOptions { Shape = null }, token: CancellationToken.None);

			var firstShowPopupTask = shell.ShowPopupAsync(firstPopup, new PopupOptions { Shape = null }, token: CancellationToken.None);
			await AssertionExtensions.AssertEventually(() => hasFirstOpened, timeout: 5000, message: "The first popup was never shown.");

			await shell.GoToAsync("//second");
			await firstShowPopupTask.WaitAsync(TimeSpan.FromSeconds(5));
			await AssertionExtensions.AssertEventually(() => hasSecondOpened, timeout: 5000, message: "The popup shown by the Closed handler was never shown.");

			Assert.Single(shell.Navigation.ModalStack.OfType<PopupPage>());

			await shell.ClosePopupAsync().WaitAsync(TimeSpan.FromSeconds(5));
			await (secondShowPopupTask ?? throw new InvalidOperationException("The Closed handler did not show a popup.")).WaitAsync(TimeSpan.FromSeconds(5));
		});
}