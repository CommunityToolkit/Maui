using System.ComponentModel;
using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

public class ToAnimationInterruptionTests : BaseAnimationTest
{
	public static TheoryData<string> ToAnimationNames { get; } = [nameof(FadeAnimation), nameof(ScaleAnimation), nameof(RotateAnimation), nameof(FlipHorizontalAnimation), nameof(FlipVerticalAnimation)];

	public static TheoryData<string> TwoLegAnimationNames { get; } = [nameof(FadeAnimation), nameof(ScaleAnimation), nameof(FlipHorizontalAnimation), nameof(FlipVerticalAnimation)];

	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(ToAnimationNames))]
	public Task CancelAnimationsMidRun_ReturnsTheViewToItsStartingValuesAndCompletes(string animationName) => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		var (property, handle) = PropertyOf(animationName);

		var firstChange = WaitForAnyChangeAsync(view, startingValues, token);
		var run = Create(animationName, 1000).Animate(view, token);
		await firstChange;

		using var restores = new OriginalValueWrites(view, property, startingValues[IndexOf(property)]);
		view.CancelAnimations();
		await run.WaitAsync(waitTimeout, token);

		run.IsCompletedSuccessfully.Should().BeTrue();
		AssertValuesEqual(view, startingValues);
		restores.Count.Should().Be(1);
		view.AnimationIsRunning(handle).Should().BeFalse();
	}, TestContext.Current.CancellationToken);

	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(TwoLegAnimationNames))]
	public Task CancelAnimationsDuringTheReturnLeg_ReturnsTheViewToItsStartingValues(string animationName) => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		var (property, _) = PropertyOf(animationName);
		var animation = Create(animationName, 1000);
		var peak = PeakOf(animationName, animation, startingValues[IndexOf(property)]);
		var original = startingValues[IndexOf(property)];

		var returning = WaitForPropertyAsync(view, property, token, value => Math.Abs(value - peak) <= precision, value => Math.Abs(value - peak) > Math.Abs(peak - original) / 3);
		var run = animation.Animate(view, token);
		await returning;

		view.CancelAnimations();
		await run.WaitAsync(waitTimeout, token);

		run.IsCompletedSuccessfully.Should().BeTrue();
		AssertValuesEqual(view, startingValues);
	}, TestContext.Current.CancellationToken);

	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(ToAnimationNames))]
	public Task AnimationBehaviorTriggeredAgainMidRun_TakesOverWithoutReturningTheViewFirst(string animationName) => RunOnSingleThreadAsync(async token =>
	{
		var animation = new ObservedAnimation(Create(animationName, 1000));
		var behavior = new AnimationBehavior { AnimationType = animation };
		var view = CreateAnimatedViewWithNonDefaultValues();
		view.Behaviors.Add(behavior);
		var startingValues = CaptureValues(view);
		var (property, _) = PropertyOf(animationName);

		var firstChange = WaitForAnyChangeAsync(view, startingValues, token);
		behavior.AnimateCommand.Execute(token);
		await firstChange;

		using (var restores = new OriginalValueWrites(view, property, startingValues[IndexOf(property)]))
		{
			behavior.AnimateCommand.Execute(token);
			await WaitForFramesAsync(view, 3, token);
			restores.Count.Should().Be(0, "the interrupted run must leave the view to the run that took over in the same turn");
		}

		await animation.WaitForEndedAnimationsAsync(2, token);
		AssertValuesEqual(view, startingValues);
	}, TestContext.Current.CancellationToken);

	[Theory(Timeout = (int)TestDuration.Long)]
	[InlineData(nameof(ScaleAnimation), nameof(TadaAnimation))]
	[InlineData(nameof(ScaleAnimation), nameof(HeartbeatAnimation))]
	[InlineData(nameof(RotateAnimation), nameof(SwingAnimation))]
	[InlineData(nameof(RotateAnimation), nameof(WobbleAnimation))]
	[InlineData(nameof(RotateAnimation), nameof(TadaAnimation))]
	public Task AttentionSeekerStartedMidRun_TakesOverWithoutTheToAnimationReturningTheViewFirst(string toAnimationName, string seekerName) => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		var (property, _) = PropertyOf(toAnimationName);

		var firstChange = WaitForAnyChangeAsync(view, startingValues, token);
		var toRun = Create(toAnimationName, 1000).Animate(view, token);
		await firstChange;

		Task seekerRun;
		using (var restores = new OriginalValueWrites(view, property, startingValues[IndexOf(property)]))
		{
			seekerRun = Create(seekerName, functionalTestLength).Animate(view, token);
			await WaitForFramesAsync(view, 3, token);
			restores.Count.Should().Be(0, "the interrupted To animation must leave the view to the attention seeker");
		}

		await Task.WhenAll(toRun, seekerRun).WaitAsync(waitTimeout, token);
		toRun.IsCompletedSuccessfully.Should().BeTrue();
		AssertValuesEqual(view, startingValues);
	}, TestContext.Current.CancellationToken);

	[Theory(Timeout = (int)TestDuration.Long)]
	[InlineData(true)]
	[InlineData(false)]
	public Task AppFadeToAsyncTakesOver_TheInterruptedFadeAnimationDoesNotReturnTheView(bool cancelAnimationsFirst) => RunOnSingleThreadAsync(async token =>
	{
		const double appTarget = 0.5;
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		var originalOpacity = view.Opacity;

		var firstChange = WaitForAnyChangeAsync(view, startingValues, token);
		var run = new FadeAnimation { Opacity = 0.2, Length = 1000 }.Animate(view, token);
		await firstChange;

		Task<bool> appFade;
		using (var restores = new OriginalValueWrites(view, VisualElement.OpacityProperty, originalOpacity))
		{
			if (cancelAnimationsFirst)
			{
				view.CancelAnimations();
			}

			appFade = view.FadeToAsync(appTarget, functionalTestLength, Easing.Linear, token);
			await WaitForFramesAsync(view, 3, token);
			restores.Count.Should().Be(0, "the app's own FadeToAsync took over");
		}

		await run.WaitAsync(waitTimeout, token);
		(await appFade.WaitAsync(waitTimeout, token)).Should().BeFalse();
		view.Opacity.Should().BeApproximately(appTarget, precision);
	}, TestContext.Current.CancellationToken);

	public static TheoryData<string, bool> ToAnimationsAndWhetherCancelAnimationsComesFirst { get; } = new()
	{
		{ nameof(FadeAnimation), false }, { nameof(FadeAnimation), true },
		{ nameof(ScaleAnimation), false }, { nameof(ScaleAnimation), true },
		{ nameof(RotateAnimation), false }, { nameof(RotateAnimation), true },
		{ nameof(FlipHorizontalAnimation), false }, { nameof(FlipHorizontalAnimation), true },
		{ nameof(FlipVerticalAnimation), false }, { nameof(FlipVerticalAnimation), true },
	};

	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(ToAnimationsAndWhetherCancelAnimationsComesFirst))]
	public Task AppsOwnToAnimationHoldingTheCurrentValueTakesOver_TheInterruptedRunDoesNotReturnTheView(string animationName, bool cancelAnimationsFirst) => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		var (property, _) = PropertyOf(animationName);
		var originalValue = startingValues[IndexOf(property)];

		var firstChange = WaitForAnyChangeAsync(view, startingValues, token);
		var run = Create(animationName, 1000).Animate(view, token);
		await firstChange;

		Task<bool> appRun;
		using (var restores = new OriginalValueWrites(view, property, originalValue))
		{
			if (cancelAnimationsFirst)
			{
				view.CancelAnimations();
			}

			// Targeting the current value keeps the app's frames from changing it, so only the AnimationIsRunning guard can tell the run that it was taken over
			appRun = AppToAnimation(animationName, view, (double)view.GetValue(property), token);
			await run.WaitAsync(waitTimeout, token);
			await appRun.WaitAsync(waitTimeout, token);
			restores.Count.Should().Be(0, "the app's own To animation took over");
		}
	}, TestContext.Current.CancellationToken);

	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(ToAnimationNames))]
	public Task CancelAnimationsThenTheAppSetsTheProperty_KeepsTheAppsValue(string animationName) => RunOnSingleThreadAsync(async token =>
	{
		const double appValue = 0.55;
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		var (property, _) = PropertyOf(animationName);

		var firstChange = WaitForAnyChangeAsync(view, startingValues, token);
		var run = Create(animationName, 1000).Animate(view, token);
		await firstChange;

		view.CancelAnimations();
		view.SetValue(property, appValue);
		await run.WaitAsync(waitTimeout, token);

		run.IsCompletedSuccessfully.Should().BeTrue();
		((double)view.GetValue(property)).Should().Be(appValue);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task SecondAnimationBehaviorOnTheSameView_ReturnsThePropertyTheFirstOneAnimated() => RunOnSingleThreadAsync(async token =>
	{
		var scale = new ObservedAnimation(new ScaleAnimation { Scale = 1.6, Length = 1000 });
		var fade = new ObservedAnimation(new FadeAnimation { Opacity = 0.2, Length = functionalTestLength });
		var scaleBehavior = new AnimationBehavior { AnimationType = scale };
		var fadeBehavior = new AnimationBehavior { AnimationType = fade };
		var view = CreateAnimatedViewWithNonDefaultValues();
		view.Behaviors.Add(scaleBehavior);
		view.Behaviors.Add(fadeBehavior);
		var startingValues = CaptureValues(view);

		var firstChange = WaitForAnyChangeAsync(view, startingValues, token);
		scaleBehavior.AnimateCommand.Execute(token);
		await firstChange;

		using (var restores = new OriginalValueWrites(view, VisualElement.ScaleProperty, startingValues[IndexOf(VisualElement.ScaleProperty)]))
		{
			fadeBehavior.AnimateCommand.Execute(token);
			await WaitForFramesAsync(view, 3, token);
			restores.Count.Should().Be(1, "nothing took over the scale, so the interrupted ScaleAnimation returns it");
			view.Scale.Should().Be(startingValues[IndexOf(VisualElement.ScaleProperty)]);
		}

		await scale.WaitForEndedAnimationsAsync(1, token);
		await fade.WaitForEndedAnimationsAsync(1, token);
		AssertValuesEqual(view, startingValues);
	}, TestContext.Current.CancellationToken);

	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(ToAnimationNames))]
	public Task Animate_AfterTheAppSetTheValueInTheSameTurnAsCancelAnimations_ReturnsToTheAppsValue(string animationName) => RunOnSingleThreadAsync(async token =>
	{
		const double appValue = 0.55;
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		var property = PropertyOf(animationName).Property;

		var firstChange = WaitForAnyChangeAsync(view, startingValues, token);
		var run = Create(animationName, 1000).Animate(view, token);
		await firstChange;

		view.CancelAnimations();
		view.SetValue(property, appValue);
		await run.WaitAsync(waitTimeout, token);
		((double)view.GetValue(property)).Should().Be(appValue);

		await Create(animationName, functionalTestLength).Animate(view, token).WaitAsync(waitTimeout, token);
		((double)view.GetValue(property)).Should().BeApproximately(appValue, precision, "the app's own value was the view's value at rest when the second run started");
	}, TestContext.Current.CancellationToken);

	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(ToAnimationNames))]
	public Task Animate_AfterTheAppSetTheValueFollowingTheInterruptedRun_ReturnsToTheAppsValue(string animationName) => RunOnSingleThreadAsync(async token =>
	{
		const double appValue = 0.55;
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		var property = PropertyOf(animationName).Property;

		var firstChange = WaitForAnyChangeAsync(view, startingValues, token);
		var run = Create(animationName, 1000).Animate(view, token);
		await firstChange;

		view.CancelAnimations();
		await run.WaitAsync(waitTimeout, token);
		view.SetValue(property, appValue);

		await Create(animationName, functionalTestLength).Animate(view, token).WaitAsync(waitTimeout, token);
		((double)view.GetValue(property)).Should().BeApproximately(appValue, precision);
	}, TestContext.Current.CancellationToken);

	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(ToAnimationNames))]
	public Task AnimationBehavior_AfterTheAppSetTheValueInTheSameTurnAsCancelAnimations_ReturnsToTheAppsValue(string animationName) => RunOnSingleThreadAsync(async token =>
	{
		const double appValue = 0.55;
		var animation = new ObservedAnimation(Create(animationName, functionalTestLength));
		var behavior = new AnimationBehavior { AnimationType = animation };
		var view = CreateAnimatedViewWithNonDefaultValues();
		view.Behaviors.Add(behavior);
		var startingValues = CaptureValues(view);
		var property = PropertyOf(animationName).Property;

		var firstChange = WaitForAnyChangeAsync(view, startingValues, token);
		var run = Create(animationName, 1000).Animate(view, token);
		await firstChange;

		view.CancelAnimations();
		view.SetValue(property, appValue);
		await run.WaitAsync(waitTimeout, token);

		behavior.AnimateCommand.Execute(token);
		await animation.WaitForEndedAnimationsAsync(1, token);
		((double)view.GetValue(property)).Should().BeApproximately(appValue, precision);
	}, TestContext.Current.CancellationToken);

	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(ToAnimationNames))]
	public Task Animate_StartedInTheSameTurnAsCancelAnimationsAndTheAppsWrite_ReturnsToTheAppsValue(string animationName) => RunOnSingleThreadAsync(async token =>
	{
		const double appValue = 0.55;
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		var property = PropertyOf(animationName).Property;

		var firstChange = WaitForAnyChangeAsync(view, startingValues, token);
		var run = Create(animationName, 1000).Animate(view, token);
		await firstChange;

		view.CancelAnimations();
		view.SetValue(property, appValue);
		var second = Create(animationName, functionalTestLength).Animate(view, token);
		await Task.WhenAll(run, second).WaitAsync(waitTimeout, token);
		((double)view.GetValue(property)).Should().BeApproximately(appValue, precision);
	}, TestContext.Current.CancellationToken);

	[Theory(Timeout = (int)TestDuration.Long)]
	[InlineData(nameof(ScaleAnimation), nameof(TadaAnimation))]
	[InlineData(nameof(ScaleAnimation), nameof(HeartbeatAnimation))]
	[InlineData(nameof(RotateAnimation), nameof(SwingAnimation))]
	[InlineData(nameof(RotateAnimation), nameof(WobbleAnimation))]
	[InlineData(nameof(RotateAnimation), nameof(TadaAnimation))]
	public Task AttentionSeekerTakingOverWhileAnotherAnimationKeepsTheTickerRunning_TheInterruptedToAnimationDoesNotRestoreTheView(string toAnimationName, string seekerName) => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		var (property, _) = PropertyOf(toAnimationName);
		var original = (double)view.GetValue(property);

		// A second animation keeps the ticker running, so the seeker's first frame comes a tick later, as on iOS
		var keepTicking = new FadeAnimation { Opacity = 0.2, Length = 700 }.Animate(view, token);
		var changed = WaitForPropertyAsync(view, property, token, value => Math.Abs(value - original) > 0.05);
		var toRun = Create(toAnimationName, 600).Animate(view, token);
		await changed;

		Task seekerRun;
		using (var restores = new OriginalValueWrites(view, property, original))
		{
			seekerRun = Create(seekerName, 600).Animate(view, token);
			await WaitForFramesAsync(view, 3, token);
			restores.Count.Should().Be(0, "a restore after the yield would undo the seeker's first frames");
		}

		await Task.WhenAll(toRun, seekerRun, keepTicking).WaitAsync(waitTimeout, token);
		AssertValuesEqual(view, startingValues);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task FadeInterruptedAtTheLegBoundaryThenTheAppSetsTheTargetValue_NextRunReturnsToTheAppsValue() => RunOnSingleThreadAsync(async token =>
	{
		const double target = 0.0;
		var view = CreateAnimatedViewWithNonDefaultValues();
		var originalOpacity = view.Opacity;

		// A keyframe animation keeps the ticker running, so the return leg's first frame comes a tick after the first leg ends, as on a device
		var keepTicking = new ShakeAnimation { Length = 800 }.Animate(view, token);
		var atTarget = WaitForPropertyAsync(view, VisualElement.OpacityProperty, token, value => value == target);
		var run = new FadeAnimation { Opacity = target, Length = 400 }.Animate(view, token);
		await atTarget;
		view.Opacity.Should().Be(target, "still at the boundary between the legs");

		view.CancelAnimations();
		await run.WaitAsync(waitTimeout, token);
		view.Opacity.Should().Be(originalOpacity, "nothing took over, so the interrupted fade returned the view");

		view.Opacity = target;
		await new FadeAnimation { Opacity = 1, Length = functionalTestLength }.Animate(view, token).WaitAsync(waitTimeout, token);

		view.Opacity.Should().BeApproximately(target, precision, "the app's own value was the view's value at rest when the second run started");
		await keepTicking.WaitAsync(waitTimeout, token);
	}, TestContext.Current.CancellationToken);

	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(ToAnimationNames))]
	public Task AppWritesTheInterruptedValueAfterTheRestore_NextRunReturnsToIt(string animationName) => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		var (property, _) = PropertyOf(animationName);
		var original = (double)view.GetValue(property);

		var firstChange = WaitForAnyChangeAsync(view, startingValues, token);
		var run = Create(animationName, 1000).Animate(view, token);
		await firstChange;

		var valueWhenInterrupted = (double)view.GetValue(property);
		view.CancelAnimations();
		await run.WaitAsync(waitTimeout, token);
		((double)view.GetValue(property)).Should().Be(original);

		view.SetValue(property, valueWhenInterrupted);
		await Create(animationName, functionalTestLength).Animate(view, token).WaitAsync(waitTimeout, token);

		((double)view.GetValue(property)).Should().BeApproximately(valueWhenInterrupted, precision, "the app's value was the view's value at rest when the run started");
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

	static double PeakOf(string animationName, BaseAnimation animation, double original) => animationName switch
	{
		nameof(FadeAnimation) => ((FadeAnimation)animation).Opacity,
		nameof(ScaleAnimation) => ((ScaleAnimation)animation).Scale,
		nameof(FlipHorizontalAnimation) or nameof(FlipVerticalAnimation) => ((RotateAnimation)animation).Rotation,
		nameof(RotateAnimation) => original + ((RotateAnimation)animation).Rotation,
		_ => throw new NotSupportedException(animationName)
	};

	static (BindableProperty Property, string Handle) PropertyOf(string animationName) => animationName switch
	{
		nameof(FadeAnimation) => (VisualElement.OpacityProperty, nameof(Microsoft.Maui.Controls.ViewExtensions.FadeToAsync)),
		nameof(ScaleAnimation) => (VisualElement.ScaleProperty, nameof(Microsoft.Maui.Controls.ViewExtensions.ScaleToAsync)),
		nameof(RotateAnimation) => (VisualElement.RotationProperty, nameof(Microsoft.Maui.Controls.ViewExtensions.RotateToAsync)),
		nameof(FlipHorizontalAnimation) => (VisualElement.RotationYProperty, nameof(Microsoft.Maui.Controls.ViewExtensions.RotateYToAsync)),
		nameof(FlipVerticalAnimation) => (VisualElement.RotationXProperty, nameof(Microsoft.Maui.Controls.ViewExtensions.RotateXToAsync)),
		_ => throw new NotSupportedException(animationName)
	};

	static Task<bool> AppToAnimation(string animationName, VisualElement view, double target, CancellationToken token) => animationName switch
	{
		nameof(FadeAnimation) => view.FadeToAsync(target, functionalTestLength, Easing.Linear, token),
		nameof(ScaleAnimation) => view.ScaleToAsync(target, functionalTestLength, Easing.Linear, token),
		nameof(RotateAnimation) => view.RotateToAsync(target, functionalTestLength, Easing.Linear, token),
		nameof(FlipHorizontalAnimation) => view.RotateYToAsync(target, functionalTestLength, Easing.Linear, token),
		nameof(FlipVerticalAnimation) => view.RotateXToAsync(target, functionalTestLength, Easing.Linear, token),
		_ => throw new NotSupportedException(animationName)
	};

	static int IndexOf(BindableProperty property) => Array.FindIndex(animatableProperties, animatable => animatable.Property == property);

	static async Task WaitForFramesAsync(VisualElement view, int count, CancellationToken token)
	{
		var seen = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var frames = 0;
		using var observation = view.ObserveAnimationFrames(_ =>
		{
			if (++frames >= count)
			{
				seen.TrySetResult();
			}
		});
		await seen.Task.WaitAsync(waitTimeout, token);
	}

	// A restore writes the original value itself, while a tween never lands on it exactly in the frames right after another run takes over.
	// The window ends after frames, not at the next frame: the mock ticker fires inside Start, before an aborted run's posted continuation.
	sealed class OriginalValueWrites : IDisposable
	{
		readonly VisualElement view;
		readonly BindableProperty property;
		readonly double originalValue;

		public OriginalValueWrites(VisualElement view, BindableProperty property, double originalValue)
		{
			this.view = view;
			this.property = property;
			this.originalValue = originalValue;
			view.PropertyChanged += HandlePropertyChanged;
		}

		public int Count { get; private set; }

		public void Dispose() => view.PropertyChanged -= HandlePropertyChanged;

		void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == property.PropertyName && (double)view.GetValue(property) == originalValue)
			{
				Count++;
			}
		}
	}
}
