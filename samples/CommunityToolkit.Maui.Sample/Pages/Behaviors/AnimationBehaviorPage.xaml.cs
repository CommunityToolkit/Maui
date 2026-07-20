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
		await ScaleToAsync(view, 1.2, Length, Easing, token);
		await ScaleToAsync(view, 1, Length, Easing, token);
	}

	static Task ScaleToAsync(VisualElement view, double scale, uint length, Easing? easing, CancellationToken token)
	{
		return view.ScaleToAsync(scale, length, easing).WaitAsync(token);
	}
}

partial class SampleScaleToAnimation : BaseAnimation
{
	public double Scale { get; set; }

	public override Task Animate(VisualElement view, CancellationToken token = default)
		=> ScaleToAsync(view, Scale, Length, Easing, token);

	static Task ScaleToAsync(VisualElement view, double scale, uint length, Easing? easing, CancellationToken token)
	{
		return view.ScaleToAsync(scale, length, easing).WaitAsync(token);
	}
}