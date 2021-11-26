using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;

namespace CommunityToolkit.Maui.Views.Popup.Snackbar.Platforms;

class PlatformPopupExtensions : IPlatformPopupExtensions
{
	private Snackbar? snackbar;
	public void Dismiss(Snackbar snackbar)
	{
		if (snackbar.nativeSnackbar is not null)
		{
			ToastNotificationManagerCompat.History.Clear();
			snackbar = null;
			snackbar.nativeSnackbar.Activated -= OnActivated;
			snackbar.nativeSnackbar.Dismissed -= OnDismissed;
			snackbar.nativeSnackbar.ExpirationTime = System.DateTimeOffset.Now;
		}

		snackbar.OnDismissed();
	}

	public ToastNotification Show(Snackbar snackbar)
	{
		var toastContentBuilder = new ToastContentBuilder()
			.AddText(snackbar.Text)
			.AddButton(new ToastButton() { ActivationType = ToastActivationType.Foreground }.SetContent(snackbar.ActionButtonText));
		var toastContent = toastContentBuilder.GetToastContent();
		toastContent.ActivationType = ToastActivationType.Background;
		var toast = new ToastNotification(toastContent.GetXml());
		toast.Activated += OnActivated;
		toast.Dismissed += OnDismissed; 
		toast.ExpirationTime = System.DateTime.Now.Add(snackbar.Duration);
		snackbar = snackbar;
		ToastNotificationManager.CreateToastNotifier().Show(toast);
		return toast;
	}

	void OnActivated(ToastNotification sender, object args)
	{
		if (snackbar is not null)
		{
			Microsoft.Maui.Controls.Device.BeginInvokeOnMainThread(snackbar.Action);
		}
	}

	void OnDismissed(ToastNotification sender, ToastDismissedEventArgs args)
	{
		snackbar?.OnDismissed();
	}
}
