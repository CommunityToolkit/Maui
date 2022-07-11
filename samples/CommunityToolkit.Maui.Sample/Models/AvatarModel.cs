namespace CommunityToolkit.Maui.Sample.Models;

public class AvatarModel
{
	public Color BackgroundColor { get; init; } = Colors.Black;
	public Color BorderColor { get; init; } = Colors.White;
	public Thickness BorderWidth { get; init; } = 0;
	public CornerRadius CornerRadius { get; init; } = new(50, 50, 50, 50);
	public string Description { get; init; } = string.Empty;
	public FontAttributes FontAttributes { get; init; } = FontAttributes.None;
	public double FontSize { get; init; } = 18;
	public double HeightRequest { get; init; } = 64;
	public ImageSource? ImageSource { get; init; }
	public Thickness Padding { get; init; } = 5;
	public string Text { get; init; } = "?";
	public Color TextColor { get; init; } = Colors.White;
	public double WidthRequest { get; init; } = 64;
}