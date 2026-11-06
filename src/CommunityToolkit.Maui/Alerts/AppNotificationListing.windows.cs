using System.Runtime.InteropServices;
using Microsoft.Windows.AppNotifications;

namespace CommunityToolkit.Maui.Alerts;

static class AppNotificationListing
{
	// Windows may never list the notification, for example while the user turned notifications off for the app, so the wait is bounded.
	static readonly TimeSpan listingTimeout = TimeSpan.FromSeconds(1);
	static readonly TimeSpan pollingInterval = TimeSpan.FromMilliseconds(10);

	// Windows may list a notification only a moment after Show returned, and a RemoveByIdAsync that reaches it first removes nothing, which
	// leaves the notification on screen. Show therefore returns once Windows lists the notification, so that the removal that follows works.
	internal static async Task WaitUntilListedAsync(AppNotification notification)
	{
		using var timeoutTokenSource = new CancellationTokenSource(listingTimeout);

		try
		{
			// Windows may never list a notification that expired, such as the one of a snackbar whose Duration is zero or less
			while (notification.Expiration > DateTimeOffset.Now && !await IsListedAsync(notification, timeoutTokenSource.Token))
			{
				await Task.Delay(pollingInterval, timeoutTokenSource.Token);
			}
		}
		catch (OperationCanceledException) when (timeoutTokenSource.IsCancellationRequested)
		{
			// Windows did not list the notification in time, so Show goes on without the listing
		}
		catch (Exception exception) when (exception is COMException or InvalidOperationException or NotSupportedException or UnauthorizedAccessException)
		{
			// Windows showed the notification but cannot list the notifications of the app in this session, and Show must not fail for that
		}
	}

	static async Task<bool> IsListedAsync(AppNotification notification, CancellationToken token) =>
		(await AppNotificationManager.Default.GetAllAsync().AsTask(token)).Any(listed => listed.Id == notification.Id);
}
