using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public class BehaviorsGalleryPage : BaseGalleryPage<BehaviorsGalleryViewModel>
{
	public BehaviorsGalleryPage(IDeviceInfo deviceInfo, BehaviorsGalleryViewModel behaviorsGalleryViewModel)
		: base("Behaviors", deviceInfo, behaviorsGalleryViewModel)
	{
	}
}