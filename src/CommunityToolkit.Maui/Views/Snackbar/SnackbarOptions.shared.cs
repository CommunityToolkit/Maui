using Microsoft.Maui;
using Microsoft.Maui.Graphics;

namespace CommunityToolkit.Maui.Controls.Snackbar;

public class SnackbarOptions
{
	public double CharacterSpacing { get; set; }

	public Font Font { get; set; } = Font.SystemFontOfSize(14);

	public Color TextColor { get; set; } = Colors.Black;

	public Color ActionTextColor { get; set; } = Colors.Black;

	public Color BackgroundColor { get; set; } = Colors.LightGray;

	public CornerRadius CornerRadius { get; set; } = new CornerRadius(4, 4, 4, 4);

}
