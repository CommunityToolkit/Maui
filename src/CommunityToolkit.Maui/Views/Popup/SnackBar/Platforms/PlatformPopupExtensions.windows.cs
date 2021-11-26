using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;

namespace CommunityToolkit.Maui.Views.Popup.Snackbar.Platforms;

class PlatformPopupExtensions : IPlatformPopupExtensions
{
	Snackbar? snackbar;

	public void Dismiss(Snackbar snackbar)
	{
		if (snackbar.NativeSnackbar is not null)
		{
			ToastNotificationManagerCompat.History.Clear();
			this.snackbar = null;

			snackbar.NativeSnackbar.Activated -= OnActivated;
			snackbar.NativeSnackbar.Dismissed -= OnDismissed;
			snackbar.NativeSnackbar.ExpirationTime = System.DateTimeOffset.Now;
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
		this.snackbar = snackbar;
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
