namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Alert
/// </summary>
public interface IAlert : IAsyncDisposable
{
	/// <summary>
	/// Message text
	/// </summary>
	string Text { get; }

	/// <summary>
	/// Dismiss the alert
	/// </summary>
	Task Dismiss(CancellationToken token = default);

	/// <summary>
	/// Show the alert
	/// </summary>
	Task Show(CancellationToken token = default);
}
