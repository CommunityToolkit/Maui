using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Views.Popup.SnackBar;
using Microsoft.Maui;

namespace CommunityToolkit.Maui.Views.Popup.SnackBar;

/// <inheritdoc/>
public class Snackbar : ISnackbar
{
#if NET6_0_ANDROID
	Google.Android.Material.Snackbar.Snackbar? nativeSnackbar;
#elif NET6_0_IOS || NET6_0_MACCATALYST
	NativeSnackBar? nativeSnackbar;
#endif

	bool isShown;

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
	public bool IsShown
	{
		get => isShown;
		internal set
		{
			isShown = value;
			Shown?.Invoke(this, new ShownEventArgs(isShown));
		}
	}

	/// <inheritdoc/>
	public IView? Anchor { get; set; }

	/// <inheritdoc/>
	public event EventHandler<ShownEventArgs>? Shown;

	/// <inheritdoc/>
	public event EventHandler? Dismissed;

	/// <summary>
	/// Create new Snackbar
	/// </summary>
	/// <param name="text">Snackbar message</param>
	/// <param name="duration">Snackbar duration</param>
	/// <param name="action">Snackbar action</param>
	/// <param name="anchor">Snackbar anchor</param>
	/// <returns>New instance of Snackbar</returns>
	public static ISnackbar Make(string text, TimeSpan? duration, Action action, IView? anchor = null)
	{
		var snackbar = new Snackbar
		{
			Text = text,
			Anchor = anchor,
			Action = action
		};
		if (duration is not null)
		{
			snackbar.Duration = duration.Value;
		}

		return snackbar;
	}

	/// <inheritdoc/>
	public Task Show()
	{
#if NET6_0_ANDROID || NET6_0_IOS || NET6_0_MACCATALYST
		nativeSnackbar = CommunityToolkit.Maui.Views.Popup.SnackBar.Platforms.PlatformPopupExtensions.Show(this);
		IsShown = true;
		return Task.CompletedTask;
#else
		throw new PlatformNotSupportedException();
#endif
	}

	/// <inheritdoc/>
	public Task Dismiss()
	{
#if NET6_0_ANDROID || NET6_0_IOS || NET6_0_MACCATALYST
		CommunityToolkit.Maui.Views.Popup.SnackBar.Platforms.PlatformPopupExtensions.Dismiss(nativeSnackbar);
		return Task.CompletedTask;
#else
		throw new PlatformNotSupportedException();
#endif
	}

	internal void OnDismissed()
	{
		IsShown = false;
		Dismissed?.Invoke(this, EventArgs.Empty);
	}
}