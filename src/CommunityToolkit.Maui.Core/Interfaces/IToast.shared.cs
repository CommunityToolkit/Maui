namespace CommunityToolkit.Maui.Core.Interfaces;


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
