using Windows.UI.Notifications;
using static CommunityToolkit.Maui.Extensions.ToastNotificationExtensions;

namespace CommunityToolkit.Maui.Alerts;

public partial class Toast
{
	private static partial void DismissPlatform(CancellationToken token)
	{
		if (PlatformToast is not null)
		{
			token.ThrowIfCancellationRequested();
			ToastNotificationManager.History.Clear();

			PlatformToast.ExpirationTime = DateTimeOffset.Now;
			PlatformToast = null;
		}
	}

	private partial void ShowPlatform(CancellationToken token)
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