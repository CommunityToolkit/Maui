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
	private partial async Task DismissPlatform(CancellationToken token)
	{
		if (PlatformSnackbar is null)
		{
			dismissedTCS = null;
			return;
		}

		token.ThrowIfCancellationRequested();
		ToastNotificationManager.History.Clear();

		PlatformSnackbar.Activated -= OnActivated;
		PlatformSnackbar.Dismissed -= OnDismissed;
		PlatformSnackbar.ExpirationTime = DateTimeOffset.Now;

		PlatformSnackbar = null;

		await (dismissedTCS?.Task ?? Task.CompletedTask);
	}

	/// <summary>
	/// Show Snackbar
	/// </summary>
	private partial async Task ShowPlatform(CancellationToken token)
	{
		await DismissPlatform(token);
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

		PlatformSnackbar = new ToastNotification(xmlDocument);
		PlatformSnackbar.Activated += OnActivated;
		PlatformSnackbar.Dismissed += OnDismissed;
		PlatformSnackbar.ExpirationTime = DateTimeOffset.Now.Add(Duration);

		ToastNotificationManager.CreateToastNotifier().Show(platformSnackbar);

		OnShown();
	}

	void OnActivated(ToastNotification sender, object args)
	{
		if (PlatformSnackbar is not null && Action is not null)
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
