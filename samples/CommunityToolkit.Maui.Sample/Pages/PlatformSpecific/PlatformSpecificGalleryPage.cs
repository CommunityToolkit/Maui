using CommunityToolkit.Maui.Sample.ViewModels.PlatformSpecific;

namespace CommunityToolkit.Maui.Sample.Pages;
public class PlatformSpecificGalleryPage(IDeviceInfo deviceInfo, PlatformSpecificGalleryViewModel viewModel) : BaseGalleryPage<PlatformSpecificGalleryViewModel>("Platform Specific", deviceInfo, viewModel)
{
}