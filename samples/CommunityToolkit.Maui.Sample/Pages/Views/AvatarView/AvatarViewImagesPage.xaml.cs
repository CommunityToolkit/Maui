using CommunityToolkit.Maui.Sample.ViewModels.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class AvatarViewImagesPage : BasePage<AvatarViewImagesViewModel>
{
	public AvatarViewImagesPage(AvatarViewImagesViewModel avatarViewImagesViewModel) : base(avatarViewImagesViewModel) => InitializeComponent();
	
	void Button_OnClicked(object? sender, EventArgs e)
	{
		buggyImage.ImageSource = ImageSource.FromFile("avatar_icon.png");
	}
}