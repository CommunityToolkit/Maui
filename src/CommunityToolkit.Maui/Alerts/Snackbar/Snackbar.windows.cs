using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace CommunityToolkit.Maui.Alerts;

public partial class Snackbar
{
	const string snackbarIdentifierArgumentKey = "snackbarIdentifier";

	// Task.Delay waits at most uint.MaxValue - 1 milliseconds (about 49.7 days)
	static readonly TimeSpan maximumDismissDelay = TimeSpan.FromMilliseconds(uint.MaxValue - 1);

	// Show and Dismiss run on the thread that calls them, a press of the notification's button arrives on a background thread: the state
	// of the shown snackbar (PlatformSnackbar, platformSnackbarIdentifier, onPlatformSnackbarDismissed and actions) changes under this lock
	static readonly Lock platformSnackbarLock = new();

	static Dictionary<string, Action?> actions = [];

	static string? platformSnackbarIdentifier;

	static Action? onPlatformSnackbarDismissed;

	static AppNotification? PlatformSnackbar { get; set; }

	internal static void HandleSnackbarAction(AppNotificationActivatedEventArgs args) => HandleSnackbarAction(args.Arguments);

	// Windows removes the notification and raises NotificationInvoked on a background thread, so the action runs and the snackbar is dismissed
	// on the UI thread, as when the action of a snackbar is clicked on the other platforms.
	internal static void HandleSnackbarAction(IDictionary<string, string> arguments)
	{
		if (!arguments.TryGetValue(snackbarIdentifierArgumentKey, out var id))
		{
			return;
		}

		Action? action;
		lock (platformSnackbarLock)
		{
			if (!actions.Remove(id, out action))
			{
				return;
			}
		}

		DispatchToTheUiThread(() =>
		{
			action?.Invoke();
			OnPlatformSnackbarDismissed(id);
		});
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
		token.ThrowIfCancellationRequested();

		if (PlatformSnackbar is null)
		{
			return;
		}

		await AppNotificationManager.Default.RemoveAllAsync();

		OnPlatformSnackbarRemoved();
	}

	/// <summary>
	/// Show Snackbar
	/// </summary>
	async Task ShowPlatform(CancellationToken token)
	{
		await DismissPlatform(token);
		token.ThrowIfCancellationRequested();

		var id = Guid.NewGuid().ToString();
		var platformSnackbar = new AppNotificationBuilder()
			.AddText(Text)
			.AddButton(new AppNotificationButton(ActionButtonText)
				.AddArgument(snackbarIdentifierArgumentKey, id))
			.BuildNotification();
		platformSnackbar.Expiration = DateTimeOffset.Now.Add(Duration);

		AppNotificationManager.Default.Show(platformSnackbar);

		var dismissAfterDurationTokenSource = new CancellationTokenSource();
		var dismissAfterDurationToken = dismissAfterDurationTokenSource.Token;
		SetPlatformSnackbar(platformSnackbar, id, Action, () =>
		{
			dismissAfterDurationTokenSource.Cancel();
			dismissAfterDurationTokenSource.Dispose();
			OnDismissed();
		});

		OnShown();

		// A Shown handler may already have dismissed the snackbar, which cancelled the token
		_ = DismissAfterDurationAsync(platformSnackbar, id, Duration, dismissAfterDurationToken);
	}

	// Windows removes a notification once it expires without telling the app, so the toolkit dismisses the snackbar itself at the end of its
	// Duration, as the other platforms do.
	static async Task DismissAfterDurationAsync(AppNotification platformSnackbar, string id, TimeSpan duration, CancellationToken token)
	{
		// A Duration of zero or less dismisses the snackbar right after it is shown; one longer than Task.Delay can wait leaves the
		// notification to expire on its own
		var delay = duration > maximumDismissDelay ? Timeout.InfiniteTimeSpan : TimeSpan.FromTicks(Math.Max(duration.Ticks, TimeSpan.TicksPerMillisecond));
		await Task.Delay(delay, token);

		DispatchToTheUiThread(() =>
		{
			// Dismiss, Show or a press of the button may have dismissed the snackbar in the meantime
			if (OnPlatformSnackbarDismissed(id))
			{
				// Best effort: Windows also removes the expired notification by itself
				_ = AppNotificationManager.Default.RemoveByIdAsync(platformSnackbar.Id);
			}
		});
	}

	// NotificationInvoked, and the end of the Duration of a snackbar that was shown off the UI thread, arrive on background threads. The
	// action is queued even on the UI thread: an exception of the Snackbar action or of a Dismissed handler then surfaces on the UI thread,
	// as on the other platforms, instead of ending in the task of DismissAfterDurationAsync that nothing awaits.
	static void DispatchToTheUiThread(Action action)
	{
		if (Application.Current?.Dispatcher is IDispatcher dispatcher)
		{
			// Dispatch only fails once the app shuts down, when nothing is left to dismiss
			_ = dispatcher.Dispatch(action);
		}
		else
		{
			action();
		}
	}

	static void SetPlatformSnackbar(AppNotification platformSnackbar, string id, Action? action, Action onDismissed)
	{
		lock (platformSnackbarLock)
		{
			PlatformSnackbar = platformSnackbar;
			platformSnackbarIdentifier = id;
			actions.Add(id, action);
			onPlatformSnackbarDismissed = onDismissed;
		}
	}

	// Dismiss: RemoveAllAsync removed every notification of the app. Another dismissal may have dismissed the snackbar while RemoveAllAsync
	// ran: only the first one forgets it and raises Dismissed.
	static void OnPlatformSnackbarRemoved()
	{
		AppNotification platformSnackbar;
		Action? onDismissed;

		lock (platformSnackbarLock)
		{
			actions.Clear();

			if (PlatformSnackbar is null)
			{
				return;
			}

			platformSnackbar = PlatformSnackbar;
			onDismissed = ForgetPlatformSnackbar();
		}

		platformSnackbar.Expiration = DateTimeOffset.Now;
		onDismissed?.Invoke();
	}

	// Dismiss, Show, the end of the Duration and a press of the button can each dismiss the same snackbar: only the first one raises Dismissed.
	static bool OnPlatformSnackbarDismissed(string id)
	{
		Action? onDismissed;

		lock (platformSnackbarLock)
		{
			if (PlatformSnackbar is null || platformSnackbarIdentifier != id)
			{
				return false;
			}

			onDismissed = ForgetPlatformSnackbar();
		}

		onDismissed?.Invoke();
		return true;
	}

	// Called under platformSnackbarLock: returns what raises Dismissed for the forgotten snackbar, which the caller invokes once it released
	// the lock
	static Action? ForgetPlatformSnackbar()
	{
		if (platformSnackbarIdentifier is not null)
		{
			actions.Remove(platformSnackbarIdentifier);
		}

		var onDismissed = onPlatformSnackbarDismissed;
		PlatformSnackbar = null;
		platformSnackbarIdentifier = null;
		onPlatformSnackbarDismissed = null;

		return onDismissed;
	}
}