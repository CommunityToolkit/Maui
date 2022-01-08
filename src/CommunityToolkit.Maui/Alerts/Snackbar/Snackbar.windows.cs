using CommunityToolkit.WinUI.Notifications;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace CommunityToolkit.Maui.Alerts;

public partial class Snackbar
{
	static readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

	static ToastNotification? _nativeSnackbar;
	TaskCompletionSource<bool>? _dismissedTCS;

	/// <summary>
	/// Dismiss Snackbar
	/// </summary>
	public virtual async partial Task Dismiss(CancellationToken token)
	{
		if (_nativeSnackbar is null)
		{
			_dismissedTCS = null;
			return;
		}

		await _semaphoreSlim.WaitAsync(token);

		try
		{
			ToastNotificationManager.History.Clear();

			_nativeSnackbar.Activated -= OnActivated;
			_nativeSnackbar.Dismissed -= OnDismissed;
			_nativeSnackbar.ExpirationTime = DateTimeOffset.Now;

			_nativeSnackbar = null;

			await (_dismissedTCS?.Task ?? Task.CompletedTask);
		}
		finally
		{
			_semaphoreSlim.Release();
		}
	}

	/// <summary>
	/// Show Snackbar
	/// </summary>
	public virtual async partial Task Show(CancellationToken token)
	{
		await Dismiss(token);

		var toastContentBuilder = new ToastContentBuilder()
										.AddText(Text)
										.AddButton(new ToastButton { ActivationType = ToastActivationType.Foreground }.SetContent(ActionButtonText));

		var toastContent = toastContentBuilder.GetToastContent();
		toastContent.ActivationType = ToastActivationType.Background;

		_dismissedTCS = new();

		var xmlDocument = new XmlDocument();
		xmlDocument.LoadXml(toastContent.GetContent());

		_nativeSnackbar = new ToastNotification(xmlDocument);
		_nativeSnackbar.Activated += OnActivated;
		_nativeSnackbar.Dismissed += OnDismissed;
		_nativeSnackbar.ExpirationTime = DateTimeOffset.Now.Add(Duration);

		ToastNotificationManager.CreateToastNotifier().Show(_nativeSnackbar);

		OnShown();
	}

	void OnActivated(ToastNotification sender, object args)
	{
		if (_nativeSnackbar is not null && Action is not null)
			Device.BeginInvokeOnMainThread(Action);
	}

	void OnDismissed(ToastNotification sender, ToastDismissedEventArgs args)
	{
		_dismissedTCS?.TrySetResult(true);
		Device.BeginInvokeOnMainThread(OnDismissed);
	}
}
