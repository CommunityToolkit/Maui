using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;

namespace CommunityToolkit.Maui.Views.Popup.SnackBar.Platforms;

class PlatformPopupExtensions : IPlatformPopupExtensions
{
	public void Dismiss(Snackbar snackbar)
	{
		if (snackbar.nativeSnackbar is not null)
		{
			ToastNotificationManagerCompat.History.Clear();
			snackbar.nativeSnackbar.ExpirationTime = System.DateTimeOffset.Now;
		}

		snackbar.OnDismissed();
	}

	public ToastNotification Show(Snackbar snackBar)
	{
		var toastContentBuilder = new ToastContentBuilder()
			.AddText(snackBar.Text)
			.AddButton(new ToastButton() { ActivationType = ToastActivationType.Foreground }.SetContent(snackBar.ActionButtonText));
		var toastContent = toastContentBuilder.GetToastContent();
		toastContent.ActivationType = ToastActivationType.Background;
		var toast = new ToastNotification(toastContent.GetXml());
		toast.Activated += delegate (ToastNotification sender, object args)
		{
			Microsoft.Maui.Controls.Device.BeginInvokeOnMainThread(snackBar.Action);
		};
		toast.Dismissed += delegate (ToastNotification sender, ToastDismissedEventArgs args)
		{
			snackBar.OnDismissed();
		};
		toast.ExpirationTime = System.DateTime.Now.Add(snackBar.Duration);
		ToastNotificationManager.CreateToastNotifier().Show(toast);
		return toast;
	}
}
