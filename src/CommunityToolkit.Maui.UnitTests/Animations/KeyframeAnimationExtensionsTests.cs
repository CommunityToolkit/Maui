using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
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

		await tadaRun.WaitAsync(waitTimeout, token);
		swingRun.IsCompleted.Should().BeFalse("Tada ends when Swing supersedes it, not at its own 2000 ms end");
		view.AnimationIsRunning(KeyframeAnimationExtensions.AnimationHandle).Should().BeTrue();
		await swingRun.WaitAsync(waitTimeout, token);

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
		view.AnimationIsRunning(KeyframeAnimationExtensions.AnimationHandle).Should().BeTrue("the superseded run's token must not abort the new run");
		await newRun.WaitAsync(waitTimeout, token);

		AssertValuesEqual(view, startingValues);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task SupersededRun_WhoseTokenIsCancelledRightAfterTheNewRunStarts_DoesNotAbortTheNewRun() => RunOnSingleThreadAsync(async token =>
	{
		using var supersededRunTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);

		var firstChange = WaitForAnyChangeAsync(view, startingValues, token);
		var supersededRun = new TadaAnimation { Length = 2000 }.Animate(view, supersededRunTokenSource.Token);
		await firstChange;
		var newRun = new TadaAnimation { Length = functionalTestLength }.Animate(view, token);
		supersededRunTokenSource.Cancel();

		view.AnimationIsRunning(KeyframeAnimationExtensions.AnimationHandle).Should().BeTrue("the superseded run's token must not abort the new run");
		await Record.ExceptionAsync(() => supersededRun.WaitAsync(waitTimeout, token));
		await newRun.WaitAsync(waitTimeout, token);
		newRun.IsCompletedSuccessfully.Should().BeTrue();
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
	public Task SwingAnimation_ShorterThanTheRunningRotateAnimation_ReturnsRotationToItsStartingValue() => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();

		var rotated = WaitForPropertyAsync(view, VisualElement.RotationProperty, token, rotation => Math.Abs(rotation - originalRotation) > 5);
		var rotateRun = new RotateAnimation { Length = 2 * functionalTestLength }.Animate(view, token);
		await rotated;
		var swingRun = new SwingAnimation { Length = functionalTestLength }.Animate(view, token);

		await Task.WhenAll(rotateRun, swingRun).WaitAsync(waitTimeout, token);

		view.Rotation.Should().BeApproximately(originalRotation, precision);
		view.AnchorY.Should().BeApproximately(originalAnchorY, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task TadaAnimation_ShorterThanTheRunningScaleAnimation_ReturnsScaleToItsStartingValue() => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();

		var scaled = WaitForPropertyAsync(view, VisualElement.ScaleProperty, token, scale => Math.Abs(scale - originalScale) > 0.05);
		var scaleRun = new ScaleAnimation { Length = 4 * functionalTestLength }.Animate(view, token);
		await scaled;
		var tadaRun = new TadaAnimation { Length = functionalTestLength }.Animate(view, token);

		await Task.WhenAll(scaleRun, tadaRun).WaitAsync(waitTimeout, token);

		view.Scale.Should().BeApproximately(originalScale, precision);
		view.Rotation.Should().BeApproximately(originalRotation, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task RubberBandAnimation_StartedWhileScaleXAndScaleYToAsyncRun_InterruptsThemAndReturnsBothScalesToTheValuesTheyHadWhenItStarted() => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var scaleXBeforeScaleToAsync = view.ScaleX;

		var scaled = WaitForPropertyAsync(view, VisualElement.ScaleXProperty, token, scaleX => Math.Abs(scaleX - scaleXBeforeScaleToAsync) > 0.05);
		var scaleXRun = view.ScaleXToAsync(2, 4 * functionalTestLength, null, token);
		var scaleYRun = view.ScaleYToAsync(2, 4 * functionalTestLength, null, token);
		await scaled;
		// A raw ScaleXToAsync is not tracked, so the attention seeker takes the mid-animation values as its originals
		var scaleXWhenRubberBandStarts = view.ScaleX;
		var scaleYWhenRubberBandStarts = view.ScaleY;
		var rubberBandRun = new RubberBandAnimation { Length = functionalTestLength }.Animate(view, token);

		var wasScaleXInterrupted = await scaleXRun.WaitAsync(waitTimeout, token);
		var wasScaleYInterrupted = await scaleYRun.WaitAsync(waitTimeout, token);
		await rubberBandRun.WaitAsync(waitTimeout, token);

		wasScaleXInterrupted.Should().BeTrue();
		wasScaleYInterrupted.Should().BeTrue();
		view.ScaleX.Should().BeApproximately(scaleXWhenRubberBandStarts, precision);
		view.ScaleY.Should().BeApproximately(scaleYWhenRubberBandStarts, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task BounceAnimation_StartedWhileTranslateToAsyncRuns_LeavesTheTranslationAtItsExplicitTarget() => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingTranslationX = view.TranslationX;
		var startingTranslationY = view.TranslationY;
		var targetTranslationX = startingTranslationX + 100;

		var moved = WaitForPropertyAsync(view, VisualElement.TranslationXProperty, token, translationX => translationX - startingTranslationX > 5);
		var translateRun = view.TranslateToAsync(targetTranslationX, startingTranslationY, 4 * functionalTestLength, null, token);
		await moved;
		var bounceRun = new BounceAnimation { Length = functionalTestLength }.Animate(view, token);

		await bounceRun.WaitAsync(waitTimeout, token);
		var wasTranslateInterrupted = await translateRun.WaitAsync(waitTimeout, token);

		wasTranslateInterrupted.Should().BeFalse();
		view.TranslationX.Should().BeApproximately(targetTranslationX, precision);
		view.TranslationY.Should().BeApproximately(startingTranslationY, precision);
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

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task SwingAnimation_OnAViewWithoutAnimationManager_ThrowsAndLeavesTheViewAndLaterRunsAtTheOriginalValues() => RunOnSingleThreadAsync(async token =>
	{
		var view = new Label { Rotation = originalRotation, AnchorY = originalAnchorY };
		var animation = new SwingAnimation { Length = functionalTestLength };

		// GetAnimationManager falls back to Application.Current, which BaseViewTest sets and never clears
		var application = Application.Current;
		Application.Current = null;
		try
		{
			await Assert.ThrowsAsync<ArgumentException>(() => animation.Animate(view, token).WaitAsync(waitTimeout, token));
		}
		finally
		{
			Application.Current = application;
		}

		view.Rotation.Should().BeApproximately(originalRotation, precision);
		view.AnchorY.Should().BeApproximately(originalAnchorY, precision);

		view.EnableAnimations();
		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		view.Rotation.Should().BeApproximately(originalRotation, precision);
		view.AnchorY.Should().BeApproximately(originalAnchorY, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task TadaAnimation_WhenAPropertyChangedHandlerThrowsWhileTheCancelledRunRestoresTheView_StillEndsAndRestoresThePropertiesAfterTheThrowingOne() => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
		var throwOnScaleRestore = false;
		view.PropertyChanged += (_, e) =>
		{
			if (throwOnScaleRestore && e.PropertyName == nameof(VisualElement.Scale))
			{
				throwOnScaleRestore = false;
				throw new InvalidOperationException("Thrown by a PropertyChanged handler.");
			}
		};

		var firstChange = WaitForAnyChangeAsync(view, startingValues, token);
		var run = new TadaAnimation { Length = 2000 }.Animate(view, cancellationTokenSource.Token);
		await firstChange;

		throwOnScaleRestore = true;
		var cancelException = Record.Exception(cancellationTokenSource.Cancel);
		cancelException.Should().NotBeNull("the exception of the handler reaches the code that cancels");
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(waitTimeout, token));

		AssertValuesEqual(view, startingValues);

		// MAUI leaves a property whose PropertyChanged handler threw unable to change, so only Rotation is checked
		const double laterRunRotation = 45;
		view.Rotation = laterRunRotation;
		await new TadaAnimation { Length = functionalTestLength }.Animate(view, token).WaitAsync(waitTimeout, token);

		view.Rotation.Should().BeApproximately(laterRunRotation, precision);
	}, TestContext.Current.CancellationToken);
}
