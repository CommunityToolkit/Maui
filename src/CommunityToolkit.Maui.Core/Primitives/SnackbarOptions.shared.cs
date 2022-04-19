using Font = Microsoft.Maui.Font;
namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Snackbar visual options
/// </summary>
public class SnackbarOptions : ITextStyle
{
	/// <summary>
	/// Snackbar message character spacing
	/// </summary>
	public double CharacterSpacing { get; set; } = AlertDefaults.CharacterSpacing;

	/// <summary>
	/// Snackbar message font
	/// </summary>
	public Font Font { get; set; } = Font.SystemFontOfSize(AlertDefaults.FontSize);

	/// <summary>
	/// Snackbar message text color
	/// </summary>
	public Color TextColor { get; set; } = AlertDefaults.TextColor;

	/// <summary>
	/// Snackbar button font
	/// </summary>
	public Font ActionButtonFont { get; set; } = Font.SystemFontOfSize(AlertDefaults.FontSize);

	/// <summary>
	/// Snackbar action button text color
	/// </summary>
	public Color ActionButtonTextColor { get; set; } = AlertDefaults.TextColor;

	/// <summary>
	/// Snackbar background color
	/// </summary>
	public Color BackgroundColor { get; set; } = AlertDefaults.BackgroundColor;

	/// <summary>
	/// Snackbar corner radius
	/// </summary>
	public CornerRadius CornerRadius { get; set; } = new CornerRadius(4, 4, 4, 4);

}