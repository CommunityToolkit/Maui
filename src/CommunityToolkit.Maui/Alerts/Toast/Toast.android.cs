using Android.Text;
using Android.Text.Style;
using Android.Widget;

namespace CommunityToolkit.Maui.Alerts;

public partial class Toast
{
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

		if (isDisposing)
		{
			PlatformToast?.Dispose();
		}

		isDisposed = true;
	}

	static void DismissPlatform(CancellationToken token)
	{
		if (PlatformToast is not null)
		{
			token.ThrowIfCancellationRequested();

			PlatformToast.Cancel();
		}
	}

	void ShowPlatform(CancellationToken token)
	{
		DismissPlatform(token);

		token.ThrowIfCancellationRequested();

		var styledText = new SpannableStringBuilder(Text);
		styledText.SetSpan(new AbsoluteSizeSpan((int)TextSize, true), 0, Text.Length, 0);

		PlatformToast = Android.Widget.Toast.MakeText(Platform.CurrentActivity?.Window?.DecorView.FindViewById(Android.Resource.Id.Content)?.RootView?.Context, styledText, GetToastLength(Duration))
						  ?? throw new Exception("Unable to create toast");

		PlatformToast.Show();
	}

	static ToastLength GetToastLength(Core.ToastDuration duration) => (ToastLength)(int)duration;
}