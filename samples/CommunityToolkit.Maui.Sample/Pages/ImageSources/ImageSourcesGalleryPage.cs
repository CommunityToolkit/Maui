using CommunityToolkit.Maui.Sample.ViewModels.ImageSources;

namespace CommunityToolkit.Maui.Sample.Pages.ImageSources;

public class ImageSourcesGalleryPage : BaseGalleryPage<ImageSourcesGalleryViewModel>
{
	public ImageSourcesGalleryPage(IDeviceInfo deviceInfo, ImageSourcesGalleryViewModel imageSourcesGalleryViewModel)
		: base("Image Sources", deviceInfo, imageSourcesGalleryViewModel)
	{
	}
}