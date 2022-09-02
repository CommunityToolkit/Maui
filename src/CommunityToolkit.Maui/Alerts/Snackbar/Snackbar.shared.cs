using CommunityToolkit.Maui.Core;

#if ANDROID
using PlatformSnackbar = Google.Android.Material.Snackbar.Snackbar;
#elif IOS || MACCATALYST
using PlatformSnackbar = CommunityToolkit.Maui.Core.Views.PlatformSnackbar;
#elif WINDOWS
using PlatformSnackbar = Windows.UI.Notifications.ToastNotification;
#endif

namespace CommunityToolkit.Maui.Alerts;

/// <inheritdoc/>
public partial class Snackbar : ISnackbar
{
	static readonly WeakEventManager weakEventManager = new();

	bool isDisposed;
	string text = string.Empty;
	string actionButtonText = AlertDefaults.ActionButtonText;

	/// <summary>
	/// Initializes a new instance of <see cref="Snackbar"/>
	/// </summary>
	public Snackbar()
	{
		Duration = GetDefaultTimeSpan();
		VisualOptions = new SnackbarOptions();
	}

	/// <inheritdoc/>
	public static bool IsShown { get; private set; }

	/// <inheritdoc/>
	public string Text
	{
		get => text;
		init => text = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <inheritdoc/>
	public string ActionButtonText
	{
		get => actionButtonText;
		init => actionButtonText = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <inheritdoc/>
	public SnackbarOptions VisualOptions { get; init; }

	/// <inheritdoc/>
	public TimeSpan Duration { get; init; }

	/// <inheritdoc/>
	public Action? Action { get; init; }

	/// <inheritdoc/>
	public IView? Anchor { get; init; }

	/// <inheritdoc/>
	public static event EventHandler Shown
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}

	/// <inheritdoc/>
	public static event EventHandler Dismissed
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Create new Snackbar
	/// </summary>
	/// <param name="message">Snackbar message</param>
	/// <param name="actionButtonText">Snackbar action button text</param>
	/// <param name="duration">Snackbar duration</param>
	/// <param name="action">Snackbar action</param>
	/// <param name="visualOptions">Snackbar visual options</param>
	/// <param name="anchor">Snackbar anchor</param>
	/// <returns>New instance of Snackbar</returns>
	public static ISnackbar Make(
		string message,
		Action? action = null,
		string actionButtonText = AlertDefaults.ActionButtonText,
		TimeSpan? duration = null,
		SnackbarOptions? visualOptions = null,
		IView? anchor = null)
	{
		return new Snackbar
		{
			Text = message,
			Action = action,
			ActionButtonText = actionButtonText,
			Duration = duration ?? GetDefaultTimeSpan(),
			VisualOptions = visualOptions ?? new(),
			Anchor = anchor
		};
	}

#if ANDROID || IOS || MACCATALYST || WINDOWS
	static PlatformSnackbar? PlatformSnackbar { get; set; }
#endif

	/// <summary>
	/// Dispose Snackbar
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Show Snackbar
	/// </summary>
	public virtual Task Show(CancellationToken token = default) => ShowPlatform(token);

	/// <summary>
	/// Dismiss Snackbar
	/// </summary>
	public virtual Task Dismiss(CancellationToken token = default) => DismissPlatform(token);

	internal static TimeSpan GetDefaultTimeSpan() => TimeSpan.FromSeconds(3);

	/// <summary>
	/// Dispose Snackbar
	/// </summary>
	protected virtual void Dispose(bool isDisposing)
	{
		if (isDisposed)
		{
			return;
		}

		if (isDisposing)
		{
#if ANDROID || IOS || MACCATALYST
			PlatformSnackbar?.Dispose();
#endif
		}

		isDisposed = true;
	}

#if !(IOS || ANDROID || MACCATALYST || WINDOWS)
	/// <inheritdoc/>
	private partial Task ShowPlatform(CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		OnShown();

		return Task.CompletedTask;
	}

	/// <inheritdoc/>
	private partial Task DismissPlatform(CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		OnDismissed();

		return Task.CompletedTask;
	}
#endif

	void OnShown()
	{
		IsShown = true;
		weakEventManager.HandleEvent(this, EventArgs.Empty, nameof(Shown));
	}

	void OnDismissed()
	{
		IsShown = false;
		weakEventManager.HandleEvent(this, EventArgs.Empty, nameof(Dismissed));
	}

	private partial Task ShowPlatform(CancellationToken token);

#if IOS || MACCATALYST
	private static partial Task DismissPlatform(CancellationToken token);
#else
	private partial Task DismissPlatform(CancellationToken token);
#endif
}

/// <summary>
/// Extension methods for <see cref="VisualElement"/>.
/// </summary>
public static class SnackbarVisualElementExtension
{
	/// <summary>
	/// Display snackbar anchored to <see cref="VisualElement"/>
	/// </summary>
	/// <param name="visualElement">Anchor element</param>
	/// <param name="message">Text of the snackbar</param>
	/// <param name="actionButtonText">Text of the snackbar button</param>
	/// <param name="action">Action of the snackbar button</param>
	/// <param name="duration">Snackbar duration</param>
	/// <param name="visualOptions">Snackbar visual options</param>
	/// <param name="token">Cancellation token</param>
	public static Task DisplaySnackbar(
		this VisualElement? visualElement,
		string message,
		Action? action = null,
		string actionButtonText = AlertDefaults.ActionButtonText,
		TimeSpan? duration = null,
		SnackbarOptions? visualOptions = null,
		CancellationToken token = default) => Snackbar.Make(message, action, actionButtonText, duration, visualOptions, visualElement).Show(token);
}