using Microsoft.Maui.Dispatching;
using Windows.UI.Notifications;
using static CommunityToolkit.Maui.Extensions.ToastNotificationExtensions;

namespace CommunityToolkit.Maui.Alerts;

public partial class Snackbar
{
	static Windows.UI.Notifications.ToastNotification? PlatformSnackbar { get; set; }

	TaskCompletionSource<bool>? dismissedTCS;

	/// <summary>
	/// Dispose Snackbar
	/// </summary>
	protected virtual void Dispose(bool isDisposing)
	{
		if (isDisposed)
		{
			return;
		}

		isDisposed = true;
	}

	/// <summary>
	/// Dismiss Snackbar
	/// </summary>
	async Task DismissPlatform(CancellationToken token)
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
	async Task ShowPlatform(CancellationToken token)
	{
		await DismissPlatform(token);
		token.ThrowIfCancellationRequested();

		dismissedTCS = new();

		PlatformSnackbar = new ToastNotification(BuildToastNotificationContent(Text, ActionButtonText));
		PlatformSnackbar.Activated += OnActivated;
		PlatformSnackbar.Dismissed += OnDismissed;
		PlatformSnackbar.ExpirationTime = DateTimeOffset.Now.Add(Duration);

		ToastNotificationManager.CreateToastNotifier().Show(PlatformSnackbar);

		OnShown();
	}

	void OnActivated(ToastNotification sender, object args)
	{
		if (PlatformSnackbar is not null && Action is not null)
		{
			Dispatcher.GetForCurrentThread().DispatchIfRequired(Action);
		}
	}

	void OnDismissed(ToastNotification sender, ToastDismissedEventArgs args)
	{
		dismissedTCS?.TrySetResult(true);
		Dispatcher.GetForCurrentThread().DispatchIfRequired(OnDismissed);
	}
}