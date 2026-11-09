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
	/// While both this property and <see cref="Views.Popup.CanBeDismissedByTappingOutsideOfPopup"/> of the <see cref="Views.Popup"/> displayed with these options are <see langword="true"/>, the <see cref="Views.Popup"/> is dismissed when the user taps outside of it or, on Android, presses the hardware back button.
	/// While either is <see langword="false"/>, tapping outside of the <see cref="Views.Popup"/> and the hardware back button do nothing.
	/// </remarks>
	[BindableProperty]
	public partial bool CanBeDismissedByTappingOutsideOfPopup { get; set; } = Options.DefaultPopupOptionsSettings.CanBeDismissedByTappingOutsideOfPopup;

	/// <inheritdoc/>
	[BindableProperty]
	public partial Action? OnTappingOutsideOfPopup { get; set; } = Options.DefaultPopupOptionsSettings.OnTappingOutsideOfPopup;

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