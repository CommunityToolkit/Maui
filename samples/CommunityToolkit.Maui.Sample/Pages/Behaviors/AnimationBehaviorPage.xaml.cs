using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class AnimationBehaviorPage : BasePage<AnimationBehaviorViewModel>
{
	public AnimationBehaviorPage(AnimationBehaviorViewModel animationBehaviorViewModel) : base(animationBehaviorViewModel)
	{
		InitializeComponent();
	}
}

class SampleScaleAnimation : BaseAnimation
{
	public override async Task Animate(VisualElement view, CancellationToken token)
	{
		await view.ScaleTo(1.2, Length, Easing).WaitAsync(token);
		await view.ScaleTo(1, Length, Easing).WaitAsync(token);
	}
}

class SampleScaleToAnimation : BaseAnimation
{
	public double Scale { get; set; }

	public override Task Animate(VisualElement view, CancellationToken token)
		=> view.ScaleTo(Scale, Length, Easing).WaitAsync(token);
}