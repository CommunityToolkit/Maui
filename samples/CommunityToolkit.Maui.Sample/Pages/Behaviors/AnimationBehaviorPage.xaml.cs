using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class AnimationBehaviorPage : BasePage<AnimationBehaviorViewModel>
{
	public AnimationBehaviorPage(IDeviceInfo deviceInfo, AnimationBehaviorViewModel animationBehaviorViewModel)
		: base(deviceInfo, animationBehaviorViewModel)
	{
		InitializeComponent();
	}
}

class SampleScaleAnimation : BaseAnimation
{
	public override async Task Animate(VisualElement view)
	{
		await view.ScaleTo(1.2, Length, Easing);
		await view.ScaleTo(1, Length, Easing);
	}
}