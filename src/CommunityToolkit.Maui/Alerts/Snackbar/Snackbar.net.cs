namespace CommunityToolkit.Maui.Alerts;

public partial class Snackbar
{
	/// <summary>
	/// Dispose Snackbar
	/// </summary>
	protected virtual void Dispose(bool isDisposing)
	{
		if (isDisposed)
		{
			return;
		}

		isDisposed = true;
	}

	/// <inheritdoc/>
	Task ShowPlatform(CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		OnShown();

		return Task.CompletedTask;
	}

	/// <inheritdoc/>
	Task DismissPlatform(CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		OnDismissed();

		return Task.CompletedTask;
	}
}