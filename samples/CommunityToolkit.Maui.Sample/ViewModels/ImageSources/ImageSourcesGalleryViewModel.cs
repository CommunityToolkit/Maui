using CommunityToolkit.Maui.Sample.Models;

namespace CommunityToolkit.Maui.Sample.ViewModels.ImageSources;

public class ImageSourcesGalleryViewModel() : BaseGalleryViewModel(
[
	SectionModel.Create<GravatarImageSourceViewModel>("GravatarImageSource", Colors.Red, "GravatarImageSource allows you to use as an Image source, a users Gravatar registered image via their email address.")
]);