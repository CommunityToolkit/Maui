using System.Threading;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;

namespace CommunityToolkit.Maui.Alerts.Snackbar;

public partial class Snackbar
{
	readonly static SemaphoreSlim _semaphoreSlim = new(1, 1);

	static ToastNotification? _nativeSnackbar;
	TaskCompletionSource<bool>? _dismissedTCS;

	/// <summary>
	/// Dismiss Snackbar
	/// </summary>
	public async Task Dismiss()
	{
		if (_nativeSnackbar is null)
		{
			_dismissedTCS = null;
			return;
		}

		await _semaphoreSlim.WaitAsync();

		try
		{
			ToastNotificationManagerCompat.History.Clear();

			_nativeSnackbar.Activated -= OnActivated;
			_nativeSnackbar.Dismissed -= OnDismissed;
			_nativeSnackbar.ExpirationTime = System.DateTimeOffset.Now;

			_nativeSnackbar = null;

			await (_dismissedTCS?.Task ?? Task.CompletedTask);

			OnDismissed();
		}
		finally
		{
			_semaphoreSlim.Release();
		}
	}

	/// <summary>
	/// Show Snackbar
	/// </summary>
	public async Task Show()
	{
		await Dismiss();

		var toastContentBuilder = new ToastContentBuilder()
										.AddText(Text)
										.AddButton(new ToastButton { ActivationType = ToastActivationType.Foreground }.SetContent(ActionButtonText));

		var toastContent = toastContentBuilder.GetToastContent();
		toastContent.ActivationType = ToastActivationType.Background;

		_dismissedTCS = new();

		_nativeSnackbar = new ToastNotification(toastContent.GetXml());
		_nativeSnackbar.Activated += OnActivated;
		_nativeSnackbar.Dismissed += OnDismissed;
		_nativeSnackbar.ExpirationTime = System.DateTime.Now.Add(Duration);

		ToastNotificationManager.CreateToastNotifier().Show(_nativeSnackbar);

		OnShown();
	}

	void OnActivated(ToastNotification sender, object args)
	{
		if (_nativeSnackbar is not null && Action is not null)
			Microsoft.Maui.Controls.Device.BeginInvokeOnMainThread(Action);
	}

	void OnDismissed(ToastNotification sender, ToastDismissedEventArgs args) => _dismissedTCS?.TrySetResult(true);
}
