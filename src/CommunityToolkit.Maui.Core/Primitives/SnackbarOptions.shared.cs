using Font = Microsoft.Maui.Font;
namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Snackbar visual options
/// </summary>
public class SnackbarOptions : ITextStyle
{
	/// <summary>
	/// Snackbar message character spacing, in a unit that depends on the platform
	/// </summary>
	/// <remarks>
	/// Unlike the CharacterSpacing of a .NET MAUI Label, which is in device-independent units, the value is applied as:
	/// <list type="bullet">
	/// <item><description>Android: the letter spacing of the message (<c>TextView.LetterSpacing</c>), in em, whatever the font size: 0.1 adds a tenth of the text size between characters.</description></item>
	/// <item><description>iOS and Mac Catalyst: the kerning of the message, in points: 100 times the value divided by the point size of the message font (no kerning when that point size is 0), so 1 with a 20-point font kerns by 5 points.</description></item>
	/// <item><description>Windows and Tizen: not applied.</description></item>
	/// </list>
	/// </remarks>
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
	public CornerRadius CornerRadius { get; set; } = new(4, 4, 4, 4);

}