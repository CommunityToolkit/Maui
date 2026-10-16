using CommunityToolkit.Maui.Alerts;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

public abstract partial class AlertsTestBase
{
	Application? applicationWithTestHandler;
	IElementHandler? previousApplicationHandler;

	// Apple platforms need no setup: the headless runner's key window hosts the alert views (see AlertsApple).
	protected static Task PreparePlatformAsync() => Task.CompletedTask;

	// Gives the runner's application a handler whose MauiContext has this test's services, as in a real app (the headless Apple runner
	// has none): the toolkit then resolves IFontManager through it. CleanUpAlertsAsync puts the previous handler back.
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