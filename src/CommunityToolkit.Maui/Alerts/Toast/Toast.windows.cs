using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace CommunityToolkit.Maui.Alerts;

public partial class Toast
{
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
		if (PlatformToast is null)
		{
			return;
		}

		token.ThrowIfCancellationRequested();
		await AppNotificationManager.Default.RemoveAllAsync();

		// Verify PlatformToast is not null again after `await`
		if (PlatformToast is not null)
		{
			PlatformToast.Expiration = DateTimeOffset.Now;
			PlatformToast = null;
		}
	}

	async Task ShowPlatform(CancellationToken token)
	{
		await DismissPlatform(token);
		token.ThrowIfCancellationRequested();
		PlatformToast = new AppNotificationBuilder()
			.AddText(Text)
			.SetDuration(GetAppNotificationDuration(Duration))
			.BuildNotification();
		PlatformToast.Expiration = DateTimeOffset.Now.Add(GetDuration(Duration));
		AppNotificationManager.Default.Show(PlatformToast);
	}

	static AppNotificationDuration GetAppNotificationDuration(ToastDuration duration) => duration switch
	{
		ToastDuration.Short => AppNotificationDuration.Default,
		ToastDuration.Long => AppNotificationDuration.Long,
		_ => throw new InvalidEnumArgumentException(nameof(Duration), (int)duration, typeof(ToastDuration))
	};
}