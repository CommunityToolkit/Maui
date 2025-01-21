namespace CommunityToolkit.Maui.Core;

/// <summary>
/// 
/// </summary>
public class PopupOptions
{

	/// <summary>
	/// Gets or sets a value indicating whether the popup can be dismissed by tapping outside the Popup.
	/// </summary>
	/// <remarks>
	/// When true and the user taps outside the popup, it will dismiss.
	/// On Android - when false the hardware back button is disabled.
	/// </remarks>
	public bool CanBeDismissedByTappingOutsideOfPopup { get; init; } = true;

	/// <summary>
	/// 
	/// </summary>
	public Color BackgroundColor { get; init; } = Color.FromRgba(0, 0, 0, 0.4);

	/// <summary>
	/// 
	/// </summary>
	public Action? OnTappingOutsideOfPopup { get; init; }

	/// <summary>
	/// 
	/// </summary>
	public IShape? Shape { get; init; }

	/// <summary>
	/// 
	/// </summary>
	public Thickness Margin { get; init; } = 30;

	/// <summary>
	/// 
	/// </summary>
	public Thickness Padding { get; init; } = 15;

	/// <summary>
	/// 
	/// </summary>
	public LayoutOptions VerticalOptions { get; init; } = LayoutOptions.Center;
	/// <summary>
	/// 
	/// </summary>
	public LayoutOptions HorizontalOptions { get; init; } = LayoutOptions.Center;
}