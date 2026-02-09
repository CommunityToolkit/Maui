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

partial class SampleScaleAnimation : BaseAnimation
{
	public override async Task Animate(VisualElement view, CancellationToken token)
	{
		await view.ScaleToAsync(1.2, Length, Easing).WaitAsync(token);
		await view.ScaleToAsync(1, Length, Easing).WaitAsync(token);
	}
}

partial class SampleScaleToAnimation : BaseAnimation
{
	public double Scale { get; set; }

	public override Task Animate(VisualElement view, CancellationToken token)
		=> view.ScaleToAsync(Scale, Length, Easing).WaitAsync(token);
}