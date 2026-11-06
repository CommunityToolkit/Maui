using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Platform;
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

			// A closed popup must leave the modal NavigationPage as the view controller on top, as it was before the popups were shown, and must have left its native navigation controller.
			// The popups are pushed into the NavigationPage and not presented, so only the depth of the native navigation controller shows whether a popup's view controller is gone.
			return PopupDismissedByNavigationScenarios.CloseTwoPopupsOneAfterTheOther(shell, closeWithClosePopupAsync, async (modalNavigationPage, expectedDepth) =>
			{
				if (expectedDepth is not int depth)
				{
					viewControllerOfTheModalNavigationPage = GetDeepestPresentedViewController(host.PlatformWindow);
					return;
				}

				await AssertionExtensions.AssertEventually(() => ReferenceEquals(GetDeepestPresentedViewController(host.PlatformWindow), viewControllerOfTheModalNavigationPage),
					timeout: 5000, message: "The view controller of the modal NavigationPage is not the one on top.");

				var navigationController = FindNavigationController((modalNavigationPage.Handler as IPlatformViewHandler)?.ViewController)
					?? throw new InvalidOperationException("The modal NavigationPage has no UINavigationController.");
				await AssertionExtensions.AssertEventually(() => navigationController.ViewControllers?.Length == depth,
					timeout: 5000, message: $"The native navigation controller of the modal NavigationPage does not hold {depth} view controllers.");
			});
		});

	[Fact]
	public Task ShowPopupAsyncCompletesWhenShellPopsThePopupFromTheModalNavigationPage() =>
		RunInShellAsync((shell, _) => PopupDismissedByNavigationScenarios.CompleteWhenShellPopsThePopupFromTheModalNavigationPage(shell));

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

	// A regression guard, not a proof of the Dispatcher: the same test also passes on iOS when the result is raised synchronously. The unit test with a queueing dispatcher proves the deferral.
	// The Closed handler of a popup that navigation removed shows another popup right away, and that popup must be shown on top of the page .NET MAUI has finished removing
	[Fact]
	public Task ClosedHandlerOfAPopupRemovedByNavigationCanShowAnotherPopup() =>
		RunInShellAsync(async (shell, host) =>
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
			Assert.NotSame(host.PlatformWindow.RootViewController, GetDeepestPresentedViewController(host.PlatformWindow));

			await shell.ClosePopupAsync().WaitAsync(TimeSpan.FromSeconds(5));
			await (secondShowPopupTask ?? throw new InvalidOperationException("The Closed handler did not show a popup.")).WaitAsync(TimeSpan.FromSeconds(5));
		});

	Task RunInShellAsync(Func<Shell, HostedMauiWindow, Task> scenario)
	{
		SetupBuilder();
		var shell = PopupDismissedByNavigationScenarios.CreateShell(out var firstPage);

		return RunInMauiWindowAsync(shell, isPagePresentedModally: false, firstPage, host => scenario(shell, host));
	}

	static UINavigationController? FindNavigationController(UIViewController? viewController)
	{
		if (viewController is UINavigationController navigationController)
		{
			return navigationController;
		}

		return viewController?.ChildViewControllers.Select(FindNavigationController).FirstOrDefault(static found => found is not null);
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
