using Microsoft.Windows.AppNotifications;

namespace CommunityToolkit.Maui.Alerts;

static class AppNotificationListing
{
	// Windows may stop listing notifications, for example when the user turned them off for the app, and Show must not wait for that.
	static readonly TimeSpan listingTimeout = TimeSpan.FromSeconds(1);
	static readonly TimeSpan pollingInterval = TimeSpan.FromMilliseconds(10);

	// Windows may list a notification only a moment after Show returned, and a RemoveByIdAsync that reaches it first removes nothing, which
	// leaves the notification on screen. Show therefore returns once Windows lists the notification, so that the removal that follows works.
	internal static async Task WaitUntilListedAsync(AppNotification notification)
	{
		using var timeoutTokenSource = new CancellationTokenSource(listingTimeout);

		try
		{
			while (!(await AppNotificationManager.Default.GetAllAsync()).Any(listed => listed.Id == notification.Id))
			{
				await Task.Delay(pollingInterval, timeoutTokenSource.Token);
			}
		}
		catch (OperationCanceledException) when (timeoutTokenSource.IsCancellationRequested)
		{
			// Windows lists nothing for the app, so there is nothing more to wait for
		}
	}
}
