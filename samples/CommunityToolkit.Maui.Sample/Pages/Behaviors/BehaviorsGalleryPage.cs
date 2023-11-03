using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public class BehaviorsGalleryPage : BaseGalleryPage<BehaviorsGalleryViewModel>
{
	public BehaviorsGalleryPage(IDeviceInfo deviceInfo, BehaviorsGalleryViewModel behaviorsGalleryViewModel)
		: base("Behaviors", deviceInfo, behaviorsGalleryViewModel)
	{
#if ANDROID || IOS
		AddStatusBarBehavior();
#endif
	}

	void AddStatusBarBehavior()
	{
		Behaviors.Add(new StatusBarBehavior
		{
			StatusBarColor = Color.FromRgb(25, 118, 210),
			StatusBarStyle = Core.StatusBarStyle.LightContent,
			ApplyOn = StatusBarApplyOn.OnPageNavigatedTo,
		});
	}
}