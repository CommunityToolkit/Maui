using System.Diagnostics;
using System.Globalization;
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
	public Task TadaAnimation_WhenAPropertyChangedHandlerThrowsWhileTheCancelledRunRestoresTheView_FaultsTheRunWithThatExceptionAndRestoresThePropertiesAfterTheThrowingOne() => RunOnSingleThreadAsync(async token =>
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
		cancelException.Should().BeNull("the run's Task reports the exception of the handler, not the code that cancels");
		var runException = await Assert.ThrowsAsync<InvalidOperationException>(() => run.WaitAsync(waitTimeout, token));
		runException.Message.Should().Be("Thrown by a PropertyChanged handler.");

		AssertValuesEqual(view, startingValues);

		// MAUI leaves a property whose PropertyChanged handler threw unable to change, so only Rotation is checked
		const double laterRunRotation = 45;
		view.Rotation = laterRunRotation;
		await new TadaAnimation { Length = functionalTestLength }.Animate(view, token).WaitAsync(waitTimeout, token);

		view.Rotation.Should().BeApproximately(laterRunRotation, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task TadaAnimation_WhenAPropertyChangedHandlerThrowsWhileTheCompletedRunRestoresTheView_FaultsTheRunWithThatExceptionAndRestoresEveryProperty() => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var restoreThrew = MoveTheViewAfterTheLastFrameAndThrowWhenTheScaleIsRestored(view);

		var run = new TadaAnimation { Length = functionalTestLength }.Animate(view, token);

		var runException = await Assert.ThrowsAsync<InvalidOperationException>(() => run.WaitAsync(waitTimeout, token));
		runException.Message.Should().Be("Thrown by a PropertyChanged handler.");
		restoreThrew().Should().BeTrue();
		view.Rotation.Should().BeApproximately(originalRotation, precision, "the properties after the throwing one are restored too");
		view.Scale.Should().BeApproximately(originalScale, precision);
		view.AnimationIsRunning(KeyframeAnimationExtensions.AnimationHandle).Should().BeFalse();
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task TadaAnimation_WhenAPropertyChangedHandlerThrowsWhileASupersededRunRestoresTheView_FaultsOnlyTheSupersededRun() => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
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
		var supersededRun = new TadaAnimation { Length = 2000 }.Animate(view, token);
		await firstChange;

		throwOnScaleRestore = true;
		var newRun = new TadaAnimation { Length = functionalTestLength }.Animate(view, token);

		await Assert.ThrowsAsync<InvalidOperationException>(() => supersededRun.WaitAsync(waitTimeout, token));
		await newRun.WaitAsync(waitTimeout, token);
		newRun.IsCompletedSuccessfully.Should().BeTrue("the newer run is not the one that failed to restore the view");
		AssertValuesEqual(view, startingValues);
	}, TestContext.Current.CancellationToken);

	[Fact]
	public void AnimationBehavior_WhenAPropertyChangedHandlerThrowsWhileTheRunRestoresTheView_SuppressesTheExceptionAndReportsItToTheTraceWhenTheOptionIsSet()
	{
		var (escapedException, reportedToTheTrace) = RunBehaviorWhoseRestoreThrows(shouldSuppressExceptionsInAnimations: true);

		Assert.Null(escapedException);
		Assert.Contains(reportedToTheTrace, message => message.Contains("Thrown by a PropertyChanged handler.", StringComparison.Ordinal));
	}

	[Fact]
	public void AnimationBehavior_WhenAPropertyChangedHandlerThrowsWhileTheRunRestoresTheView_ReportsTheExceptionToTheAppWhenTheOptionIsNotSet()
	{
		var (escapedException, reportedToTheTrace) = RunBehaviorWhoseRestoreThrows(shouldSuppressExceptionsInAnimations: false);

		Assert.IsType<InvalidOperationException>(escapedException);
		Assert.Empty(reportedToTheTrace);
	}

	static (Exception? EscapedException, List<string> ReportedToTheTrace) RunBehaviorWhoseRestoreThrows(bool shouldSuppressExceptionsInAnimations)
	{
		var behavior = new AnimationBehavior { AnimationType = new TadaAnimation { Length = functionalTestLength } };
		var view = CreateAnimatedViewWithNonDefaultValues();
		view.Behaviors.Add(behavior);
		MoveTheViewAfterTheLastFrameAndThrowWhenTheScaleIsRestored(view);
		var reportedToTheTrace = new List<string>();
		using var traceListener = new InformationRecordingTraceListener(reportedToTheTrace);

		new Options().SetShouldSuppressExceptionsInAnimations(shouldSuppressExceptionsInAnimations);
		Trace.Listeners.Add(traceListener);
		try
		{
			// AsyncContext rethrows an exception that escapes the fire-and-forget AnimateCommand, which would crash an app
			var escapedException = Record.Exception(() => UndisposedAsyncContext.Run(() => behavior.AnimateCommand.Execute(TestContext.Current.CancellationToken)));

			return (escapedException, reportedToTheTrace);
		}
		finally
		{
			Trace.Listeners.Remove(traceListener);
			new Options().SetShouldSuppressExceptionsInAnimations(false);
		}
	}

	// The last frame of MAUI's tween always writes the animation's end values, which are the original values, so a restore only writes (and a handler only throws)
	// when something moved the view after that frame. MAUI leaves a property whose PropertyChanged handler threw unable to change, so the handler throws once.
	static Func<bool> MoveTheViewAfterTheLastFrameAndThrowWhenTheScaleIsRestored(VisualElement view)
	{
		var hasMovedTheView = false;
		var hasThrown = false;
		view.PropertyChanged += (_, e) =>
		{
			if (!hasMovedTheView && e.PropertyName == nameof(VisualElement.Rotation) && view.Rotation == originalRotation)
			{
				hasMovedTheView = true;
				view.Scale = 3;
				view.Rotation = 99;
			}
			else if (hasMovedTheView && !hasThrown && e.PropertyName == nameof(VisualElement.Scale) && view.Scale == originalScale)
			{
				hasThrown = true;
				throw new InvalidOperationException("Thrown by a PropertyChanged handler.");
			}
		};

		return () => hasThrown;
	}

	// AnimationBehavior reports a suppressed exception with Trace.TraceInformation, while the mock ticker only writes the exceptions that escape a frame with Trace.WriteLine
	sealed class InformationRecordingTraceListener(List<string> messages) : TraceListener
	{
		public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? format, params object?[]? args)
		{
			if (eventType is TraceEventType.Information && format is not null && args is not null)
			{
				messages.Add(string.Format(CultureInfo.InvariantCulture, format, args));
			}
		}

		public override void Write(string? message)
		{
		}

		public override void WriteLine(string? message)
		{
		}
	}
}
