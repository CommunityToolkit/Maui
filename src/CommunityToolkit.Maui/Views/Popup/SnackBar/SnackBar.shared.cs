using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Views.Popup.SnackBar;
using Microsoft.Maui;

namespace CommunityToolkit.Maui.Views.Popup.SnackBar;

public class Snackbar : ISnackbar
{
#if NET6_0_ANDROID
	Google.Android.Material.Snackbar.Snackbar? nativeSnackbar;
#elif NET6_0_IOS || NET6_0_MACCATALYST
	NativeSnackBar? nativeSnackbar;
#endif

	bool isShown;

	public Snackbar()
	{
		Text = string.Empty;
		Duration = TimeSpan.FromMilliseconds(3000);
		Action = () => { };
		ActionButtonText = "OK";
		VisualOptions = new SnackbarOptions();
	}

	public SnackbarOptions VisualOptions { get; set; }

	public string Text { get; set; }

	public TimeSpan Duration { get; set; }

	public Action Action { get; set; }

	public string ActionButtonText { get; set; }

	public bool IsShown
	{
		get => isShown;
		internal set
		{
			isShown = value;
			Shown?.Invoke(this, new ShownEventArgs(isShown));
		}
	}

	public IView? Anchor { get; set; }

	public event EventHandler<ShownEventArgs>? Shown;
	public event EventHandler? Dismissed;

	public static Snackbar Make(string text, TimeSpan? duration, Action action, IView? anchor = null)
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

	public Task Show()
	{
#if NET6_0_ANDROID || NET6_0_IOS || NET6_0_MACCATALYST
		nativeSnackbar = CommunityToolkit.Maui.Views.Popup.SnackBar.Platforms.PlatformPopupExtensions.Show(this);
#else
		throw new PlatformNotSupportedException();
#endif
		IsShown = true;
		return Task.CompletedTask;
	}

	public Task Dismiss()
	{
#if NET6_0_ANDROID || NET6_0_IOS || NET6_0_MACCATALYST
		CommunityToolkit.Maui.Views.Popup.SnackBar.Platforms.PlatformPopupExtensions.Dismiss(nativeSnackbar);
#else
		throw new PlatformNotSupportedException();
#endif

		return Task.CompletedTask;
	}

	internal void OnDismissed()
	{
		IsShown = false;
		Dismissed?.Invoke(this, EventArgs.Empty);
	}
}