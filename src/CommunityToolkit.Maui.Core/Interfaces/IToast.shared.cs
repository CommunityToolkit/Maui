namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Toast
/// </summary>
public interface IToast : IAsyncDisposable
{
	/// <summary>
	/// Toast duration
	/// </summary>
	ToastDuration Duration { get; }

	/// <summary>
	/// Toast message
	/// </summary>
	string Text { get; }

	/// <summary>
	/// Dismiss the toast
	/// </summary>
	Task Dismiss(CancellationToken token = default);

	/// <summary>
	/// Show the toast
	/// </summary>
	Task Show(CancellationToken token = default);
}

/// <summary>
/// Toast duration
/// </summary>
public enum ToastDuration
{
	/// <summary>
	/// Short
	/// </summary>
	Short,

	/// <summary>
	/// Long
	/// </summary>
	Long
}