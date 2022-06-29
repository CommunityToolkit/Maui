using CommunityToolkit.Maui.Sample.ViewModels.ViewControls;

namespace CommunityToolkit.Maui.Sample.Pages.ViewControls;

public class ControlsGalleryPage : BaseGalleryPage<ControlsGalleryViewModel>
{
	public ControlsGalleryPage(IDeviceInfo deviceInfo, ControlsGalleryViewModel viewModel)
		: base("Controls", deviceInfo, viewModel)
	{
	}
}