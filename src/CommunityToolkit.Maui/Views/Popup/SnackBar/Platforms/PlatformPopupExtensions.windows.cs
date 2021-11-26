using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;

namespace CommunityToolkit.Maui.Views.Popup.SnackBar.Platforms;

class PlatformPopupExtensions : IPlatformPopupExtensions
{
	private Snackbar? snackbar;
	public void Dismiss(Snackbar snackBar)
	{
		if (snackBar.nativeSnackbar is not null)
		{
			ToastNotificationManagerCompat.History.Clear();
			snackbar = null;
			snackBar.nativeSnackbar.Activated -= OnActivated;
			snackBar.nativeSnackbar.Dismissed -= OnDismissed;
			snackBar.nativeSnackbar.ExpirationTime = System.DateTimeOffset.Now;
		}

		snackBar.OnDismissed();
	}

	public ToastNotification Show(Snackbar snackBar)
	{
		var toastContentBuilder = new ToastContentBuilder()
			.AddText(snackBar.Text)
			.AddButton(new ToastButton() { ActivationType = ToastActivationType.Foreground }.SetContent(snackBar.ActionButtonText));
		var toastContent = toastContentBuilder.GetToastContent();
		toastContent.ActivationType = ToastActivationType.Background;
		var toast = new ToastNotification(toastContent.GetXml());
		toast.Activated += OnActivated;
		toast.Dismissed += OnDismissed; 
		toast.ExpirationTime = System.DateTime.Now.Add(snackBar.Duration);
		snackbar = snackBar;
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
