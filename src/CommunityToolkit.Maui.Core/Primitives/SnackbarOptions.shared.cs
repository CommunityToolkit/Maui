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
	public double CharacterSpacing { get; set; } = Defaults.CharacterSpacing;

	/// <summary>
	/// Snackbar message font
	/// </summary>
	public Font Font { get; set; } = Font.SystemFontOfSize(Defaults.FontSize);

	/// <summary>
	/// Snackbar message text color
	/// </summary>
	public Color TextColor { get; set; } = Defaults.TextColor;

	/// <summary>
	/// Snackbar button font
	/// </summary>
	public Font ActionButtonFont { get; set; } = Font.SystemFontOfSize(Defaults.FontSize);

	/// <summary>
	/// Snackbar action button text color
	/// </summary>
	public Color ActionButtonTextColor { get; set; } = Defaults.TextColor;

	/// <summary>
	/// Snackbar background color
	/// </summary>
	public Color BackgroundColor { get; set; } = Defaults.BackgroundColor;

	/// <summary>
	/// Snackbar corner radius
	/// </summary>
	public CornerRadius CornerRadius { get; set; } = new CornerRadius(4, 4, 4, 4);

}