using CommunityToolkit.Maui.Sample.ViewModels.Essentials;

namespace CommunityToolkit.Maui.Sample.Pages.Essentials;

public class EssentialsGalleryPage : BaseGalleryPage<EssentialsGalleryViewModel>
{
	public EssentialsGalleryPage(IDeviceInfo deviceInfo, EssentialsGalleryViewModel essentialsGalleryViewModel)
		: base("Essentials", deviceInfo, essentialsGalleryViewModel)
	{
	}
}