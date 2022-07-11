using CommunityToolkit.Maui.Sample.Models;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public sealed class ViewsGalleryViewModel : BaseGalleryViewModel
{
	public ViewsGalleryViewModel()
		: base(new[]
		{
			SectionModel.Create<PopupPositionViewModel>("Custom Positioning Popup", Colors.Red, "Displays a basic popup anywhere on the screen using VerticalOptions and HorizontalOptions"),
			SectionModel.Create<PopupAnchorViewModel>("Anchor Popup", Colors.Red, "Popups can be anchored to other view's on the screen"),
			SectionModel.Create<MultiplePopupViewModel>("Mutiple Popups Page", Colors.Red, "A page demonstrating multiple different Popups"),
			SectionModel.Create<DrawingViewViewModel>("DrawingView", Colors.Red, "DrawingView provides a canvas for users to \"paint\" on the screen. The drawing can also be captured and displayed as an Image."),
			SectionModel.Create<AvatarViewViewModel>("AvatarView", Colors.Red, "AvatarView is a control for displaying an image or user's initials avatar. This page demonstrates available properties and samples."),
		})
	{

	}
}