using CommunityToolkit.Maui.Sample.ViewModels.Layouts;

namespace CommunityToolkit.Maui.Sample.Pages.Layouts;

public class LayoutsGalleryPage : BaseGalleryPage<LayoutsGalleryViewModel>
{
	public LayoutsGalleryPage(IDeviceInfo deviceInfo, LayoutsGalleryViewModel layoutGalleryViewModel)
		: base("Layouts", deviceInfo, layoutGalleryViewModel)
	{
	}
}