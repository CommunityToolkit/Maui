using Android.Text;
using Android.Text.Style;
using Android.Widget;

namespace CommunityToolkit.Maui.Alerts;

public partial class Toast
{
	private partial void DismissNative(CancellationToken token)
	{
		if (NativeToast is not null)
		{
			token.ThrowIfCancellationRequested();

			NativeToast.Cancel();
		}
	}

	private partial void ShowNative(CancellationToken token)
	{
		DismissNative(token);

		token.ThrowIfCancellationRequested();

		var styledText = new SpannableStringBuilder(Text);
		styledText.SetSpan(new AbsoluteSizeSpan((int)TextSize, true), 0, Text.Length, 0);

		NativeToast = Android.Widget.Toast.MakeText(Platform.AppContext, styledText, GetToastLength(Duration))
						  ?? throw new Exception("Unable to create toast");

		NativeToast.Show();
	}

	static ToastLength GetToastLength(Core.ToastDuration duration)
	{
		return (ToastLength)(int)duration;
	}
}