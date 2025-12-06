using Microsoft.Maui.Controls.Shapes;

namespace CommunityToolkit.Maui;

/// <summary>
/// Popup options.
/// </summary>
public partial class PopupOptions : BindableObject, IPopupOptions
{
	/// <summary>
	/// Gets or sets a value indicating whether the popup can be dismissed by tapping outside of the popup.
	/// Default is provided by <see cref="Options.DefaultPopupOptionsSettings"/>.
	/// </summary>
	[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateCanBeDismissedByTappingOutsideOfPopup))]
	public partial bool CanBeDismissedByTappingOutsideOfPopup { get; set; }
	static object CreateCanBeDismissedByTappingOutsideOfPopup(BindableObject? _) => Options.DefaultPopupOptionsSettings.CanBeDismissedByTappingOutsideOfPopup;

	/// <summary>
	/// Gets or sets an <see cref="Action"/> invoked when the user taps outside of the popup.
	/// Default is provided by <see cref="Options.DefaultPopupOptionsSettings"/>.
	/// </summary>
	[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateOnTappingOutsideOfPopup))]
	public partial Action? OnTappingOutsideOfPopup { get; set; }
	static object? CreateOnTappingOutsideOfPopup(BindableObject? _) => Options.DefaultPopupOptionsSettings.OnTappingOutsideOfPopup;

	/// <summary>
	/// Gets or sets the overlay <see cref="Color"/> applied to the page while the popup is displayed.
	/// Default is provided by <see cref="Options.DefaultPopupOptionsSettings"/>.
	/// </summary>
	[BindableProperty(DefaultValueCreatorMethodName = nameof(CreatePageOverlayColor))]
	public partial Color PageOverlayColor { get; set; }
	static object CreatePageOverlayColor(BindableObject? _) => Options.DefaultPopupOptionsSettings.PageOverlayColor;

	/// <summary>
	/// Gets or sets the <see cref="Shape"/> used to render the popup's outline.
	/// Default is provided by <see cref="Options.DefaultPopupOptionsSettings"/>.
	/// </summary>
	[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateShape))]
	public partial Shape? Shape { get; set; }
	static object? CreateShape(BindableObject? _) => Options.DefaultPopupOptionsSettings.Shape;

	/// <summary>
	/// Gets or sets the <see cref="Shadow"/> applied to the popup.
	/// Default is provided by <see cref="Options.DefaultPopupOptionsSettings"/>.
	/// </summary>
	[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateShadow))]
	public partial Shadow? Shadow { get; set; }
	static object? CreateShadow(BindableObject? _) => Options.DefaultPopupOptionsSettings.Shadow;

	/// <summary>
	/// An empty instance of <see cref="IPopupOptions"/> containing default values.
	/// </summary>
	public static IPopupOptions Empty { get; } = new PopupOptions();
}