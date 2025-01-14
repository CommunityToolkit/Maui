namespace CommunityToolkit.Maui.Core;

/// <summary>
/// 
/// </summary>
public class PopupOptions<T> : PopupOptions
{
	/// <summary>
	/// 
	/// </summary>
	public new Action<T>? OnOpened { get; init; }

	/// <summary>
	/// 
	/// </summary>
	public new Action<T>? OnClosed { get; init; }
}

/// <summary>
/// 
/// </summary>
public class PopupOptions
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
	public Action? OnOpened { get; init; }

	/// <summary>
	/// 
	/// </summary>
	public Action? OnClosed { get; init; }
}