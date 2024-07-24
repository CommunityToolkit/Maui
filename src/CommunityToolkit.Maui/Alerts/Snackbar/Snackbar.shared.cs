using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Alerts;

/// <inheritdoc/>
public partial class Snackbar : ISnackbar
{
	static readonly WeakEventManager weakEventManager = new();

	bool isDisposed;
	readonly string text = string.Empty;
	readonly string actionButtonText = AlertDefaults.ActionButtonText;

	/// <summary>
	/// Initializes a new instance of <see cref="Snackbar"/>
	/// </summary>
	public Snackbar()
	{
#if WINDOWS
		if (!Options.ShouldEnableSnackbarOnWindows)
		{
			throw new InvalidOperationException($"Additional setup is required in the Package.appxmanifest file to enable {nameof(Snackbar)} on Windows. Additonally, `{nameof(AppBuilderExtensions.UseMauiCommunityToolkit)}(options => options.{nameof(Options.SetShouldEnableSnackbarOnWindows)}({bool.TrueString.ToLower()});` must be called to enable Snackbar on Windows. See the Platform Specific Initialization section of the {nameof(Snackbar)} documentaion for more information: https://learn.microsoft.com/dotnet/communitytoolkit/maui/alerts/snackbar")
			{
				HelpLink = "https://learn.microsoft.com/dotnet/communitytoolkit/maui/alerts/snackbar"
			};
		}
#endif

		Duration = GetDefaultTimeSpan();
		VisualOptions = new SnackbarOptions();
	}

	/// <summary>
	/// Returns <see langword="true"/> if the <see cref="Snackbar"/> is currently visible.
	/// </summary>
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

	/// <summary>
	/// Occurs when <see cref="IsShown"/> changes.
	/// </summary>
	public static event EventHandler Shown
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Occurs when Snackbar is dismissed.
	/// </summary>
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