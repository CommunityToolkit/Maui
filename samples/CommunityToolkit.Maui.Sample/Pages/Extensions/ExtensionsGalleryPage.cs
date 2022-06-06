using CommunityToolkit.Maui.Sample.ViewModels.Converters;
using CommunityToolkit.Maui.Sample.ViewModels.Extensions;

namespace CommunityToolkit.Maui.Sample.Pages.Extensions;

public class ExtensionsGalleryPage : BaseGalleryPage<ExtensionsGalleryViewModel>
{
	public ExtensionsGalleryPage(IDeviceInfo deviceInfo, ExtensionsGalleryViewModel extensionsGalleryViewModel)
		: base("Extensions", deviceInfo, extensionsGalleryViewModel)
	{
	}
}