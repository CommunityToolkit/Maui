using Microsoft.Maui.Controls.Shapes;

namespace CommunityToolkit.Maui;

/// <summary>
/// Sets global app defaults for <see cref="PopupOptions"/>
/// </summary>
/// <remarks>Set in .UseMauiCommunityToolkit(Options)</remarks>
public record DefaultPopupOptionsSettings : IPopupOptions
{
	/// <inheritdoc cref="IPopupOptions.CanBeDismissedByTappingOutsideOfPopup"/>
	public bool CanBeDismissedByTappingOutsideOfPopup { get; init; } = PopupOptionsDefaults.CanBeDismissedByTappingOutsideOfPopup;

	/// <inheritdoc cref="IPopupOptions.CanBeDismissedByBackButtonPressed"/>
	public bool CanBeDismissedByBackButtonPressed { get; init; } = PopupOptionsDefaults.CanBeDismissedByBackButtonPressed;

	/// <inheritdoc cref="IPopupOptions.OnTappingOutsideOfPopup"/>
	public Action? OnTappingOutsideOfPopup { get; init; } = PopupOptionsDefaults.OnTappingOutsideOfPopup;

	/// <inheritdoc cref="IPopupOptions.OnBackButtonPressed"/>
	public Action? OnBackButtonPressed { get; init; } = PopupOptionsDefaults.OnBackButtonPressed;

	/// <inheritdoc cref="IPopupOptions.OnDismiss"/>
	public Action? OnDismiss { get; init; } = PopupOptionsDefaults.OnDismiss;

	/// <inheritdoc cref="IPopupOptions.PageOverlayColor"/>
	public Color PageOverlayColor { get; init; } = PopupOptionsDefaults.PageOverlayColor;

	/// <inheritdoc cref="IPopupOptions.Shape"/>
	public Shape? Shape { get; init; } = PopupOptionsDefaults.Shape;

	/// <inheritdoc cref="IPopupOptions.Shadow"/>
	public Shadow? Shadow { get; init; } = PopupOptionsDefaults.Shadow;

	/// <summary>
	/// Default Values for <see cref="PopupOptions"/>
	/// </summary>
	static class PopupOptionsDefaults
	{
		/// <summary>
		/// Default value for <see cref="PopupOptions.Shape"/> <see cref="Microsoft.Maui.Controls.Shapes.Shape.StrokeThickness"/>
		/// </summary>
		public const double BorderStrokeThickness = 2;

		/// <summary>
		/// Default value for <see cref="PopupOptions.CanBeDismissedByTappingOutsideOfPopup"/>
		/// </summary>
		public const bool CanBeDismissedByTappingOutsideOfPopup = true;

		/// <summary>
		/// Default value for <see cref="PopupOptions.CanBeDismissedByBackButtonPressed"/>
		/// </summary>
		public const bool CanBeDismissedByBackButtonPressed = true;

		/// <summary>
		/// Default value for <see cref="PopupOptions.OnTappingOutsideOfPopup"/> 
		/// </summary>
		public static Action? OnTappingOutsideOfPopup { get; } = null;

		/// <summary>
		/// Default value for <see cref="PopupOptions.OnBackButtonPressed"/> 
		/// </summary>
		public static Action? OnBackButtonPressed { get; } = null;

		/// <summary>
		/// Default value for <see cref="PopupOptions.OnDismiss"/> 
		/// </summary>
		public static Action? OnDismiss { get; } = null;

		/// <summary>
		/// Default value for <see cref="PopupOptions.PageOverlayColor"/> 
		/// </summary>
		public static Color PageOverlayColor { get; } = Colors.Black.WithAlpha(0.3f);

		/// <summary>
		/// Default value for <see cref="PopupOptions.Shape"/> <see cref="Shape.Stroke"/>
		/// </summary>
		public static Brush? BorderStroke { get; } = Colors.LightGray;

		/// <summary>
		/// Default value for <see cref="PopupOptions.Shape"/> 
		/// </summary>
		public static Shape? Shape { get; } = new RoundRectangle
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
}