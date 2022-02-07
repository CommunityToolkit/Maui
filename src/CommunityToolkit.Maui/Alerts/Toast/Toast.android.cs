using Android.Widget;
using Android.Text;
using Android.Text.Style;
using AndroidToast = Android.Widget.Toast;

namespace CommunityToolkit.Maui.Alerts;

public partial class Toast
{
	static AndroidToast? nativeToast;

	static AndroidToast? NativeToast
	{
		get
		{
			return MainThread.IsMainThread
				? nativeToast
				: throw new InvalidOperationException($"{nameof(nativeToast)} can only be called from the Main Thread");
		}
		set
		{
			if (!MainThread.IsMainThread)
			{
				throw new InvalidOperationException($"{nameof(nativeToast)} can only be called from the Main Thread");
			}

			nativeToast = value;
		}
	}

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

		NativeToast = AndroidToast.MakeText(Platform.AppContext, styledText, GetToastLength(Duration))
						  ?? throw new Exception("Unable to create toast");

		NativeToast.Show();
	}

	static ToastLength GetToastLength(Core.ToastDuration duration)
	{
		return (ToastLength)(int)duration;
	}
}