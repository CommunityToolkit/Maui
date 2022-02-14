using CommunityToolkit.Maui.Animations;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class AnimationBehaviorPage : BasePage
{
	public AnimationBehaviorPage()
	{
		InitializeComponent();
	}

}

class SampleScaleAnimation : BaseAnimation
{
	public override async Task Animate(VisualElement? view)
	{
		if (view is null)
		{
			return;
		}

		await view.ScaleTo(1.2, Length, Easing);
		await view.ScaleTo(1, Length, Easing);
	}
}