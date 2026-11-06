using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class PopupDismissedByNavigationTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public Task TwoPopupsOnTopOfAModalNavigationPageCloseOneAfterTheOther(bool closeWithClosePopupAsync) =>
		RunInShellAsync((shell, host) =>
		{
			UIViewController? viewControllerOfTheModalNavigationPage = null;

			// A closed popup must leave the modal NavigationPage as the view controller on top, as it was before the popups were shown
			return PopupDismissedByNavigationScenarios.CloseTwoPopupsOneAfterTheOther(shell, closeWithClosePopupAsync, async isBaseline =>
			{
				if (isBaseline)
				{
					viewControllerOfTheModalNavigationPage = GetDeepestPresentedViewController(host.PlatformWindow);
					return;
				}

				await AssertionExtensions.AssertEventually(() => ReferenceEquals(GetDeepestPresentedViewController(host.PlatformWindow), viewControllerOfTheModalNavigationPage),
					timeout: 5000, message: "The view controller of the modal NavigationPage is not the one on top.");
			});
		});

	[Fact]
	public Task ShowPopupAsyncCompletesWhenShellPopsTheModalNavigationPageThatContainsThePopup() =>
		RunInShellAsync((shell, _) => PopupDismissedByNavigationScenarios.CompleteWhenShellPopsTheModalNavigationPageThatContainsThePopup(shell));

	[Fact]
	public Task ShowPopupAsyncCompletesWhenShellSwitchesToAnotherShellElement() =>
		RunInShellAsync((shell, _) => PopupDismissedByNavigationScenarios.CompleteWhenShellSwitchesToAnotherShellElement(shell));

	[Fact]
	public Task ShowPopupAsyncCompletesWhenOtherCodePopsTheModalPage() =>
		RunInShellAsync((shell, _) => PopupDismissedByNavigationScenarios.CompleteWhenOtherCodePopsTheModalPage(shell));

	[Fact]
	public Task ShowPopupAsyncCompletesWhenThePageOfTheWindowIsReplaced() =>
		RunInShellAsync(async (shell, host) =>
		{
			var popup = new Popup();
			var closedCount = 0;
			var hasOpened = false;
			popup.Closed += (_, _) => closedCount++;
			popup.Opened += (_, _) => hasOpened = true;

			var showPopupTask = shell.ShowPopupAsync(popup, new PopupOptions { Shape = null }, token: CancellationToken.None);
			await AssertionExtensions.AssertEventually(() => shell.Navigation.ModalStack.OfType<PopupPage>().Count() == 1 && hasOpened, timeout: 5000, message: "The popup was never shown.");

			host.Window.Page = new ContentPage();
			var result = await showPopupTask.WaitAsync(TimeSpan.FromSeconds(5));

			Assert.True(result.WasDismissedByTappingOutsideOfPopup);
			Assert.Equal(1, closedCount);
		});

	Task RunInShellAsync(Func<Shell, HostedMauiWindow, Task> scenario)
	{
		SetupBuilder();
		var shell = PopupDismissedByNavigationScenarios.CreateShell(out var firstPage);

		return RunInMauiWindowAsync(shell, isPagePresentedModally: false, firstPage, host => scenario(shell, host));
	}

	static UIViewController GetDeepestPresentedViewController(UIWindow platformWindow)
	{
		var viewController = platformWindow.RootViewController ?? throw new InvalidOperationException("The window has no root view controller.");
		while (viewController.PresentedViewController is UIViewController presented)
		{
			viewController = presented;
		}

		return viewController;
	}
}
