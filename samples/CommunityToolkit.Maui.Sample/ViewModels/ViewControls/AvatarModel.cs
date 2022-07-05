namespace CommunityToolkit.Maui.Sample.ViewModels.ViewControls;

public class AvatarModel
{
	public Color? AvatarBackgroundColor { get; set; } = Colors.Black;
	public FontAttributes? FontAttributes { get; set; }

	public double? FontSize { get; set; } = Device.GetNamedSize(NamedSize.Large, typeof(Label));

	public ImageSource? ImageSource { get; set; }

	public string? Text { get; set; } = "?";

	public Color? TextColor { get; set; } = Colors.White;

	public Thickness? BorderWidth { get; set; } = 0;

	public Thickness? AvatarPadding { get; set; } = 5;

	public double? AvatarWidthRequest { get; set; } = 64;

	public double? AvatarHeightRequest { get; set; } = 64;

	public CornerRadius? CornerRadius { get; set; } = new(50, 50, 50, 50);

	public Color? BorderColor { get; set; }
}