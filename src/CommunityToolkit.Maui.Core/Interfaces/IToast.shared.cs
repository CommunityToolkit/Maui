namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Toast
/// </summary>
public interface IToast : IAlert
{
	/// <summary>
	/// Toast duration
	/// </summary>
	ToastDuration Duration { get; }

	/// <summary>
	/// Toast font size
	/// </summary>
	double TextSize { get; }
}

/// <summary>
/// Toast duration
/// </summary>
public enum ToastDuration
{
	/// <summary>
	/// Displays Toast for a short time
	/// </summary>
	Short,

	/// <summary>
	/// Displays Toast for a long time
	/// </summary>
	Long
}