using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

public abstract partial class PopupWindowHostTestBase
{
	protected Task RunInShellAsync(Func<Shell, HostedMauiWindow, Task> scenario, Action<MauiAppBuilder>? configureBuilder = null)
	{
		SetupBuilder(configureBuilder);
		var shell = PopupDismissedByNavigationScenarios.CreateShell(out var firstPage);

		return RunInMauiWindowAsync(shell, isPagePresentedModally: false, firstPage, host => scenario(shell, host));
	}

	protected static Task AssertPopupIsPresentedAsync(HostedMauiWindow host, bool isPresented) =>
		AssertionExtensions.AssertEventually(() => isPresented != ReferenceEquals(GetDeepestPresentedViewController(host.PlatformWindow), host.PlatformWindow.RootViewController),
			timeout: 5000, message: isPresented ? "The popup is not presented." : "The popup is still presented.");

	protected static UIViewController GetDeepestPresentedViewController(UIWindow platformWindow)
	{
		var viewController = platformWindow.RootViewController ?? throw new InvalidOperationException("The window has no root view controller.");
		while (viewController.PresentedViewController is UIViewController presented)
		{
			viewController = presented;
		}

		return viewController;
	}
}