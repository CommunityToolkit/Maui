namespace CommunityToolkit.Maui.Sample.ViewModels.ViewControls;

public class AvatarModel
{
	public Color BackgroundColor { get; set; } = Colors.Black;
	public FontAttributes FontAttributes { get; set; } = FontAttributes.Bold | FontAttributes.Italic;

	public double FontSize { get; set; } = Device.GetNamedSize(NamedSize.Large, typeof(Label));

	public ImageSource? ImageSource { get; set; }

	public string Text { get; set; } = "?";

	public Color TextColor { get; set; } = Colors.White;
}