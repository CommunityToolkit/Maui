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

	/// <inheritdoc/>
	/// <remarks>
	/// When true and the user taps outside the popup, it will dismiss.
	/// </remarks>
	[BindableProperty]
	public partial bool CanBeDismissedByTappingOutsideOfPopup { get; set; } = Options.DefaultPopupOptionsSettings.CanBeDismissedByTappingOutsideOfPopup;

	/// <inheritdoc/>
	/// <remarks>
	/// On Android - when false the hardware back button is disabled.
	/// </remarks>
	[BindableProperty]
	public partial bool CanBeDismissedByBackButtonPressed { get; set; } = Options.DefaultPopupOptionsSettings.CanBeDismissedByBackButtonPressed;

	/// <inheritdoc/>
	[BindableProperty]
	public partial Action? OnTappingOutsideOfPopup { get; set; } = Options.DefaultPopupOptionsSettings.OnTappingOutsideOfPopup;

	/// <inheritdoc/>
	[BindableProperty]
	public partial Action? OnBackButtonPressed { get; set; } = Options.DefaultPopupOptionsSettings.OnBackButtonPressed;
	
	/// <inheritdoc/>
	[BindableProperty]
	public partial Action? OnDismiss { get; set; } = Options.DefaultPopupOptionsSettings.OnDismiss;

	/// <inheritdoc/>
	[BindableProperty]
	public partial Color PageOverlayColor { get; set; } = Options.DefaultPopupOptionsSettings.PageOverlayColor;

	/// <inheritdoc/>
	[BindableProperty]
	public partial Shape? Shape { get; set; } = Options.DefaultPopupOptionsSettings.Shape;

	/// <inheritdoc cref="IPopupOptions.Shadow"/>
	[BindableProperty]
	public partial Shadow? Shadow { get; set; } = Options.DefaultPopupOptionsSettings.Shadow;
}