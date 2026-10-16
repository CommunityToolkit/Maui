namespace CommunityToolkit.Maui.DeviceTests;

public abstract partial class AlertsTestBase
{
	// The Windows tests run inside the visual runner's real window and need no application of their own. When Windows only fails to list
	// notifications, a test still runs up to its first read of the Action Center (AlertsWindows.GetDisplayedAsync), which skips it. Every
	// test starts with an empty Action Center, so a notification of an earlier test is never mistaken for the one under test.
	protected static async Task PreparePlatformAsync()
	{
		await AlertsWindows.SkipWhenNotificationsCannotBeShownAsync();
		await AlertsWindows.TryRemoveAllAsync();
	}

	async Task CleanUpAlertsAsync()
	{
		await AlertsWindows.TryRemoveAllAsync();
		AlertsWindows.GetRegisteredActions().Clear();
		ResetToolkitAlertState();
	}
}