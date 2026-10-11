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

		if (PlatformToast is null)
		{
			return;
		}

		await AppNotificationManager.Default.RemoveAllAsync();

		// Verify PlatformToast is not null again after `await`
		if (PlatformToast is not null)
		{
			PlatformToast.Expiration = DateTimeOffset.Now;
			PlatformToast = null;
		}
	}

	static AppNotificationDuration GetAppNotificationDuration(ToastDuration duration) => duration switch
	{
		ToastDuration.Short => AppNotificationDuration.Default,
		ToastDuration.Long => AppNotificationDuration.Long,
		_ => throw new InvalidEnumArgumentException(nameof(Duration), (int)duration, typeof(ToastDuration))
	};

	// Takes the notification off screen at the end of the Duration of the toast, as the toast disappears on the other platforms. It is
	// removed by its Id, so the other notifications of the app stay, and also when PlatformToast is another toast by then: two Show calls
	// that overlap can both show their notification. When Show or Dismiss removed it first, there is nothing left to remove.
	// Best effort: nothing awaits the removal, and the notification expires a minute later anyway.
	static async Task RemoveAtTheEndOfTheDurationAsync(AppNotification platformToast, TimeSpan duration)
	{
		await Task.Delay(duration).ConfigureAwait(false);

		_ = AppNotificationManager.Default.RemoveByIdAsync(platformToast.Id);
	}

	async Task ShowPlatform(CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		EnsureWindowsToastEnabled();

		await DismissPlatform(token);
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
}