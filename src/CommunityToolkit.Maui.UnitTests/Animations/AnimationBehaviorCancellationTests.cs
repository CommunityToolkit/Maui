using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Behaviors;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

public class AnimationBehaviorCancellationTests : BaseAnimationTest
{
	public static TheoryData<string> AllAnimationNames { get; } = [nameof(FadeAnimation), nameof(ScaleAnimation), nameof(RotateAnimation), nameof(FlipHorizontalAnimation), nameof(FlipVerticalAnimation), nameof(ShakeAnimation), nameof(TadaAnimation), nameof(HeartbeatAnimation), nameof(RubberBandAnimation), nameof(BounceAnimation), nameof(SwingAnimation), nameof(WobbleAnimation)];

	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(AllAnimationNames))]
	public Task AnimationBehaviorCommandTokenCancelledMidRun_EndsWithoutAnExceptionAndReturnsTheView(string animationName) => RunOnSingleThreadAsync(async token =>
	{
		using var commandTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
		var animation = new ObservedAnimation(Create(animationName, 1000));
		var behavior = new AnimationBehavior { AnimationType = animation };
		var view = CreateAnimatedViewWithNonDefaultValues();
		view.Behaviors.Add(behavior);
		var startingValues = CaptureValues(view);

		var firstChange = WaitForAnyChangeAsync(view, startingValues, token);
		behavior.AnimateCommand.Execute(commandTokenSource.Token);
		await firstChange;

		commandTokenSource.Cancel();
		await animation.WaitForEndedAnimationsAsync(1, token);

		AssertValuesEqual(view, startingValues);
	}, TestContext.Current.CancellationToken);

	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(AllAnimationNames))]
	public Task AnimationBehaviorTriggeredWithACancelledTokenWhileAnimating_StopsTheRunningAnimationWithoutAnException(string animationName) => RunOnSingleThreadAsync(async token =>
	{
		using var cancelledTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
		await cancelledTokenSource.CancelAsync();
		var animation = new ObservedAnimation(Create(animationName, 1000));
		var behavior = new AnimationBehavior { AnimationType = animation };
		var view = CreateAnimatedViewWithNonDefaultValues();
		view.Behaviors.Add(behavior);
		var startingValues = CaptureValues(view);

		var firstChange = WaitForAnyChangeAsync(view, startingValues, token);
		behavior.AnimateCommand.Execute(CancellationToken.None);
		await firstChange;

		behavior.AnimateCommand.Execute(cancelledTokenSource.Token);
		await animation.WaitForEndedAnimationsAsync(1, token);

		AssertValuesEqual(view, startingValues);
	}, TestContext.Current.CancellationToken);

	static BaseAnimation Create(string animationName, uint length) => animationName switch
	{
		nameof(FadeAnimation) => new FadeAnimation { Opacity = 0.2, Length = length },
		nameof(ScaleAnimation) => new ScaleAnimation { Scale = 1.6, Length = length },
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
