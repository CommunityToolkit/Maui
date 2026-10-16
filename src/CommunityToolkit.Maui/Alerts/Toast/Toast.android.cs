using Android.Text;
using Android.Text.Style;
using Android.Widget;

namespace CommunityToolkit.Maui.Alerts;

public partial class Toast
{
	// PlatformToast is shared by every Toast, so another instance may have replaced the toast that this instance showed
	Android.Widget.Toast? shownPlatformToast;

	static Android.Widget.Toast? PlatformToast { get; set; }

	/// <summary>
	/// Dispose Toast
	/// </summary>
	protected virtual void Dispose(bool isDisposing)
	{
		if (isDisposed)
		{
			return;
		}

		if (isDisposing && shownPlatformToast is not null)
		{
			// A later Show or Dismiss must not reach the disposed Java peer, and the toast that another instance showed must stay dismissable
			if (PlatformToast == shownPlatformToast)
			{
				PlatformToast = null;
			}

			shownPlatformToast.Dispose();
			shownPlatformToast = null;
		}

		isDisposed = true;
	}

	static void DismissPlatform(CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		if (PlatformToast is null)
		{
			return;
		}

		PlatformToast.Cancel();
	}

	static ToastLength GetToastLength(Core.ToastDuration duration) => (ToastLength)(int)duration;

	void ShowPlatform(CancellationToken token)
	{
		DismissPlatform(token);

		token.ThrowIfCancellationRequested();

		var styledText = new SpannableStringBuilder(Text);
		styledText.SetSpan(new AbsoluteSizeSpan((int)TextSize, true), 0, Text.Length, 0);

		PlatformToast = shownPlatformToast = Android.Widget.Toast.MakeText(Platform.CurrentActivity?.Window?.DecorView.FindViewById(Android.Resource.Id.Content)?.RootView?.Context, styledText, GetToastLength(Duration))
						  ?? throw new Exception("Unable to create toast");

		PlatformToast.Show();
	}
}