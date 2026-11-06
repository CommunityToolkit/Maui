using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.DeviceTests;

// The scenarios use .NET MAUI's own navigation of a Shell that is the root page of a Window: the platform files of PopupDismissedByNavigationTests host that Window.
// A popup shown with Shell.ShowPopupAsync is a Shell route, which .NET MAUI pushes onto the NavigationPage when the Shell shows a modal NavigationPage.
static class PopupDismissedByNavigationScenarios
{
	const int timeout = 5000;

	public static Shell CreateShell(out ContentPage firstPage)
	{
		firstPage = new ContentPage { Title = "First" };

		return new Shell
		{
			Items =
			{
				new ShellContent { Route = "first", Content = firstPage },
				new ShellContent { Route = "second", Content = new ContentPage { Title = "Second" } },
			}
		};
	}

	// Two popups on top of a modal NavigationPage were never closed: CloseAsync popped the whole NavigationPage, and ShowPopupAsync never completed.
	public static async Task CloseTwoPopupsOneAfterTheOther(Shell shell, bool closeWithClosePopupAsync, Func<NavigationPage, int?, Task> checkPlatformState)
	{
		var modalNavigationPage = new NavigationPage(new ContentPage { Title = "Modal" });
		await shell.Navigation.PushModalAsync(modalNavigationPage, false);
		// null: the platform state before any popup is shown
		await checkPlatformState(modalNavigationPage, null);

		var lowerPopup = CreatePopup("Lower");
		var upperPopup = CreatePopup("Upper");
		var lowerTask = shell.ShowPopupAsync(lowerPopup, CreateOptions(), token: CancellationToken.None);
		await AssertionExtensions.AssertEventually(() => CountPopupPages(modalNavigationPage) == 1, timeout: timeout, message: "The lower popup was never shown.");
		var upperTask = shell.ShowPopupAsync(upperPopup, CreateOptions(), token: CancellationToken.None);
		await AssertionExtensions.AssertEventually(() => CountPopupPages(modalNavigationPage) == 2, timeout: timeout, message: "The upper popup was never shown.");

		await Close(shell, upperPopup, closeWithClosePopupAsync);
		await upperTask.WaitAsync(TimeSpan.FromMilliseconds(timeout));
		Assert.Equal(1, CountPopupPages(modalNavigationPage));
		Assert.Equal(2, modalNavigationPage.Navigation.NavigationStack.Count());
		await checkPlatformState(modalNavigationPage, 2);

		await Close(shell, lowerPopup, closeWithClosePopupAsync);
		await lowerTask.WaitAsync(TimeSpan.FromMilliseconds(timeout));
		Assert.Equal(0, CountPopupPages(modalNavigationPage));
		Assert.Single(modalNavigationPage.Navigation.NavigationStack);
		Assert.Same(modalNavigationPage, Assert.Single(shell.Navigation.ModalStack));
		await checkPlatformState(modalNavigationPage, 1);

		// Closing the popups must leave Shell able to navigate: it neither throws nor keeps the routes of the closed popups
		await shell.GoToAsync("..").WaitAsync(TimeSpan.FromMilliseconds(timeout));
		Assert.Empty(shell.Navigation.ModalStack);
		Assert.DoesNotContain("PopupPage", shell.CurrentState.Location.OriginalString, StringComparison.Ordinal);
	}

