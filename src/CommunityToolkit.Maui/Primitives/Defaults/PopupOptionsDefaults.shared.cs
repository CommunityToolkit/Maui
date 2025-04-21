using System.ComponentModel;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Shapes;

namespace CommunityToolkit.Maui;

/// <summary>
/// Default Values for <see cref="PopupOptions"/>
/// </summary>
static class PopupOptionsDefaults
{
	/// <summary>
	/// Default value for <see cref="PopupOptions.Shape"/> <see cref="Shape.StrokeThickness"/>
	/// </summary>
	public const double BorderStrokeThickness = 2;

	/// <summary>
	/// Default value for <see cref="PopupOptions.CanBeDismissedByTappingOutsideOfPopup"/>
	/// </summary>
	public const bool CanBeDismissedByTappingOutsideOfPopup = true;

	/// <summary>
	/// Default value for <see cref="PopupOptions.OnTappingOutsideOfPopup"/> 
	/// </summary>
	public static Action? OnTappingOutsideOfPopup { get; } = null;

	/// <summary>
	/// Default value for <see cref="PopupOptions.PageOverlayColor"/> 
	/// </summary>
	public static Color PageOverlayColor { get; } = Colors.Black.WithAlpha(0.3f);

	/// <summary>
	/// Default value for <see cref="PopupOptions.Shape"/> <see cref="Shape.Stroke"/>
	/// </summary>
	public static Brush? BorderStroke { get; } = Colors.White;

	/// <summary>
	/// Default value for <see cref="PopupOptions.Shape"/> 
	/// </summary>
	public static IShape? Shape { get; } = new RoundRectangle
	{
		CornerRadius = new CornerRadius(20, 20, 20, 20),
		StrokeThickness = BorderStrokeThickness,
		Stroke = BorderStroke
	};

	/// <summary>
	/// Default value for <see cref="PopupOptions.Shadow"/> 
	/// </summary>
	public static Shadow Shadow { get; } = new Shadow
	{
		Brush = Colors.Black,
		Offset = new(20, 20),
		Radius = 40,
		Opacity = 0.8f
	};
}