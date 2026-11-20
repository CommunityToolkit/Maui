using System.ComponentModel;
using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;
using MauiViewExtensions = Microsoft.Maui.Controls.ViewExtensions;

namespace CommunityToolkit.Maui.UnitTests.Animations;

// An app's own .NET MAUI To animation that starts during an attention seeker usually ticks after it, and writes the property as an absolute value
public class AppsToAnimationDuringAttentionSeekerTests : BaseAnimationTest
{
	const uint seekerLength = 640;
	const uint appsAnimationLength = 160;
	const int framesBeforeTheAppsAnimation = 3;
	const int appsAnimationFrames = (int)(appsAnimationLength / 16);

	public static TheoryData<string, string> SeekersAndTheAppsToAnimationsOfTheirProperties { get; } = new()
	{
		{ nameof(HeartbeatAnimation), nameof(MauiViewExtensions.ScaleToAsync) },
		{ nameof(HeartbeatAnimation), nameof(MauiViewExtensions.RelScaleToAsync) },
		{ nameof(TadaAnimation), nameof(MauiViewExtensions.ScaleToAsync) },
		{ nameof(TadaAnimation), nameof(MauiViewExtensions.RotateToAsync) },
		{ nameof(RubberBandAnimation), nameof(MauiViewExtensions.ScaleXToAsync) },
		{ nameof(RubberBandAnimation), nameof(MauiViewExtensions.ScaleYToAsync) },
		{ nameof(SwingAnimation), nameof(MauiViewExtensions.RotateToAsync) },
		{ nameof(SwingAnimation), nameof(MauiViewExtensions.RelRotateToAsync) },
		{ nameof(WobbleAnimation), nameof(MauiViewExtensions.RotateToAsync) },
	};

	public static TheoryData<string, string> SeekersWithOneOrTwoProperties { get; } = new()
	{
		{ nameof(HeartbeatAnimation), nameof(MauiViewExtensions.ScaleToAsync) },
		{ nameof(TadaAnimation), nameof(MauiViewExtensions.RotateToAsync) },
	};