	// Shell's GoToAsync("..") pops the PopupPage from the modal NavigationPage with ModalStack[^1].Navigation.PopAsync, which raises no Window.ModalPopped
	public static async Task CompleteWhenShellPopsThePopupFromTheModalNavigationPage(Shell shell, bool isOpenedRaised = true)
	{
		var modalNavigationPage = new NavigationPage(new ContentPage { Title = "Modal" });
		await shell.Navigation.PushModalAsync(modalNavigationPage, false);
		var popup = CreatePopup("Popup");
		var closedCount = 0;
		var hasOpened = false;
		popup.Closed += (_, _) => closedCount++;
		popup.Opened += (_, _) => hasOpened = true;

		var showPopupTask = shell.ShowPopupAsync(popup, CreateOptions(), token: CancellationToken.None);
		await AssertionExtensions.AssertEventually(() => CountPopupPages(modalNavigationPage) == 1 && (hasOpened || !isOpenedRaised), timeout: timeout, message: "The popup was never shown.");

		await shell.GoToAsync("..");
		var result = await showPopupTask.WaitAsync(TimeSpan.FromMilliseconds(timeout));

		Assert.True(result.WasDismissedByTappingOutsideOfPopup);
		Assert.Equal(1, closedCount);
		Assert.DoesNotContain("PopupPage", shell.CurrentState.Location.OriginalString, StringComparison.Ordinal);
		Assert.Same(modalNavigationPage, Assert.Single(shell.Navigation.ModalStack));
		Assert.Single(modalNavigationPage.Navigation.NavigationStack);
	}

	public static async Task CompleteWhenShellSwitchesToAnotherShellElement(Shell shell, bool isOpenedRaised = true)
	{
		var popup = CreatePopup("Popup");
		var closedCount = 0;
		var hasOpened = false;
		popup.Closed += (_, _) => closedCount++;
		popup.Opened += (_, _) => hasOpened = true;

		// Opened is raised when .NET MAUI reports that the PopupPage was navigated to. A Window without a handler never reports it, and the removal must be observed regardless
		var showPopupTask = shell.ShowPopupAsync(popup, CreateOptions(), token: CancellationToken.None);
		await AssertionExtensions.AssertEventually(() => shell.Navigation.ModalStack.OfType<PopupPage>().Count() == 1 && (hasOpened || !isOpenedRaised), timeout: timeout, message: "The popup was never shown.");

		await shell.GoToAsync("//second");
		var result = await showPopupTask.WaitAsync(TimeSpan.FromMilliseconds(timeout));

		Assert.True(result.WasDismissedByTappingOutsideOfPopup);
		Assert.Equal(1, closedCount);
		Assert.Empty(shell.Navigation.ModalStack);
	}

	public static async Task CompleteWhenOtherCodePopsTheModalPage(Shell shell)
	{
		var popup = CreatePopup("Popup");
		var closedCount = 0;
		var hasOpened = false;
		popup.Closed += (_, _) => closedCount++;
		popup.Opened += (_, _) => hasOpened = true;

		// Opened is raised when .NET MAUI reports that the PopupPage was navigated to, which is when the popup starts to watch for its removal by navigation
		var showPopupTask = shell.ShowPopupAsync(popup, CreateOptions(), token: CancellationToken.None);
		await AssertionExtensions.AssertEventually(() => shell.Navigation.ModalStack.OfType<PopupPage>().Count() == 1 && hasOpened, timeout: timeout, message: "The popup was never shown.");

		await shell.Navigation.PopModalAsync(false);
		var result = await showPopupTask.WaitAsync(TimeSpan.FromMilliseconds(timeout));

		Assert.True(result.WasDismissedByTappingOutsideOfPopup);
		Assert.Equal(1, closedCount);
		Assert.Empty(shell.Navigation.ModalStack);
	}

	// Bounded, because CloseAsync waits for .NET MAUI to confirm that the PopupPage was popped, which never happens when it pops the wrong page
	static Task Close(Shell shell, Popup popup, bool closeWithClosePopupAsync) =>
		(closeWithClosePopupAsync
			? (Task)shell.ClosePopupAsync()
			: popup.CloseAsync()).WaitAsync(TimeSpan.FromMilliseconds(timeout));

	static int CountPopupPages(NavigationPage modalNavigationPage) => modalNavigationPage.Navigation.NavigationStack.OfType<PopupPage>().Count();

	static Popup CreatePopup(string text) => new()
	{
		Padding = 0,
		Content = new Label { Text = text }
	};

	static PopupOptions CreateOptions() => new() { Shape = null };
}
