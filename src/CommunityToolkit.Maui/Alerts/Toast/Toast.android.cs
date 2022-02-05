using Android.Widget;
using Android.Text;
using Android.Text.Style;
using AndroidToast = Android.Widget.Toast;

namespace CommunityToolkit.Maui.Alerts;

public partial class Toast
{
	static AndroidToast? nativeToast;
	static readonly SemaphoreSlim semaphoreSlim = new(1, 1);

	/// <summary>
	/// Dismiss Toast
	/// </summary>
	public virtual async partial Task Dismiss(CancellationToken token)
	{
		if (nativeToast is null)
		{
			return;
		}

		await semaphoreSlim.WaitAsync(token);

		try
		{
			token.ThrowIfCancellationRequested();

			nativeToast.Cancel();
		}
		finally
		{
			semaphoreSlim.Release();
		}
	}

	/// <summary>
	/// Show Toast
	/// </summary>
	public virtual async partial Task Show(CancellationToken token)
	{
		await Dismiss(token);
		token.ThrowIfCancellationRequested();

		var styledText = new SpannableStringBuilder(Text);
		styledText.SetSpan( new AbsoluteSizeSpan((int)TextSize), 0, Text.Length, 0);
		nativeToast = AndroidToast.MakeText(Platform.AppContext, styledText, GetToastLength(Duration))
						  ?? throw new Exception("Unable to create toast");
		nativeToast.Show();
	}

	static ToastLength GetToastLength(Core.ToastDuration duration)
	{
		return (ToastLength)(int)duration;
	}
}