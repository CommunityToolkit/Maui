using Microsoft.Maui.Controls.Shapes;

namespace CommunityToolkit.Maui;

/// <summary>
/// Popup options.
/// </summary>
public partial class PopupOptions : BindableObject, IPopupOptions
{
	/// <summary>
	/// An empty instance of <see cref="IPopupOptions"/> containing default values.
	/// </summary>
	public static IPopupOptions Empty { get; } = new PopupOptions();

	/// <summary>
	/// Gets or sets a value indicating whether the popup can be dismissed by tapping outside the popup.
	/// Default is provided by <see cref="Options.DefaultPopupOptionsSettings"/>.
	/// </summary>
	[BindableProperty]
	public partial bool CanBeDismissedByTappingOutsideOfPopup { get; set; } = Options.DefaultPopupOptionsSettings.CanBeDismissedByTappingOutsideOfPopup;

	/// <summary>
	/// Gets or sets an <see cref="Action"/> invoked when the user taps outside the popup.
	/// Default is provided by <see cref="Options.DefaultPopupOptionsSettings"/>.
	/// </summary>
	[BindableProperty]
	public partial Action? OnTappingOutsideOfPopup { get; set; } = Options.DefaultPopupOptionsSettings.OnTappingOutsideOfPopup;

	/// <summary>
	/// Gets or sets the overlay <see cref="Color"/> applied to the page while the popup is displayed.
	/// Default is provided by <see cref="Options.DefaultPopupOptionsSettings"/>.
	/// </summary>
	[BindableProperty]
	public partial Color PageOverlayColor { get; set; } = Options.DefaultPopupOptionsSettings.PageOverlayColor;

	/// <summary>
	/// Gets or sets the <see cref="Shape"/> used to render the popup's outline.
	/// Default is provided by <see cref="Options.DefaultPopupOptionsSettings"/>.
	/// </summary>
	[BindableProperty]
	public partial Shape? Shape { get; set; } = Options.DefaultPopupOptionsSettings.Shape;

	/// <summary>
	/// Gets or sets the <see cref="Shadow"/> applied to the popup.
	/// Default is provided by <see cref="Options.DefaultPopupOptionsSettings"/>.
	/// </summary>
	[BindableProperty]
	public partial Shadow? Shadow { get; set; } = Options.DefaultPopupOptionsSettings.Shadow;
}