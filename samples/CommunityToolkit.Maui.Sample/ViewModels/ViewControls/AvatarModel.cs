namespace CommunityToolkit.Maui.Sample.ViewModels.ViewControls;

public class AvatarModel
{
	public Color AvatarBackgroundColor { get; init; } = Colors.Black;
	public FontAttributes? FontAttributes { get; init; }

	public double FontSize { get; init; } = Device.GetNamedSize(NamedSize.Large, typeof(Label));

	public ImageSource ImageSource { get; init; }

	public string Text { get; init; } = "?";

	public Color TextColor { get; init; } = Colors.White;

	public Thickness BorderWidth { get; init; } = 0;

	public Thickness? AvatarPadding { get; set; } = 5;

	public double? AvatarWidthRequest { get; set; } = 64;

	public double? AvatarHeightRequest { get; set; } = 64;

	public CornerRadius? CornerRadius { get; set; } = new(50, 50, 50, 50);

	public Color? BorderColor { get; set; }
}