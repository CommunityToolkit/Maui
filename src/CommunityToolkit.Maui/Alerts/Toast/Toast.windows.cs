using Windows.UI.Notifications;
using static CommunityToolkit.Maui.Extensions.ToastNotificationExtensions;

namespace CommunityToolkit.Maui.Alerts;

public partial class Toast
{
	static Windows.UI.Notifications.ToastNotification? PlatformToast { get; set; }

	/// <summary>
	/// Dispose Toast
	/// </summary>
	protected virtual void Dispose(bool isDisposing)
	{
		if (isDisposed)
		{
			return;
		}

		if (isDisposing)
		{
		}

		isDisposed = true;
	}

	static void DismissPlatform(CancellationToken token)
	{
		if (PlatformToast is not null)
		{
			token.ThrowIfCancellationRequested();
			ToastNotificationManager.History.Clear();

			PlatformToast.ExpirationTime = DateTimeOffset.Now;
			PlatformToast = null;
		}
	}

	void ShowPlatform(CancellationToken token)
	{
		DismissPlatform(token);
		token.ThrowIfCancellationRequested();

		PlatformToast = new ToastNotification(BuildToastNotificationContent(Text))
		{
			ExpirationTime = DateTimeOffset.Now.Add(GetDuration(Duration))
		};

		ToastNotificationManager.CreateToastNotifier().Show(PlatformToast);
	}
}