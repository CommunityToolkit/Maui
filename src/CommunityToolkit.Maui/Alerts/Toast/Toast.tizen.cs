namespace CommunityToolkit.Maui.Alerts;

public partial class Toast
{
	/// <summary>
	/// Dispose Toast
	/// </summary>
	protected virtual void Dispose(bool isDisposing)
	{
		if (isDisposed)
		{
			return;
		}

		isDisposed = true;
	}

	/// <summary>
	/// Show Toast
	/// </summary>
	void ShowPlatform(CancellationToken token)
	{
		DismissPlatform(token);
		token.ThrowIfCancellationRequested();

		new Tizen.Applications.ToastMessage
		{
			Message = Text
		}.Post();
	}

	/// <summary>
	/// Dismiss Toast
	/// </summary>
	static void DismissPlatform(CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
	}
}