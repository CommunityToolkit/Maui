using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Extensions;

public class ExtensionsGalleryPage : BaseGalleryPage<ExtensionsGalleryViewModel>
{
	public ExtensionsGalleryPage(IDeviceInfo deviceInfo, ExtensionsGalleryViewModel extensionsGalleryViewModel)
		: base("Extensions", deviceInfo, extensionsGalleryViewModel)
	{
	}
}