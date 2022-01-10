using Android.Widget;
using AndroidToast = Android.Widget.Toast;

namespace CommunityToolkit.Maui.Alerts;

public partial class Toast
{
	static AndroidToast? nativeToast;

	/// <summary>
	/// Dismiss Toast
	/// </summary>
	public virtual partial Task Dismiss(CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		nativeToast?.Cancel();
		return Task.CompletedTask;
	}

	/// <summary>
	/// Show Toast
	/// </summary>
	public virtual async partial Task Show(CancellationToken token)
	{
		await Dismiss(token);
		token.ThrowIfCancellationRequested();

		nativeToast = AndroidToast.MakeText(Platform.AppContext, Text, (ToastLength)(int)Duration)
						  ?? throw new Exception("Unable to create toast");
		nativeToast.Show();
	}
}