using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui;

/// <summary>
/// Sets global app defaults for <see cref="Popup"/>
/// </summary>
/// <remarks>Set in .UseMauiCommunityToolkit(Options)</remarks>
public record DefaultPopupSettings
{
	/// <inheritdoc cref="Views.Popup.Margin"/>
	public Thickness Margin { get; init; } = PopupDefaults.Margin;

	/// <inheritdoc cref="Views.Popup.Padding"/>
	public Thickness Padding { get; init; } = PopupDefaults.Padding;

	/// <inheritdoc cref="Views.Popup.HorizontalOptions"/>
	public LayoutOptions HorizontalOptions { get; init; } = PopupDefaults.HorizontalOptions;

	/// <inheritdoc cref="Views.Popup.VerticalOptions"/>
	public LayoutOptions VerticalOptions { get; init; } = PopupDefaults.VerticalOptions;

	/// <inheritdoc cref="Views.Popup.CanBeDismissedByTappingOutsideOfPopup"/>
	public bool CanBeDismissedByTappingOutsideOfPopup { get; init; } = PopupDefaults.CanBeDismissedByTappingOutsideOfPopup;
	
	/// <inheritdoc cref="Views.Popup.CanBeDismissedByBackButtonPressed"/>
	public bool CanBeDismissedByBackButtonPressed { get; init; } = PopupDefaults.CanBeDismissedByBackButtonPressed;

	/// <inheritdoc cref="VisualElement.BackgroundColor"/>
	public Color BackgroundColor { get; init; } = PopupDefaults.BackgroundColor;

	/// <summary>
	/// Popup's background color in dark mode.
	/// </summary>
	public Color BackgroundColorDark { get; init; } = PopupDefaults.BackgroundColorDark;

	/// <summary>
	/// Gets the optional callback invoked each time a <see cref="Popup"/> is presented — that is, when the page
	/// that hosts it is created — after the popup and its content are fully built, but before the popup is
	/// parented on that page. Use it to customize a popup per presentation (for example adjusting its
	/// <see cref="VisualElement.BackgroundColor"/>); unlike a constructor-time hook, the popup's content is
	/// available when this runs. A callback that throws prevents the popup from being presented.
	/// Defaults to <see langword="null"/>.
	/// </summary>
	public Action<Popup>? OnInitializing { get; init; } = PopupDefaults.OnInitializing;

	/// <summary>
	/// Default Values for <see cref="Popup"/>
	/// </summary>
	static class PopupDefaults
	{
		/// <summary>
		/// Default value for <see cref="Popup.CanBeDismissedByTappingOutsideOfPopup"/>
		/// </summary>
		public const bool CanBeDismissedByTappingOutsideOfPopup = true;

		/// <summary>
		/// Default value for <see cref="Popup.CanBeDismissedByBackButtonPressed"/>
		/// </summary>
		public const bool CanBeDismissedByBackButtonPressed = true;

		/// <summary>
		/// Default value for <see cref="Popup.Margin"/> 
		/// </summary>
		public static Thickness Margin { get; } = new(30);

		/// <summary>
		/// Default value for <see cref="Popup.Padding"/> 
		/// </summary>
		public static Thickness Padding { get; } = new(15);

		/// <summary>
		/// Default value for <see cref="Popup.VerticalOptions"/> 
		/// </summary>
		public static LayoutOptions VerticalOptions { get; } = LayoutOptions.Center;

		/// <summary>
		/// Default value for <see cref="Popup.HorizontalOptions"/> 
		/// </summary>
		public static LayoutOptions HorizontalOptions { get; } = LayoutOptions.Center;

		/// <summary>
		/// Default value for <see cref="VisualElement.BackgroundColor"/> BackgroundColor 
		/// </summary>
		public static Color BackgroundColor { get; } = Colors.White;

		/// <summary>
		/// Default value for Popup's background color in dark mode
		/// </summary>
		public static Color BackgroundColorDark { get; } = Colors.Black;

		/// <summary>
		/// Default value for <see cref="DefaultPopupSettings.OnInitializing"/>
		/// </summary>
		public static Action<Popup>? OnInitializing { get; } = null;

	}
}