using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Views.Popup.SnackBar.Platforms;
using Microsoft.Maui;

namespace CommunityToolkit.Maui.Views.Popup.SnackBar;
#if NET6_0_ANDROID
using NativeSnackbar = Google.Android.Material.Snackbar.Snackbar;
#elif NET6_0_IOS || NET6_0_MACCATALYST
using NativeSnackbar = NativeSnackBar;
#else
using NativeSnackbar = System.Object;
#endif

/// <inheritdoc/>
public class Snackbar : ISnackbar
{
	internal NativeSnackbar? nativeSnackbar;

	static bool isShown;

	/// <summary>
	/// Initializes a new instance of <see cref="Snackbar"/>
	/// </summary>
	public Snackbar()
	{
		Text = string.Empty;
		Duration = TimeSpan.FromMilliseconds(3000);
		Action = () => { };
		ActionButtonText = "OK";
		VisualOptions = new SnackbarOptions();
#if NET6_0_ANDROID || NET6_0_IOS || NET6_0_MACCATALYST
		PlatformPopupExtensions = new PlatformPopupExtensions();
#endif
	}

	/// <inheritdoc/>
	public SnackbarOptions VisualOptions { get; set; }

	/// <inheritdoc/>
	public string Text { get; set; }

	/// <inheritdoc/>
	public TimeSpan Duration { get; set; }

	/// <inheritdoc/>
	public Action Action { get; set; }

	/// <inheritdoc/>
	public string ActionButtonText { get; set; }

	/// <inheritdoc/>
	public static bool IsShown
	{
		get => isShown;
		internal set
		{
			isShown = value;
			Shown?.Invoke(null, new ShownEventArgs(isShown));
		}
	}

	/// <inheritdoc/>
	public IView? Anchor { get; set; }

	/// <inheritdoc/>
	public static event EventHandler<ShownEventArgs>? Shown;

	/// <inheritdoc/>
	public static event EventHandler? Dismissed;

	internal IPlatformPopupExtensions? PlatformPopupExtensions { get; set; }

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
		Action action,
		string actionButtonText = "OK",
		TimeSpan? duration = null,
		SnackbarOptions? visualOptions = null, 
		IView? anchor = null)
	{
		var snackbar = new Snackbar
		{
			Text = message,
			ActionButtonText = actionButtonText,			
			Anchor = anchor,
			Action = action
		};
		if (duration is not null)
		{
			snackbar.Duration = duration.Value;
		}

		if (visualOptions is not null)
		{
			snackbar.VisualOptions = visualOptions;
		}

		return snackbar;
	}

	/// <inheritdoc/>
	public Task Show()
	{
		if (IsShown)
		{
			PlatformPopupExtensions?.Dismiss(this);
		}

		nativeSnackbar = PlatformPopupExtensions?.Show(this);
		IsShown = true;
		return Task.CompletedTask;
	}

	/// <inheritdoc/>
	public Task Dismiss()
	{
		PlatformPopupExtensions?.Dismiss(this);
		return Task.CompletedTask;
	}

	internal void OnDismissed()
	{
		IsShown = false;
		Dismissed?.Invoke(this, EventArgs.Empty);
	}

	/// <inheritdoc/>
	public void Dispose()
	{
#if NET6_0_ANDROID || NET6_0_IOS || NET6_0_MACCATALYST
		nativeSnackbar?.Dispose();
#endif
	}
}