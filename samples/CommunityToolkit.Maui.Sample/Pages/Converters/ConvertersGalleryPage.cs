using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public class ConvertersGalleryPage : BaseGalleryPage<ConvertersGalleryViewModel>
{
	public ConvertersGalleryPage(IDeviceInfo deviceInfo, ConvertersGalleryViewModel convertersGalleryViewModel)
		: base("Converters", deviceInfo, convertersGalleryViewModel)
	{
	}
}