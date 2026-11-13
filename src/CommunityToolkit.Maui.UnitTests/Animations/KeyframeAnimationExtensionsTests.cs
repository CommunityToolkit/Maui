using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

public class KeyframeAnimationExtensionsTests : BaseAnimationTest
{
	const double originalScale = 0.8;
	const double originalRotation = 30;
	const double originalAnchorY = 0.7;

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task DifferentKeyframeAnimation_StartedOnSameView_SupersedesTheRunningOne() => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);

		var firstChange = WaitForAnyChangeAsync(view, startingValues, token);
		var tadaRun = new TadaAnimation { Length = 2000 }.Animate(view, token);
		await firstChange;
		var swingRun = new SwingAnimation { Length = functionalTestLength }.Animate(view, token);

		await Task.WhenAll(tadaRun, swingRun).WaitAsync(waitTimeout, token);

		AssertValuesEqual(view, startingValues);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task SupersededRun_CompletesWithoutThrowing_AndItsTokenNoLongerAbortsTheNewRun() => RunOnSingleThreadAsync(async token =>
	{
		using var supersededRunTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);

		var firstChange = WaitForAnyChangeAsync(view, startingValues, token);
		var supersededRun = new TadaAnimation { Length = 2000 }.Animate(view, supersededRunTokenSource.Token);
		await firstChange;
		var newRun = new TadaAnimation { Length = functionalTestLength }.Animate(view, token);

		await supersededRun.WaitAsync(waitTimeout, token);
		supersededRunTokenSource.Cancel();
		await newRun.WaitAsync(waitTimeout, token);

		AssertValuesEqual(view, startingValues);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task AnimationBehavior_StartingScaleAnimation_WhileTadaRuns_StopsTadaFirst() => RunOnSingleThreadAsync(async token =>
	{
		var scaleAnimation = new ObservedAnimation(new ScaleAnimation { Length = functionalTestLength });
		var behavior = new AnimationBehavior
		{
			AnimationType = scaleAnimation
		};
		var view = CreateAnimatedViewWithNonDefaultValues();
		view.Behaviors.Add(behavior);
		var startingValues = CaptureValues(view);

		var firstChange = WaitForAnyChangeAsync(view, startingValues, token);
		var tadaRun = new TadaAnimation { Length = 2000 }.Animate(view, token);
		await firstChange;

		behavior.AnimateCommand.Execute(token);

		view.AnimationIsRunning(KeyframeAnimationExtensions.AnimationHandle).Should().BeFalse();
		view.Rotation.Should().BeApproximately(originalRotation, precision);
		await tadaRun.WaitAsync(waitTimeout, token);
		await scaleAnimation.WaitForEndedAnimationsAsync(1, token);
		view.Scale.Should().BeApproximately(originalScale, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task AnimationBehavior_StartingTadaAnimation_WhileScaleAnimationRuns_ReturnsScaleToItsStartingValue() => RunOnSingleThreadAsync(async token =>
	{
		var tadaAnimation = new ObservedAnimation(new TadaAnimation { Length = functionalTestLength });
		var behavior = new AnimationBehavior
		{
			AnimationType = tadaAnimation
		};
		var view = CreateAnimatedViewWithNonDefaultValues();
		view.Behaviors.Add(behavior);

		var scaled = WaitForPropertyAsync(view, VisualElement.ScaleProperty, token, scale => Math.Abs(scale - originalScale) > 0.05);
		var scaleRun = new ScaleAnimation { Length = 400 }.Animate(view, token);
		await scaled;

		behavior.AnimateCommand.Execute(token);

		await scaleRun.WaitAsync(waitTimeout, token);
		await tadaAnimation.WaitForEndedAnimationsAsync(1, token);
		view.Scale.Should().BeApproximately(originalScale, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task SwingAnimation_StartedWhileRotateAnimationRuns_ReturnsRotationToItsStartingValue() => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();

		var rotated = WaitForPropertyAsync(view, VisualElement.RotationProperty, token, rotation => Math.Abs(rotation - originalRotation) > 5);
		var rotateRun = new RotateAnimation { Length = functionalTestLength }.Animate(view, token);
		await rotated;
		var swingRun = new SwingAnimation { Length = 2 * functionalTestLength }.Animate(view, token);

		await Task.WhenAll(rotateRun, swingRun).WaitAsync(waitTimeout, token);

		view.Rotation.Should().BeApproximately(originalRotation, precision);
		view.AnchorY.Should().BeApproximately(originalAnchorY, precision);
	}, TestContext.Current.CancellationToken);
}
