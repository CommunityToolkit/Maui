using System;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Views.Popup.Snackbar;

/// <inheritdoc/>
public partial class Snackbar : ISnackbar
{
	readonly static WeakEventManager _weakEventManager = new();

	/// <summary>
	/// Initializes a new instance of <see cref="Snackbar"/>
	/// </summary>
	public Snackbar()
	{
		Text = string.Empty;
		Duration = GetDefaultTimeSpan();
		ActionButtonText = "OK";
		VisualOptions = new SnackbarOptions();
	}

	/// <inheritdoc/>
	public SnackbarOptions VisualOptions { get; set; }

	/// <inheritdoc/>
	public string Text { get; set; }

	/// <inheritdoc/>
	public TimeSpan Duration { get; set; }

	/// <inheritdoc/>
	public Action? Action { get; set; }

	/// <inheritdoc/>
	public string ActionButtonText { get; set; }

	/// <inheritdoc/>
	public static bool IsShown { get; private set; }

	/// <inheritdoc/>
	public IView? Anchor { get; set; }

	/// <inheritdoc/>
	public static event EventHandler Shown
	{
		add => _weakEventManager.AddEventHandler(value);
		remove => _weakEventManager.RemoveEventHandler(value);
	}

	/// <inheritdoc/>
	public static event EventHandler Dismissed
	{
		add => _weakEventManager.AddEventHandler(value);
		remove => _weakEventManager.RemoveEventHandler(value);
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
		string actionButtonText = "OK",
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

#if NET6_0
	/// <summary>
	/// Show Snackbar
	/// </summary>
	/// <returns></returns>
	/// <exception cref="NotSupportedException"></exception>
	public virtual System.Threading.Tasks.Task Show() => throw new NotSupportedException($"{nameof(Show)} must be called on a platform");

	/// <summary>
	/// Dismiss Snackbar
	/// </summary>
	/// <returns></returns>
	/// <exception cref="NotSupportedException"></exception>
	public virtual System.Threading.Tasks.Task Dismiss() => throw new NotSupportedException($"{nameof(Dismiss)} must be called on a platform");
#endif

	/// <summary>
	/// Dispose Snackbar
	/// </summary>
	public async ValueTask DisposeAsync()
	{
		await DisposeAsyncCore();
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Dispose Snackbar
	/// </summary>
	/// <param name="isDisposing"></param>
	protected virtual async ValueTask DisposeAsyncCore()
	{
#if NET6_0_ANDROID || NET6_0_IOS || NET6_0_MACCATALYST
		await Microsoft.Maui.Controls.Device.InvokeOnMainThreadAsync(() => _nativeSnackbar?.Dispose());
#else
		await Task.CompletedTask;
#endif
	}

	static TimeSpan GetDefaultTimeSpan() => TimeSpan.FromSeconds(3);

	void OnShown()
	{
		IsShown = true;
		_weakEventManager.HandleEvent(this, EventArgs.Empty, nameof(Shown));
	}

	void OnDismissed()
	{
		IsShown = false;
		_weakEventManager.HandleEvent(this, EventArgs.Empty, nameof(Dismissed));
	}
}