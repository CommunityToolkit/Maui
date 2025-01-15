namespace CommunityToolkit.Maui.Core;

/// <summary>
/// 
/// </summary>
public class PopupOptions<T>
{
	/// <summary>
	/// 
	/// </summary>
	public bool CanBeDismissedByTappingOutsideOfPopup { get; init; }

	/// <summary>
	/// 
	/// </summary>
	public Color? BackgroundColor { get; init; }
	
	/// <summary>
	/// 
	/// </summary>
	public Action<T>? OnOpened { get; set; }

	/// <summary>
	/// 
	/// </summary>
	public Action<T>? OnClosed { get; set; }

	/// <summary>
	/// 
	/// </summary>
	public Action? OnTappingOutsideOfPopup { get; set; }
}