using CommunityToolkit.WinUI.Notifications;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace CommunityToolkit.Maui.Alerts;

public partial class Snackbar
{
	TaskCompletionSource<bool>? dismissedTCS;

	/// <summary>
	/// Dismiss Snackbar
	/// </summary>
	private partial async Task DismissNative(CancellationToken token)
	{
		if (NativeSnackbar is null)
		{
			dismissedTCS = null;
			return;
		}

		token.ThrowIfCancellationRequested();
		ToastNotificationManager.History.Clear();

		NativeSnackbar.Activated -= OnActivated;
		NativeSnackbar.Dismissed -= OnDismissed;
		NativeSnackbar.ExpirationTime = DateTimeOffset.Now;

		NativeSnackbar = null;

		await (dismissedTCS?.Task ?? Task.CompletedTask);
	}

	/// <summary>
	/// Show Snackbar
	/// </summary>
	private partial async Task ShowNative(CancellationToken token)
	{
		await DismissNative(token);
		token.ThrowIfCancellationRequested();
		var toastContentBuilder = new ToastContentBuilder()
			.AddText(Text)
			.AddButton(
				new ToastButton { ActivationType = ToastActivationType.Foreground }.SetContent(ActionButtonText));

		var toastContent = toastContentBuilder.GetToastContent();
		toastContent.ActivationType = ToastActivationType.Background;

		dismissedTCS = new();

		var xmlDocument = new XmlDocument();
		xmlDocument.LoadXml(toastContent.GetContent());

		NativeSnackbar = new ToastNotification(xmlDocument);
		NativeSnackbar.Activated += OnActivated;
		NativeSnackbar.Dismissed += OnDismissed;
		NativeSnackbar.ExpirationTime = DateTimeOffset.Now.Add(Duration);

		ToastNotificationManager.CreateToastNotifier().Show(nativeSnackbar);

		OnShown();
	}

	void OnActivated(ToastNotification sender, object args)
	{
		if (NativeSnackbar is not null && Action is not null)
		{
			MainThread.BeginInvokeOnMainThread(Action);
		}
	}

	void OnDismissed(ToastNotification sender, ToastDismissedEventArgs args)
	{
		dismissedTCS?.TrySetResult(true);
		MainThread.BeginInvokeOnMainThread(OnDismissed);
	}
}
