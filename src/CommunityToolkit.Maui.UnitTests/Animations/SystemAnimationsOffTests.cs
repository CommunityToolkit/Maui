using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
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

	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(AnimationNames))]
	public Task SystemAnimationsTurnedOffMidRun_CompletesAtTheStartingValues(string animationName) => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		var firstChange = WaitForAnyChangeAsync(view, startingValues, token);

		var run = CreateAnimation(animationName, 1000).Animate(view, token);
		await firstChange;
		view.DisableSystemAnimations();

		await run.WaitAsync(waitTimeout, token);

		run.IsCompletedSuccessfully.Should().BeTrue();
		AssertValuesEqual(view, startingValues);
	}, TestContext.Current.CancellationToken);

	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(AnimationNames))]
	public Task WithSystemAnimationsOff_EndsBeforeAnimateReturns(string animationName) => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		view.DisableSystemAnimations();

		var run = CreateAnimation(animationName, 1000).Animate(view, token);

		run.IsCompletedSuccessfully.Should().BeTrue("with the system's animations off no frame may show the view away from its starting values");
		AssertValuesEqual(view, startingValues);
		await run;
	}, TestContext.Current.CancellationToken);

	static BaseAnimation CreateAnimation(string animationName, uint length = functionalTestLength) => animationName switch
	{
		nameof(FadeAnimation) => new FadeAnimation { Opacity = 0.3, Length = length },
		nameof(ScaleAnimation) => new ScaleAnimation { Scale = 1.2, Length = length },
		nameof(RotateAnimation) => new RotateAnimation { Rotation = 180, Length = length },
		nameof(FlipHorizontalAnimation) => new FlipHorizontalAnimation { Rotation = 90, Length = length },
		nameof(FlipVerticalAnimation) => new FlipVerticalAnimation { Rotation = 90, Length = length },
		nameof(ShakeAnimation) => new ShakeAnimation { Length = length },
		nameof(TadaAnimation) => new TadaAnimation { Length = length },
		nameof(HeartbeatAnimation) => new HeartbeatAnimation { Length = length },
		nameof(RubberBandAnimation) => new RubberBandAnimation { Length = length },
		nameof(BounceAnimation) => new BounceAnimation { Length = length },
		nameof(SwingAnimation) => new SwingAnimation { Length = length },
		nameof(WobbleAnimation) => new WobbleAnimation { Length = length },
		_ => throw new NotSupportedException(animationName)
	};
}
