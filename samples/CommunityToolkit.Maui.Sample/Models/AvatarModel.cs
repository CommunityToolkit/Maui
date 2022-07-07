namespace CommunityToolkit.Maui.Sample.Models;

public class AvatarModel
{
	public Color BackgroundColor { get; init; } = Colors.Black;

	public FontAttributes? FontAttributes { get; init; }

#pragma warning disable CS0612 // Type or member is obsolete, however is still used in Microsoft.Maui
	public double FontSize { get; init; } = Device.GetNamedSize(NamedSize.Large, typeof(Label));
#pragma warning restore CS0612 // Type or member is obsolete, however is still used in Microsoft.Maui

	public ImageSource? ImageSource { get; init; }

	public string Text { get; init; } = "?";

	public Color TextColor { get; init; } = Colors.White;

	public Thickness BorderWidth { get; init; } = 0;

	public Thickness Padding { get; init; } = 5;

	public double WidthRequest { get; init; } = 64;

	public double HeightRequest { get; init; } = 64;

	public CornerRadius CornerRadius { get; init; } = new(50, 50, 50, 50);

	public Color? BorderColor { get; init; }
}