	public static TheoryData<string, string> SeekersAndTheAppsToAnimationsOfAPropertyTheyDoNotAnimate { get; } = new()
	{
		{ nameof(HeartbeatAnimation), nameof(MauiViewExtensions.RotateToAsync) },
		{ nameof(HeartbeatAnimation), nameof(MauiViewExtensions.ScaleXToAsync) },
		{ nameof(TadaAnimation), nameof(MauiViewExtensions.ScaleYToAsync) },
		{ nameof(RubberBandAnimation), nameof(MauiViewExtensions.ScaleToAsync) },
		{ nameof(RubberBandAnimation), nameof(MauiViewExtensions.RotateToAsync) },
		{ nameof(SwingAnimation), nameof(MauiViewExtensions.ScaleToAsync) },
		{ nameof(WobbleAnimation), nameof(MauiViewExtensions.ScaleToAsync) },
		{ nameof(BounceAnimation), nameof(MauiViewExtensions.RotateToAsync) },
		{ nameof(ShakeAnimation), nameof(MauiViewExtensions.ScaleToAsync) },
	};

	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(SeekersAndTheAppsToAnimationsOfTheirProperties))]
	public Task AppsToAnimation_StartedWhileASeekerAnimatesItsProperty_TakesOverAndKeepsItsTarget(string seekerName, string appsAnimationName) => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		var property = PropertyOf(appsAnimationName);
		var otherSeekerProperties = SeekerPropertiesOf(seekerName).Where(seekerProperty => seekerProperty != property).ToArray();
		var original = (double)view.GetValue(property);
		var valuesAtEachFrame = new List<double>();
		var otherValuesAtEachFrame = new List<double[]>();
		var isSeekerRunningAtEachFrame = new List<bool>();
		var (appsStart, appsTarget) = (double.NaN, double.NaN);
		var appsAnimationStarted = new TaskCompletionSource<Task<bool>>(TaskCreationOptions.RunContinuationsAsynchronously);
		var writesAfterTheAppsStart = new List<double>();

		void RecordWrite(object? sender, PropertyChangedEventArgs e)
		{
			if (appsAnimationStarted.Task.IsCompleted && e.PropertyName == property.PropertyName)
			{
				writesAfterTheAppsStart.Add((double)view.GetValue(property));
			}
		}

		view.PropertyChanged += RecordWrite;
		Task seekerRun;
		using (view.ObserveAnimationFrames(_ =>
		{
			valuesAtEachFrame.Add((double)view.GetValue(property));
			otherValuesAtEachFrame.Add([.. otherSeekerProperties.Select(otherProperty => (double)view.GetValue(otherProperty))]);
			isSeekerRunningAtEachFrame.Add(view.AnimationIsRunning(KeyframeAnimationExtensions.AnimationHandle));
			if (valuesAtEachFrame.Count == framesBeforeTheAppsAnimation)
			{
				appsStart = (double)view.GetValue(property);
				appsTarget = TargetAwayFromTheOriginal(property, original, appsStart);
				appsAnimationStarted.SetResult(StartAppsAnimation(view, appsAnimationName, appsTarget, appsAnimationLength, token));
			}
		}))
		{
			seekerRun = CreateSeeker(seekerName).Animate(view, token);
			var appsAnimation = await appsAnimationStarted.Task.WaitAsync(waitTimeout, token);
			var wasAppsAnimationInterrupted = await appsAnimation.WaitAsync(waitTimeout, token);
			wasAppsAnimationInterrupted.Should().BeFalse();
			await seekerRun.WaitAsync(waitTimeout, token);
			await WaitForTickerFramesAsync(view, 3, token);
		}

		view.PropertyChanged -= RecordWrite;

		seekerRun.IsCompletedSuccessfully.Should().BeTrue("a seeker that another animation takes over completes, like one a ScaleAnimation or RotateAnimation takes over");
		isSeekerRunningAtEachFrame[framesBeforeTheAppsAnimation].Should().BeFalse("the seeker ends in the first frame the app's animation ticks");
		for (var frame = framesBeforeTheAppsAnimation; frame < valuesAtEachFrame.Count; frame++)
		{
			var progress = Math.Min(1, (frame - framesBeforeTheAppsAnimation + 1) / (double)appsAnimationFrames);
			valuesAtEachFrame[frame].Should().BeApproximately(appsStart + ((appsTarget - appsStart) * progress), precision, $"frame {frame} shows the app's animation");
			for (var i = 0; i < otherSeekerProperties.Length; i++)
			{
				otherValuesAtEachFrame[frame][i].Should().BeApproximately(startingValues[IndexOf(otherSeekerProperties[i])], precision, $"frame {frame} shows {otherSeekerProperties[i].PropertyName} back at its original value");
			}
		}

		var appsFrameValues = AppsFrameValues(appsStart, appsTarget);
		writesAfterTheAppsStart.Should().OnlyContain(value => IsOneOf(value, appsFrameValues), "nothing but the app's animation writes the property after it started");
		AssertValuesEqual(view, WithValue(startingValues, property, appsTarget));
	}, TestContext.Current.CancellationToken);

	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(SeekersWithOneOrTwoProperties))]
	public Task AppsToAnimationToTheCurrentValue_StartedWhileASeekerRuns_HoldsTheViewAtThatValue(string seekerName, string appsAnimationName) => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		var property = PropertyOf(appsAnimationName);
		var (seekerRun, appsAnimation, appsStart) = await StartTheAppsAnimationDuringASeekerAsync(view, seekerName, appsAnimationName, (_, current) => current, appsAnimationLength, token);

		(await appsAnimation.WaitAsync(waitTimeout, token)).Should().BeFalse();
		await seekerRun.WaitAsync(waitTimeout, token);
		await WaitForTickerFramesAsync(view, 3, token);

		seekerRun.IsCompletedSuccessfully.Should().BeTrue();
		AssertValuesEqual(view, WithValue(startingValues, property, appsStart));
	}, TestContext.Current.CancellationToken);

	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(SeekersWithOneOrTwoProperties))]
	public Task AppsZeroLengthToAnimation_StartedWhileASeekerRuns_LeavesTheViewAtItsTarget(string seekerName, string appsAnimationName) => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		var property = PropertyOf(appsAnimationName);
		var appsTarget = double.NaN;
		var (seekerRun, appsAnimation, _) = await StartTheAppsAnimationDuringASeekerAsync(view, seekerName, appsAnimationName, (original, current) => appsTarget = TargetAwayFromTheOriginal(property, original, current), 0, token);

		(await appsAnimation.WaitAsync(waitTimeout, token)).Should().BeFalse();
		await seekerRun.WaitAsync(waitTimeout, token);
		await WaitForTickerFramesAsync(view, 3, token);

		seekerRun.IsCompletedSuccessfully.Should().BeTrue();
		AssertValuesEqual(view, WithValue(startingValues, property, appsTarget));
	}, TestContext.Current.CancellationToken);

	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(SeekersWithOneOrTwoProperties))]
	public Task AppsToAnimationCancelledAfterItTookOverFromASeeker_LeavesTheValueItReached(string seekerName, string appsAnimationName) => RunOnSingleThreadAsync(async token =>
	{
		using var appsTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		var property = PropertyOf(appsAnimationName);
		var (seekerRun, appsAnimation, _) = await StartTheAppsAnimationDuringASeekerAsync(view, seekerName, appsAnimationName, (original, current) => TargetAwayFromTheOriginal(property, original, current), 10 * appsAnimationLength, token, appsTokenSource.Token);
		await WaitForFramesAsync(view, 3, token);

		await appsTokenSource.CancelAsync();
		var valueWhenCancelled = (double)view.GetValue(property);
		(await appsAnimation.WaitAsync(waitTimeout, token)).Should().BeTrue();
		await seekerRun.WaitAsync(waitTimeout, token);
		await WaitForTickerFramesAsync(view, 3, token);

		seekerRun.IsCompletedSuccessfully.Should().BeTrue();
		AssertValuesEqual(view, WithValue(startingValues, property, valueWhenCancelled));
	}, TestContext.Current.CancellationToken);

	// Tada stops the app's ScaleToAsync before its RotateToAsync, whose continuation starts a ScaleToAsync that MAUI ticks before Tada in every frame
	[Fact(Timeout = (int)TestDuration.Long)]
	public Task AppsScaleToAsyncStartedInsideTheSeekersStart_TakesOverInTheSeekersFirstFrame() => RunOnSingleThreadAsync(async token =>
	{
		const double appsTarget = 1.6;
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		Task<bool>? appsScaleStartedInsideTheSeekersStart = null;
		var appsStart = double.NaN;
		var isRecordingScaleWrites = false;
		var scaleWritesAfterTheAppsStart = new List<double>();
		void RecordScaleWrite(object? sender, PropertyChangedEventArgs e)
		{
			if (isRecordingScaleWrites && e.PropertyName == VisualElement.ScaleProperty.PropertyName)
			{
				scaleWritesAfterTheAppsStart.Add(view.Scale);
			}
		}

		view.PropertyChanged += RecordScaleWrite;
		var appsRotation = view.RotateToAsync(view.Rotation, seekerLength, Easing.Linear, token);
		_ = appsRotation.ContinueWith(_ =>
		{
			// MAUI writes the app's first frame before ScaleToAsync returns
			appsStart = view.Scale;
			isRecordingScaleWrites = true;
			appsScaleStartedInsideTheSeekersStart = view.ScaleToAsync(appsTarget, appsAnimationLength, Easing.Linear, token);
		}, token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

		var seekerRun = CreateSeeker(nameof(TadaAnimation)).Animate(view, token);
		var appsScale = appsScaleStartedInsideTheSeekersStart ?? throw new InvalidOperationException("Tada's start did not abort the app's RotateToAsync.");

		await seekerRun.WaitAsync(waitTimeout, token);
		(await appsScale.WaitAsync(waitTimeout, token)).Should().BeFalse();
		await WaitForTickerFramesAsync(view, 3, token);
		view.PropertyChanged -= RecordScaleWrite;

		seekerRun.IsCompletedSuccessfully.Should().BeTrue();
		var appsFrameValues = AppsFrameValues(appsStart, appsTarget);
		scaleWritesAfterTheAppsStart.Should().OnlyContain(value => IsOneOf(value, appsFrameValues), "the seeker writes no scale after the app's ScaleToAsync started");
		AssertValuesEqual(view, WithValue(startingValues, VisualElement.ScaleProperty, appsTarget));
	}, TestContext.Current.CancellationToken);

	// MAUI plays a run's first step inside Commit; stopping the run there would leave Commit to start a tween that keeps ticking for the run's whole length
	[Fact(Timeout = (int)TestDuration.Long)]
	public Task AppsScaleToAsyncStartedInsideTheSeekersStart_LeavesNoTweenTickingAfterItEnds() => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		Task<bool>? appsScaleStartedInsideTheSeekersStart = null;
		var appsRotation = view.RotateToAsync(view.Rotation, seekerLength, Easing.Linear, token);
		_ = appsRotation.ContinueWith(_ => appsScaleStartedInsideTheSeekersStart = view.ScaleToAsync(1.6, appsAnimationLength, Easing.Linear, token), token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

		var seekerRun = new TadaAnimation { Length = 60_000 }.Animate(view, token);
		var appsScale = appsScaleStartedInsideTheSeekersStart ?? throw new InvalidOperationException("Tada's start did not abort the app's RotateToAsync.");

		await seekerRun.WaitAsync(waitTimeout, token);
		await appsScale.WaitAsync(waitTimeout, token);
		await WaitForTickerFramesAsync(view, 3, token);

		view.GetTickingAnimationCount().Should().Be(0);
	}, TestContext.Current.CancellationToken);

	// Aborted in the turn it started, the app's animation never wrote a value of its own, so nothing took over
	[Fact(Timeout = (int)TestDuration.Long)]
	public Task AppsToAnimationAbortedBeforeItsFirstFrame_LeavesTheSeekerRunningToItsEnd() => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		var elapsedMillisecondsBeforeTheSeeker = view.GetElapsedAnimationMilliseconds();
		var (seekerRun, appsAnimation, _) = await StartTheAppsAnimationDuringASeekerAsync(view, nameof(HeartbeatAnimation), nameof(MauiViewExtensions.ScaleToAsync), (_, _) => 1.6, appsAnimationLength, token);
		view.AbortAnimation(nameof(MauiViewExtensions.ScaleToAsync));

		(await appsAnimation.WaitAsync(waitTimeout, token)).Should().BeTrue();
		await seekerRun.WaitAsync(waitTimeout, token);

		(view.GetElapsedAnimationMilliseconds() - elapsedMillisecondsBeforeTheSeeker).Should().BeGreaterThanOrEqualTo(seekerLength);
		AssertValuesEqual(view, startingValues);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task AppsDirectWriteWhileASeekerRuns_LeavesTheSeekerRunningToItsEnd() => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		var elapsedMillisecondsBeforeTheSeeker = view.GetElapsedAnimationMilliseconds();
		var firstChange = WaitForAnyChangeAsync(view, startingValues, token);
		var seekerRun = CreateSeeker(nameof(HeartbeatAnimation)).Animate(view, token);
		await firstChange;

		view.Scale = 1.6;
		await seekerRun.WaitAsync(waitTimeout, token);

		(view.GetElapsedAnimationMilliseconds() - elapsedMillisecondsBeforeTheSeeker).Should().BeGreaterThanOrEqualTo(seekerLength);
		AssertValuesEqual(view, startingValues);
	}, TestContext.Current.CancellationToken);

	// A seeker is only ended by a To animation of a property it animates, so the others cannot tell a seeker's end from its running on
	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(SeekersAndTheAppsToAnimationsOfAPropertyTheyDoNotAnimate))]
	public Task AppsToAnimationOfAPropertyTheSeekerDoesNotAnimate_LeavesTheSeekerRunningToItsEnd(string seekerName, string appsAnimationName) => RunOnSingleThreadAsync(async token =>
	{
		const double appsScaleTarget = 1.6;
		const double appsRotationTarget = 90;
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		var property = PropertyOf(appsAnimationName);
		var appsTarget = property == VisualElement.RotationProperty ? appsRotationTarget : appsScaleTarget;
		SeekerPropertiesOf(seekerName).Should().NotContain(property, "the row is about a property the seeker does not animate");
		var elapsedMillisecondsBeforeTheSeeker = view.GetElapsedAnimationMilliseconds();
		var firstChange = WaitForAnyChangeAsync(view, startingValues, token);
		var seekerRun = CreateSeeker(seekerName).Animate(view, token);
		await firstChange;

		var appsAnimation = StartAppsAnimation(view, appsAnimationName, appsTarget, appsAnimationLength, token);
		await seekerRun.WaitAsync(waitTimeout, token);
		var elapsedMillisecondsWhenTheSeekerEnded = view.GetElapsedAnimationMilliseconds() - elapsedMillisecondsBeforeTheSeeker;
		(await appsAnimation.WaitAsync(waitTimeout, token)).Should().BeFalse();
		await WaitForTickerFramesAsync(view, 3, token);

		elapsedMillisecondsWhenTheSeekerEnded.Should().BeGreaterThanOrEqualTo(seekerLength, "the app's animation of another property does not end the seeker");
		seekerRun.IsCompletedSuccessfully.Should().BeTrue();
		AssertValuesEqual(view, WithValue(startingValues, property, appsTarget));
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task RotateAnimation_StartedWhileHeartbeatRuns_LeavesHeartbeatRunningToItsEnd() => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		var elapsedMillisecondsBeforeTheSeeker = view.GetElapsedAnimationMilliseconds();
		var firstChange = WaitForAnyChangeAsync(view, startingValues, token);
		var seekerRun = CreateSeeker(nameof(HeartbeatAnimation)).Animate(view, token);
		await firstChange;

		var rotateRun = new RotateAnimation { Rotation = 90, Length = functionalTestLength }.Animate(view, token);
		await rotateRun.WaitAsync(waitTimeout, token);
		seekerRun.IsCompleted.Should().BeFalse("Rotate does not animate the scale, so it does not end Heartbeat");
		await seekerRun.WaitAsync(waitTimeout, token);
		var elapsedMillisecondsWhenTheSeekerEnded = view.GetElapsedAnimationMilliseconds() - elapsedMillisecondsBeforeTheSeeker;
		await WaitForTickerFramesAsync(view, 3, token);

		elapsedMillisecondsWhenTheSeekerEnded.Should().BeGreaterThanOrEqualTo(seekerLength);
		seekerRun.IsCompletedSuccessfully.Should().BeTrue();
		AssertValuesEqual(view, startingValues);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task SeekerStartedWhileTheAppsToAnimationThatTookOverRuns_StopsItAndReturnsToTheValueItStoppedAt() => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		var (firstSeekerRun, appsAnimation, _) = await StartTheAppsAnimationDuringASeekerAsync(view, nameof(TadaAnimation), nameof(MauiViewExtensions.ScaleToAsync), (_, _) => 1.6, 10 * appsAnimationLength, token);
		await WaitForFramesAsync(view, 3, token);

		var scaleWhenTheSecondSeekerStarts = view.Scale;
		var secondSeekerRun = CreateSeeker(nameof(HeartbeatAnimation)).Animate(view, token);

		(await appsAnimation.WaitAsync(waitTimeout, token)).Should().BeTrue("a seeker stops the app's ScaleToAsync when it starts");
		await Task.WhenAll(firstSeekerRun, secondSeekerRun).WaitAsync(waitTimeout, token);
		await WaitForTickerFramesAsync(view, 3, token);

		AssertValuesEqual(view, WithValue(startingValues, VisualElement.ScaleProperty, scaleWhenTheSecondSeekerStarts));
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task ScaleAnimation_AfterTheAppsScaleToAsyncTookOverFromASeeker_ReturnsToTheAppsTarget() => RunOnSingleThreadAsync(async token =>
	{
		const double appsTarget = 1.6;
		var view = CreateAnimatedViewWithNonDefaultValues();
		var (seekerRun, appsAnimation, _) = await StartTheAppsAnimationDuringASeekerAsync(view, nameof(HeartbeatAnimation), nameof(MauiViewExtensions.ScaleToAsync), (_, _) => appsTarget, appsAnimationLength, token);
		await appsAnimation.WaitAsync(waitTimeout, token);
		await seekerRun.WaitAsync(waitTimeout, token);

		await new ScaleAnimation { Scale = 2, Length = functionalTestLength }.Animate(view, token).WaitAsync(waitTimeout, token);
		await WaitForTickerFramesAsync(view, 3, token);

		view.Scale.Should().BeApproximately(appsTarget, precision, "the app's ScaleToAsync left the view at its target, so that was the scale at rest when Scale started");
	}, TestContext.Current.CancellationToken);

	// The scale the app held is the scale the seeker reached, which only HandedOverToApp tells apart from a value an interrupted run left behind
	[Fact(Timeout = (int)TestDuration.Long)]
	public Task ScaleAnimation_AfterTheAppsScaleToAsyncHeldTheScaleASeekerReached_ReturnsToThatScale() => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var (seekerRun, appsAnimation, appsStart) = await StartTheAppsAnimationDuringASeekerAsync(view, nameof(HeartbeatAnimation), nameof(MauiViewExtensions.ScaleToAsync), (_, current) => current, appsAnimationLength, token);
		await appsAnimation.WaitAsync(waitTimeout, token);
		await seekerRun.WaitAsync(waitTimeout, token);

		await new ScaleAnimation { Scale = 2, Length = functionalTestLength }.Animate(view, token).WaitAsync(waitTimeout, token);
		await WaitForTickerFramesAsync(view, 3, token);

		view.Scale.Should().BeApproximately(appsStart, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task AppsScaleToAsync_StartedWhileAnAnimationBehaviorsSeekerRuns_TakesOverAndKeepsItsTarget() => RunOnSingleThreadAsync(async token =>
	{
		const double appsTarget = 1.6;
		var animation = new ObservedAnimation(new TadaAnimation { Length = seekerLength });
		var behavior = new AnimationBehavior { AnimationType = animation };
		var view = CreateAnimatedViewWithNonDefaultValues();
		view.Behaviors.Add(behavior);
		var startingValues = CaptureValues(view);

		var firstChange = WaitForAnyChangeAsync(view, startingValues, token);
		behavior.AnimateCommand.Execute(token);
		await firstChange;
		await WaitForFramesAsync(view, 2, token);

		var appsAnimation = view.ScaleToAsync(appsTarget, appsAnimationLength, Easing.Linear, token);
		await animation.WaitForEndedAnimationsAsync(1, token);
		(await appsAnimation.WaitAsync(waitTimeout, token)).Should().BeFalse();
		await WaitForTickerFramesAsync(view, 3, token);

		AssertValuesEqual(view, WithValue(startingValues, VisualElement.ScaleProperty, appsTarget));
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task SystemAnimationsSwitchedOffAfterTheAppsScaleToAsyncTookOverFromASeeker_LeaveTheAppsTarget() => RunOnSingleThreadAsync(async token =>
	{
		const double appsTarget = 1.6;
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		var (seekerRun, appsAnimation, _) = await StartTheAppsAnimationDuringASeekerAsync(view, nameof(TadaAnimation), nameof(MauiViewExtensions.ScaleToAsync), (_, _) => appsTarget, 10 * appsAnimationLength, token);
		await WaitForFramesAsync(view, 3, token);

		view.DisableSystemAnimations();
		await appsAnimation.WaitAsync(waitTimeout, token);
		await seekerRun.WaitAsync(waitTimeout, token);

		AssertValuesEqual(view, WithValue(startingValues, VisualElement.ScaleProperty, appsTarget));
	}, TestContext.Current.CancellationToken);

	static async Task<(Task SeekerRun, Task<bool> AppsAnimation, double AppsStart)> StartTheAppsAnimationDuringASeekerAsync(Label view, string seekerName, string appsAnimationName,
		Func<double, double, double> chooseTarget, uint appsLength, CancellationToken token, CancellationToken? appsToken = null)
	{
		var property = PropertyOf(appsAnimationName);
		var original = (double)view.GetValue(property);
		var frames = 0;
		var appsStart = double.NaN;
		var appsAnimationStarted = new TaskCompletionSource<Task<bool>>(TaskCreationOptions.RunContinuationsAsynchronously);
		Task seekerRun;
		Task<bool> appsAnimation;

		using (view.ObserveAnimationFrames(_ =>
		{
			if (++frames == framesBeforeTheAppsAnimation)
			{
				appsStart = (double)view.GetValue(property);
				appsAnimationStarted.SetResult(StartAppsAnimation(view, appsAnimationName, chooseTarget(original, appsStart), appsLength, appsToken ?? token));
			}
		}))
		{
			seekerRun = CreateSeeker(seekerName).Animate(view, token);
			appsAnimation = await appsAnimationStarted.Task.WaitAsync(waitTimeout, token);
		}

		appsStart.Should().NotBe(original, "the seeker has moved the property when the app's animation starts");

		return (seekerRun, appsAnimation, appsStart);
	}

	// On the far side of the value the app's animation starts from, so a write of the original value stands out from every value the app's animation writes
	static double TargetAwayFromTheOriginal(BindableProperty property, double original, double current)
	{
		var distance = property == VisualElement.RotationProperty ? 90 : 0.6;
		return current + (current >= original ? distance : -distance);
	}

	static double[] AppsFrameValues(double appsStart, double appsTarget) => [.. Enumerable.Range(0, appsAnimationFrames + 1).Select(step => appsStart + ((appsTarget - appsStart) * step / appsAnimationFrames))];

	static bool IsOneOf(double value, double[] allowedValues) => allowedValues.Any(allowedValue => Math.Abs(allowedValue - value) <= precision);

	static int IndexOf(BindableProperty property) => Array.FindIndex(animatableProperties, animatable => animatable.Property == property);

	static double[] WithValue(double[] values, BindableProperty property, double value)
	{
		var result = values.ToArray();
		result[IndexOf(property)] = value;
		return result;
	}

	static Task<bool> StartAppsAnimation(VisualElement view, string appsAnimationName, double target, uint length, CancellationToken token) => appsAnimationName switch
	{
		nameof(MauiViewExtensions.ScaleToAsync) => view.ScaleToAsync(target, length, Easing.Linear, token),
		nameof(MauiViewExtensions.RelScaleToAsync) => view.RelScaleToAsync(target - view.Scale, length, Easing.Linear, token),
		nameof(MauiViewExtensions.ScaleXToAsync) => view.ScaleXToAsync(target, length, Easing.Linear, token),
		nameof(MauiViewExtensions.ScaleYToAsync) => view.ScaleYToAsync(target, length, Easing.Linear, token),
		nameof(MauiViewExtensions.RotateToAsync) => view.RotateToAsync(target, length, Easing.Linear, token),
		nameof(MauiViewExtensions.RelRotateToAsync) => view.RelRotateToAsync(target - view.Rotation, length, Easing.Linear, token),
		_ => throw new NotSupportedException(appsAnimationName)
	};

	static BindableProperty PropertyOf(string appsAnimationName) => appsAnimationName switch
	{
		nameof(MauiViewExtensions.ScaleToAsync) or nameof(MauiViewExtensions.RelScaleToAsync) => VisualElement.ScaleProperty,
		nameof(MauiViewExtensions.ScaleXToAsync) => VisualElement.ScaleXProperty,
		nameof(MauiViewExtensions.ScaleYToAsync) => VisualElement.ScaleYProperty,
		nameof(MauiViewExtensions.RotateToAsync) or nameof(MauiViewExtensions.RelRotateToAsync) => VisualElement.RotationProperty,
		_ => throw new NotSupportedException(appsAnimationName)
	};

	static BindableProperty[] SeekerPropertiesOf(string seekerName) => seekerName switch
	{
		nameof(HeartbeatAnimation) => [VisualElement.ScaleProperty],
		nameof(TadaAnimation) => [VisualElement.ScaleProperty, VisualElement.RotationProperty],
		nameof(RubberBandAnimation) => [VisualElement.ScaleXProperty, VisualElement.ScaleYProperty],
		nameof(SwingAnimation) => [VisualElement.RotationProperty, VisualElement.AnchorYProperty],
		nameof(WobbleAnimation) => [VisualElement.TranslationXProperty, VisualElement.RotationProperty],
		nameof(BounceAnimation) => [VisualElement.TranslationYProperty],
		nameof(ShakeAnimation) => [VisualElement.TranslationXProperty],
		_ => throw new NotSupportedException(seekerName)
	};

	static BaseAnimation CreateSeeker(string seekerName) => seekerName switch
	{
		nameof(HeartbeatAnimation) => new HeartbeatAnimation { Length = seekerLength },
		nameof(TadaAnimation) => new TadaAnimation { Length = seekerLength },
		nameof(RubberBandAnimation) => new RubberBandAnimation { Length = seekerLength },
		nameof(SwingAnimation) => new SwingAnimation { Length = seekerLength },
		nameof(WobbleAnimation) => new WobbleAnimation { Length = seekerLength },
		nameof(BounceAnimation) => new BounceAnimation { Length = seekerLength },
		nameof(ShakeAnimation) => new ShakeAnimation { Length = seekerLength },
		_ => throw new NotSupportedException(seekerName)
	};
}
