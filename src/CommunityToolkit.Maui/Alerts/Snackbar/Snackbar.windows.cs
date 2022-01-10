using CommunityToolkit.WinUI.Notifications;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace CommunityToolkit.Maui.Alerts;

public partial class Snackbar
{
	static readonly SemaphoreSlim semaphoreSlim = new(1, 1);

	static ToastNotification? nativeSnackbar;
	TaskCompletionSource<bool>? dismissedTCS;

	/// <summary>
	/// Dismiss Snackbar
	/// </summary>
	public virtual async partial Task Dismiss(CancellationToken token)
	{
		if (nativeSnackbar is null)
		{
			dismissedTCS = null;
			return;
		}

		await semaphoreSlim.WaitAsync(token);

		try
		{
			token.ThrowIfCancellationRequested();
			ToastNotificationManager.History.Clear();

			nativeSnackbar.Activated -= OnActivated;
			nativeSnackbar.Dismissed -= OnDismissed;
			nativeSnackbar.ExpirationTime = DateTimeOffset.Now;

			nativeSnackbar = null;

			await (dismissedTCS?.Task ?? Task.CompletedTask);
		}
		finally
		{
			semaphoreSlim.Release();
		}
	}

	/// <summary>
	/// Show Snackbar
	/// </summary>
	public virtual async partial Task Show(CancellationToken token)
	{
		await Dismiss(token);
		token.ThrowIfCancellationRequested();

		var toastContentBuilder = new ToastContentBuilder()
										.AddText(Text)
										.AddButton(new ToastButton { ActivationType = ToastActivationType.Foreground }.SetContent(ActionButtonText));

		var toastContent = toastContentBuilder.GetToastContent();
		toastContent.ActivationType = ToastActivationType.Background;

		dismissedTCS = new();

		var xmlDocument = new XmlDocument();
		xmlDocument.LoadXml(toastContent.GetContent());

		nativeSnackbar = new ToastNotification(xmlDocument);
		nativeSnackbar.Activated += OnActivated;
		nativeSnackbar.Dismissed += OnDismissed;
		nativeSnackbar.ExpirationTime = DateTimeOffset.Now.Add(Duration);

		ToastNotificationManager.CreateToastNotifier().Show(nativeSnackbar);

		OnShown();
	}

	void OnActivated(ToastNotification sender, object args)
	{
		if (nativeSnackbar is not null && Action is not null)
    {
			Device.BeginInvokeOnMainThread(Action);
    }
	}

	void OnDismissed(ToastNotification sender, ToastDismissedEventArgs args)
	{
		dismissedTCS?.TrySetResult(true);
		Device.BeginInvokeOnMainThread(OnDismissed);
	}
}
