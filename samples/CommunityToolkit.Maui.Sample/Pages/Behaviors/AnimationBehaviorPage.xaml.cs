using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class AnimationBehaviorPage : BasePage<AnimationBehaviorViewModel>
{
	public AnimationBehaviorPage(AnimationBehaviorViewModel animationBehaviorViewModel) : base(animationBehaviorViewModel)
	{
		InitializeComponent();
	}

	void OnStopSpinClicked(object? sender, EventArgs e) => SpinningBorder.CancelAnimations();
}

partial class SampleScaleAnimation : BaseAnimation
{
	public override async Task Animate(VisualElement view, CancellationToken token = default)
	{
		await view.ScaleToAsync(1.2, Length, Easing, token);
		await view.ScaleToAsync(1, Length, Easing, token);
	}
}

partial class SampleScaleToAnimation : BaseAnimation
{
	public double Scale { get; set; }

	public override Task Animate(VisualElement view, CancellationToken token = default)
		=> view.ScaleToAsync(Scale, Length, Easing, token);
}