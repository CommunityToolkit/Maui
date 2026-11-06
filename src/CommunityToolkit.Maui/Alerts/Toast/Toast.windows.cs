using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace CommunityToolkit.Maui.Alerts;

public partial class Toast
{
	// Windows shows the banner of a notification for its own time (about 6 s for AppNotificationDuration.Default, up to 25 s for Long), and
	// it queues a notification that arrives while a banner is shown. Removing a notification takes its banner off screen only while the
	// notification has not expired, and an expired notification that waits in the queue is never shown. The toolkit therefore removes the
	// toast itself at the end of its Duration, and the notification expires only after that, to leave the Action Center if the app ends first.
	static readonly TimeSpan expirationAfterTheEndOfTheDuration = TimeSpan.FromMinutes(1);

	// Show and Dismiss take turns, in the order they are called. Otherwise a Show that starts while an earlier one waits for the removal of the
	// displayed toast can remove the notifications before the earlier toast is shown, and its own toast then waits behind that banner until
	// both are removed at the end of their Duration. WaitAsync can also hand the turn to a call whose token was cancelled while it waited, so
	// each call checks its token once its turn came.
	static readonly SemaphoreSlim showAndDismissSemaphore = new(1, 1);

	static AppNotification? PlatformToast { get; set; }

	/// <summary>
	/// Dispose Toast
	/// </summary>
	protected virtual void Dispose(bool isDisposing)
	{
		if (isDisposed)
		{
			return;
		}

		isDisposed = true;
	}

	static async Task DismissPlatform(CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		await showAndDismissSemaphore.WaitAsync(token);
		try
		{
			token.ThrowIfCancellationRequested();
			await RemovePlatformToastAsync();
		}
		finally
		{
			showAndDismissSemaphore.Release();
		}
	}

	static AppNotificationDuration GetAppNotificationDuration(ToastDuration duration) => duration switch
	{
		ToastDuration.Short => AppNotificationDuration.Default,
		ToastDuration.Long => AppNotificationDuration.Long,
		_ => throw new InvalidEnumArgumentException(nameof(Duration), (int)duration, typeof(ToastDuration))
	};

	// The notification is removed by its Id, so the other notifications of the app stay. When Show or Dismiss removed it first, there is
	// nothing left to remove. Best effort: nothing awaits the removal, and the notification expires a minute later anyway.
	static async Task RemoveAtTheEndOfTheDurationAsync(AppNotification platformToast, TimeSpan duration)
	{
		await Task.Delay(duration).ConfigureAwait(false);

		_ = AppNotificationManager.Default.RemoveByIdAsync(platformToast.Id);
	}

	// Removed by its Id, as at the end of the Duration: RemoveAllAsync would also remove the notifications that the app shows itself, such as
	// those of a Snackbar.
	static async Task RemovePlatformToastAsync()
	{
		if (PlatformToast is not AppNotification platformToast)
		{
			return;
		}

		await AppNotificationManager.Default.RemoveByIdAsync(platformToast.Id);

		platformToast.Expiration = DateTimeOffset.Now;
		PlatformToast = null;
	}

	async Task ShowPlatform(CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		EnsureWindowsToastEnabled();

		await showAndDismissSemaphore.WaitAsync(token);
		try
		{
			token.ThrowIfCancellationRequested();
			await RemovePlatformToastAsync();
			token.ThrowIfCancellationRequested();

			var duration = GetDuration(Duration);
			var platformToast = new AppNotificationBuilder()
				.AddText(Text)
				.SetDuration(GetAppNotificationDuration(Duration))
				.BuildNotification();
			platformToast.Expiration = DateTimeOffset.Now.Add(duration + expirationAfterTheEndOfTheDuration);

			PlatformToast = platformToast;
			AppNotificationManager.Default.Show(platformToast);

			_ = RemoveAtTheEndOfTheDurationAsync(platformToast, duration);
		}
		finally
		{
			showAndDismissSemaphore.Release();
		}
	}
}