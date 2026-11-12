using CommunityToolkit.Maui.Alerts;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

public abstract partial class AlertsTestBase
{
	Application? applicationWithTestHandler;
	IElementHandler? previousApplicationHandler;

	// Apple platforms need no setup: the headless runner's key window hosts the alert views (see AlertsApple).
	protected static Task PreparePlatformAsync() => Task.CompletedTask;

	// The headless Apple runner's application has no handler, unlike a real app's: the toolkit resolves IFontManager through the handler's
	// MauiContext, which here has this test's services. CleanUpAlertsAsync puts the previous handler back.
	protected void InstallApplicationHandler()
	{
		applicationWithTestHandler = Application.Current ?? throw new InvalidOperationException("The runner has no Application.");
		previousApplicationHandler = applicationWithTestHandler.Handler;
		applicationWithTestHandler.Handler = new StubApplicationHandler(MauiContext);
	}

	Task CleanUpAlertsAsync()
	{
		_ = Snackbar.Make(string.Empty).Dismiss(CancellationToken.None);
		_ = Toast.Make(string.Empty).Dismiss(CancellationToken.None);

		foreach (var alertView in AlertsApple.GetAlertViews())
		{
			alertView.RemoveFromSuperview();
		}

		ResetToolkitAlertState();

		if (applicationWithTestHandler is not null)
		{
			applicationWithTestHandler.Handler = previousApplicationHandler;
			applicationWithTestHandler = null;
		}

		return Task.CompletedTask;
	}
}