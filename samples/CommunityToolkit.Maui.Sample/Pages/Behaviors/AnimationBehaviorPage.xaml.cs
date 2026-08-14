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
	public override async Task Animate(VisualElement view, CancellationToken token = default)
	{
#if NET11_0_OR_GREATER
		await view.ScaleToAsync(1.2, Length, Easing, token);
		await view.ScaleToAsync(1, Length, Easing, token);
#else
		await view.ScaleToAsync(1.2, Length, Easing).WaitAsync(token);
		await view.ScaleToAsync(1, Length, Easing).WaitAsync(token);
#endif
	}
}

partial class SampleScaleToAnimation : BaseAnimation
{
	public double Scale { get; set; }

	public override Task Animate(VisualElement view, CancellationToken token = default)
#if NET11_0_OR_GREATER
		=> view.ScaleToAsync(Scale, Length, Easing, token);
#else
		=> view.ScaleToAsync(Scale, Length, Easing).WaitAsync(token);
#endif
}