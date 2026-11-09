using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui;

/// <summary>
/// Sets global app defaults for <see cref="Popup"/>
/// </summary>
/// <remarks>
/// <para>Set in .UseMauiCommunityToolkit(Options).</para>
/// <para>
/// <see cref="Margin"/>, <see cref="Padding"/>, <see cref="HorizontalOptions"/>, <see cref="VerticalOptions"/> and <see cref="CanBeDismissedByTappingOutsideOfPopup"/> are the default values of the properties of the same name of every <see cref="Popup"/>,
/// and <see cref="BackgroundColor"/> or <see cref="BackgroundColorDark"/> fills the background of a <see cref="Popup"/> that has neither a <see cref="VisualElement.BackgroundColor"/> nor a <see cref="VisualElement.Background"/>.
/// A value set on the <see cref="Popup"/>, a binding or a <see cref="Style"/> takes precedence over them, except that a <see cref="Popup"/> whose <see cref="Popup.Margin"/> is 0 on every side,
/// or whose <see cref="Popup.HorizontalOptions"/> or <see cref="Popup.VerticalOptions"/> is <see cref="LayoutOptions.Fill"/>, is displayed with <see cref="Margin"/>, <see cref="HorizontalOptions"/> or <see cref="VerticalOptions"/> instead.
/// </para>
/// <para>
/// <see cref="OnInitializing"/> is not a default value: it runs each time a <see cref="Popup"/> is displayed, so a value it sets on the <see cref="Popup"/> replaces the value set before, for example in XAML,
/// and takes precedence over a <see cref="Style"/>.
/// </para>
/// </remarks>
public record DefaultPopupSettings
{
	/// <inheritdoc cref="Views.Popup.Margin" path="/summary"/>
	public Thickness Margin { get; init; } = PopupDefaults.Margin;

	/// <inheritdoc cref="Views.Popup.Padding" path="/summary"/>
	public Thickness Padding { get; init; } = PopupDefaults.Padding;

	/// <inheritdoc cref="Views.Popup.HorizontalOptions" path="/summary"/>
	public LayoutOptions HorizontalOptions { get; init; } = PopupDefaults.HorizontalOptions;

	/// <inheritdoc cref="Views.Popup.VerticalOptions" path="/summary"/>
	public LayoutOptions VerticalOptions { get; init; } = PopupDefaults.VerticalOptions;

	/// <inheritdoc cref="Views.Popup.CanBeDismissedByTappingOutsideOfPopup" path="/summary"/>
	public bool CanBeDismissedByTappingOutsideOfPopup { get; init; } = PopupDefaults.CanBeDismissedByTappingOutsideOfPopup;

	/// <summary>
	/// Gets the color that fills the background of a <see cref="Popup"/> that has neither a <see cref="VisualElement.BackgroundColor"/> nor a <see cref="VisualElement.Background"/>,
	/// unless the app uses the dark theme (see <see cref="BackgroundColorDark"/>).
	/// </summary>
	/// <remarks>
	/// <see cref="Popup"/> does not set its own <see cref="VisualElement.BackgroundColor"/> to this value, so a <see cref="Style"/> can set it.
	/// </remarks>
	public Color BackgroundColor { get; init; } = PopupDefaults.BackgroundColor;

	/// <summary>
	/// Gets the color that fills the background of a <see cref="Popup"/> that has neither a <see cref="VisualElement.BackgroundColor"/> nor a <see cref="VisualElement.Background"/>
	/// while the app uses the dark theme (<see cref="AppTheme.Dark"/>).
	/// </summary>
	/// <remarks>
	/// <see cref="Popup"/> does not set its own <see cref="VisualElement.BackgroundColor"/> to this value, so a <see cref="Style"/> can set it.
	/// The displayed background follows changes of the app's theme while the <see cref="Popup"/> is displayed.
	/// </remarks>
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