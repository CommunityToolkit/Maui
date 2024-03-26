using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace CommunityToolkit.Maui.Alerts;

public partial class Snackbar
{
	const string snackbarIdentifierArgumentKey = "snackbarIdentifier";

	static AppNotification? PlatformSnackbar { get; set; }

	static Dictionary<string, Action?> actions = new();

	TaskCompletionSource<bool>? dismissedTCS;

	internal static void HandleSnackbarAction(AppNotificationActivatedEventArgs args)
	{
		if (args.Arguments.TryGetValue(snackbarIdentifierArgumentKey, out var id) && actions.TryGetValue(id, out var action) && action is not null)
		{
			Dispatcher.GetForCurrentThread().DispatchIfRequired(action);
		}
	}

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
		await AppNotificationManager.Default.RemoveAllAsync();
		actions.Clear();

		// Verify PlatformToast is not null again after `await`
		if (PlatformSnackbar is not null)
		{
			PlatformSnackbar.Expiration = DateTimeOffset.Now;
			PlatformSnackbar = null;
		}

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
		var id = Guid.NewGuid().ToString();
		PlatformSnackbar = new AppNotificationBuilder()
			.AddText(Text)
			.AddButton(new AppNotificationButton(ActionButtonText)
				.AddArgument(snackbarIdentifierArgumentKey, id))
			.BuildNotification();
		PlatformSnackbar.Expiration = DateTimeOffset.Now.Add(Duration);

		AppNotificationManager.Default.Show(PlatformSnackbar);

		actions.Add(id, Action);

		OnShown();
	}
}