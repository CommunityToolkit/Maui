namespace CommunityToolkit.Maui.DeviceTests;

public abstract partial class AlertsTestBase
{
	// The Windows tests run inside the visual runner's real window and need no application of their own. The first test probes whether
	// the app can show notifications and whether Windows lists them back (the probe removes every notification, so it runs before a test
	// shows its own). A test skips when the app cannot show notifications in this session, and only the assertions that read the
	// Action Center skip when Windows does not list them (AlertsWindows.GetDisplayedAsync). Every test starts with an empty Action Center,
	// so a notification of an earlier test is never mistaken for the one under test.
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