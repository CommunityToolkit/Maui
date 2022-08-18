using CommunityToolkit.Maui.Sample.ViewModels.Converters;
using CommunityToolkit.Maui.Sample.ViewModels.ImageSources;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public class ImageSourcesGalleryPage : BaseGalleryPage<ImageSourcesGalleryViewModel>
{
	public ImageSourcesGalleryPage(IDeviceInfo deviceInfo, ImageSourcesGalleryViewModel imageSourcesGalleryViewModel)
		: base("Image Sources", deviceInfo, imageSourcesGalleryViewModel)
	{
	}
}