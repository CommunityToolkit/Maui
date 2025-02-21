namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Popup options.
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
	/// Popup background color.
	/// </summary>
	public Color BackgroundColor { get; init; } = Color.FromRgba(0, 0, 0, 0.4);

	/// <summary>
	/// Action to be executed when the user taps outside the Popup.
	/// </summary>
	public Action? OnTappingOutsideOfPopup { get; init; }

	/// <summary>
	/// Popup border.
	/// </summary>
	public IShape? Shape { get; init; }

	/// <summary>
	/// Popup margin.
	/// </summary>
	public Thickness Margin { get; init; } = 30;

	/// <summary>
	/// Popup padding.
	/// </summary>
	public Thickness Padding { get; init; } = 15;

	/// <summary>
	/// Popup vertical options.
	/// </summary>
	public LayoutOptions VerticalOptions { get; init; } = LayoutOptions.Center;

	/// <summary>
	/// Popup horizontal options.
	/// </summary>
	public LayoutOptions HorizontalOptions { get; init; } = LayoutOptions.Center;
}