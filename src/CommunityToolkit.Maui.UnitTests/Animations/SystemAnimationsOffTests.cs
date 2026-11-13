using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.UnitTests.Mocks;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

// With the system's animations off (iOS Reduce Motion, Android's "Remove animations"), MAUI jumps every tween to its end and reports it as aborted
public class SystemAnimationsOffTests : BaseAnimationTest
{
	public static TheoryData<string> AnimationNames { get; } = [nameof(FadeAnimation), nameof(ScaleAnimation), nameof(RotateAnimation), nameof(FlipHorizontalAnimation), nameof(FlipVerticalAnimation), nameof(ShakeAnimation), nameof(TadaAnimation), nameof(HeartbeatAnimation), nameof(RubberBandAnimation), nameof(BounceAnimation), nameof(SwingAnimation), nameof(WobbleAnimation)];

	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(AnimationNames))]
	public Task Animate_WithSystemAnimationsOff_CompletesAndLeavesTheViewAtItsStartingValues(string animationName) => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		view.DisableSystemAnimations();

		await CreateAnimation(animationName).Animate(view, token).WaitAsync(waitTimeout, token);

		AssertValuesEqual(view, startingValues);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task AnimationBehavior_WithSystemAnimationsOff_LeavesTheViewAtItsStartingValues() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new ObservedAnimation(CreateAnimation(nameof(FlipHorizontalAnimation)));
		var behavior = new AnimationBehavior { AnimationType = animation };
		var view = CreateAnimatedViewWithNonDefaultValues();
		view.Behaviors.Add(behavior);
		var startingValues = CaptureValues(view);
		view.DisableSystemAnimations();

		behavior.AnimateCommand.Execute(token);
		await animation.WaitForEndedAnimationsAsync(1, token);

		AssertValuesEqual(view, startingValues);
	}, TestContext.Current.CancellationToken);

	static BaseAnimation CreateAnimation(string animationName) => animationName switch
	{
		nameof(FadeAnimation) => new FadeAnimation { Opacity = 0.3, Length = functionalTestLength },
		nameof(ScaleAnimation) => new ScaleAnimation { Scale = 1.2, Length = functionalTestLength },
		nameof(RotateAnimation) => new RotateAnimation { Rotation = 180, Length = functionalTestLength },
		nameof(FlipHorizontalAnimation) => new FlipHorizontalAnimation { Rotation = 90, Length = functionalTestLength },
		nameof(FlipVerticalAnimation) => new FlipVerticalAnimation { Rotation = 90, Length = functionalTestLength },
		nameof(ShakeAnimation) => new ShakeAnimation { Length = functionalTestLength },
		nameof(TadaAnimation) => new TadaAnimation { Length = functionalTestLength },
		nameof(HeartbeatAnimation) => new HeartbeatAnimation { Length = functionalTestLength },
		nameof(RubberBandAnimation) => new RubberBandAnimation { Length = functionalTestLength },
		nameof(BounceAnimation) => new BounceAnimation { Length = functionalTestLength },
		nameof(SwingAnimation) => new SwingAnimation { Length = functionalTestLength },
		nameof(WobbleAnimation) => new WobbleAnimation { Length = functionalTestLength },
		_ => throw new NotSupportedException(animationName)
	};
}
