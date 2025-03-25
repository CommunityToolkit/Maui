using System.ComponentModel;

namespace CommunityToolkit.Maui;

/// <summary>
/// Default Values for PopupOptions
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public class PopupOptionsDefaults
{
	/// <summary>
	/// Default value for <see cref="PopupOptions.CanBeDismissedByTappingOutsideOfPopup"/>
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const bool CanBeDismissedByTappingOutsideOfPopup = true;

	/// <summary>
	/// Default value for <see cref="PopupOptions.OnTappingOutsideOfPopup"/> 
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Action? OnTappingOutsideOfPopup { get; } = null;

	/// <summary>
	/// Default value for <see cref="PopupOptions.BackgroundColor"/> 
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Color BackgroundColor { get; } = Color.FromRgba(0, 0, 0, 0.4);

	/// <summary>
	/// Default value for <see cref="PopupOptions.BorderStroke"/> 
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Brush? BorderStroke { get; } = null;

	/// <summary>
	/// Default value for <see cref="PopupOptions.Shape"/> 
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static IShape? Shape { get; } = null;

	/// <summary>
	/// Default value for <see cref="PopupOptions.Margin"/> 
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Thickness Margin { get; } = new Thickness(30);

	/// <summary>
	/// Default value for <see cref="PopupOptions.Padding"/> 
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Thickness Padding { get; } = new Thickness(15);

	/// <summary>
	/// Default value for <see cref="PopupOptions.VerticalOptions"/> 
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static LayoutOptions VerticalOptions { get; } = LayoutOptions.Center;

	/// <summary>
	/// Default value for <see cref="PopupOptions.HorizontalOptions"/> 
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static LayoutOptions HorizontalOptions { get; } = LayoutOptions.Center;
}