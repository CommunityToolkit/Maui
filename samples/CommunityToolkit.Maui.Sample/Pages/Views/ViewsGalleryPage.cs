using CommunityToolkit.Maui.Sample.ViewModels.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public class ViewsGalleryPage : BaseGalleryPage<ViewsGalleryViewModel>
{
	public ViewsGalleryPage(IDeviceInfo deviceInfo, ViewsGalleryViewModel viewsGalleryViewModel)
		: base("Views", deviceInfo, viewsGalleryViewModel)
	{
	}
}