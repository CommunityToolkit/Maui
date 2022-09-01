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
	static void ShowPlatform(CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
	}

	/// <summary>
	/// Dismiss Toast
	/// </summary>
	static void DismissPlatform(CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
	}
}