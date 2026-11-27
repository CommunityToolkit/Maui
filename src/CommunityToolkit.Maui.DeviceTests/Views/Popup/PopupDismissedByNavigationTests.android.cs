using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class PopupDismissedByNavigationTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public Task TwoPopupsOnTopOfAModalNavigationPageCloseOneAfterTheOther(bool closeWithClosePopupAsync)
	{
		SetupBuilder();
		var shell = PopupDismissedByNavigationScenarios.CreateShell(out var firstPage);

		return RunInWindowAsync(shell, firstPage, hostShell: true, _ =>
			PopupDismissedByNavigationScenarios.CloseTwoPopupsOneAfterTheOther(shell, closeWithClosePopupAsync, static (_, _) => Task.CompletedTask));
	}

	// A Window without a handler makes a Shell raise no NavigatedTo on the pages it pushes, so these two cover a PopupPage that navigation removes before NavigatedTo
	[Fact]
	public Task ShowPopupAsyncCompletesWhenShellPopsThePopupFromTheModalNavigationPage()
	{
		SetupBuilder();
		var shell = PopupDismissedByNavigationScenarios.CreateShell(out var firstPage);

		return RunInWindowAsync(shell, firstPage, hostShell: true, _ => PopupDismissedByNavigationScenarios.CompleteWhenShellPopsThePopupFromTheModalNavigationPage(shell, isOpenedRaised: false));
	}

	[Fact]
	public Task ShowPopupAsyncCompletesWhenShellSwitchesToAnotherShellElement()
	{
		SetupBuilder();
		var shell = PopupDismissedByNavigationScenarios.CreateShell(out var firstPage);

		return RunInWindowAsync(shell, firstPage, hostShell: true, _ => PopupDismissedByNavigationScenarios.CompleteWhenShellSwitchesToAnotherShellElement(shell, isOpenedRaised: false));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public Task CloseThatWaitedWhileAnotherPopupWasShownThrowsPopupBlockedException(bool onTopOfAModalNavigationPage)
	{
		SetupBuilder();
		var shell = PopupDismissedByNavigationScenarios.CreateShell(out var firstPage);

		return RunInWindowAsync(shell, firstPage, hostShell: true, _ =>
			PopupDismissedByNavigationScenarios.CloseThatWaitedWhileAnotherPopupWasShownThrowsPopupBlockedException(shell, onTopOfAModalNavigationPage));
	}

	[Fact]
	public Task ShowPopupAsyncCompletesWhenOtherCodePopsTheModalPage()
	{
		SetupBuilder();
		var page = new ContentPage();

		return RunInWindowAsync(page, page, hostShell: false, async _ =>
		{
			var popup = new Popup();
			var closedCount = 0;
			var hasOpened = false;
			popup.Closed += (_, _) => closedCount++;
			popup.Opened += (_, _) => hasOpened = true;

			var showPopupTask = page.ShowPopupAsync(popup, new PopupOptions { Shape = null });
			await AssertionExtensions.AssertEventually(() => page.Navigation.ModalStack.OfType<PopupPage>().Count() == 1 && hasOpened, timeout: 5000, message: "The popup was never shown.");

			await page.Navigation.PopModalAsync(false);
			var result = await showPopupTask.WaitAsync(TimeSpan.FromSeconds(5));

			Assert.True(result.WasDismissedByTappingOutsideOfPopup);
			Assert.Equal(1, closedCount);
		});
	}
}