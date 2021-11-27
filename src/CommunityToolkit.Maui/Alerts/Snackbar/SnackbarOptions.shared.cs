using Microsoft.Maui;
using Microsoft.Maui.Graphics;

namespace CommunityToolkit.Maui.Alerts.Snackbar;

/// <summary>
/// Snackbar visual options
/// </summary>
public class SnackbarOptions : ITextStyle
{
	/// <summary>
	/// Snackbar message character spacing
	/// </summary>
	public double CharacterSpacing { get; set; } = 0.0d;

	/// <summary>
	/// Snackbar message font
	/// </summary>
	public Font Font { get; set; } = Font.SystemFontOfSize(14);

	/// <summary>
	/// Snackbar message text color
	/// </summary>
	public Color TextColor { get; set; } = Colors.Black;

	/// <summary>
	/// Snackbar button font
	/// </summary>
	public Font ActionButtonFont { get; set; } = Font.SystemFontOfSize(14);

	/// <summary>
	/// Snackbar action button text color
	/// </summary>
	public Color ActionButtonTextColor { get; set; } = Colors.Black;

	/// <summary>
	/// Snackbar background color
	/// </summary>
	public Color BackgroundColor { get; set; } = Colors.LightGray;

	/// <summary>
	/// Snackbar corner radius
	/// </summary>
	public CornerRadius CornerRadius { get; set; } = new CornerRadius(4, 4, 4, 4);

}