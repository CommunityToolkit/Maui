using System.ComponentModel;
using Microsoft.Maui.Primitives;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Default Values for PopupOptions
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public class PopupOptionsDefaults
{
	/// <summary>
	/// Default value for <see cref="CanBeDismissedByTappingOutsideOfPopup"/> in PopupOptions/>
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const bool CanBeDismissedByTappingOutsideOfPopup = true;

	/// <summary>
	/// Default value for <see cref="OnTappingOutsideOfPopup"/> in PopupOptions/>
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Action? OnTappingOutsideOfPopup { get; } = null;

	/// <summary>
	/// Default value for <see cref="BackgroundColor"/> in PopupOptions/>
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Color BackgroundColor { get; } = Color.FromRgba(0, 0, 0, 0.4);

	/// <summary>
	/// Default value for <see cref="Shape"/> in PopupOptions/>
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static IShape? Shape { get; } = null;

	/// <summary>
	/// Default value for <see cref="Margin"/> in PopupOptions/>
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Thickness Margin { get; } = new Thickness(30);

	/// <summary>
	/// Default value for <see cref="Padding"/> in PopupOptions/>
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Thickness Padding { get; } = new Thickness(15);

	/// <summary>
	/// Default value for <see cref="VerticalOptions"/> in PopupOptions/>
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static LayoutAlignment VerticalOptions { get; } = LayoutAlignment.Center;

	/// <summary>
	/// Default value for <see cref="HorizontalOptions"/> in PopupOptions/>
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static LayoutAlignment HorizontalOptions { get; } = LayoutAlignment.Center;
}