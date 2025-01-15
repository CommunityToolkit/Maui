namespace CommunityToolkit.Maui.Core;

/// <summary>
/// 
/// </summary>
public class PopupOptions
{
	/// <summary>
	/// 
	/// </summary>
	public bool CanBeDismissedByTappingOutsideOfPopup { get; init; } = true;

	/// <summary>
	/// 
	/// </summary>
	public Color? BackgroundColor { get; init; }
	
	/// <summary>
	/// 
	/// </summary>
	public Action? OnTappingOutsideOfPopup { get; set; }

	/// <summary>
	/// 
	/// </summary>
	public IShape? Shape { get; set; }

	/// <summary>
	/// 
	/// </summary>
	public LayoutOptions VerticalOptions { get; set; } = LayoutOptions.Center;
	/// <summary>
	/// 
	/// </summary>
	public LayoutOptions HorizontalOptions { get; set; } = LayoutOptions.Center;
}