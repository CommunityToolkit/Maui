using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

// A "To" animation and an attention seeker that write the same property would fight over it every frame
public class ToAnimationDuringAttentionSeekerTests : BaseAnimationTest
{
	const double originalAnchorY = 0.7;
	const uint seekerLength = 2000;

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task ScaleAnimation_StartedWhileHeartbeatRuns_StopsHeartbeatAndLeavesTheViewAtItsOriginalValues() => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		var firstChange = WaitForAnyChangeAsync(view, startingValues, token);
		var heartbeatRun = new HeartbeatAnimation { Length = seekerLength }.Animate(view, token);
		await firstChange;

		var scaleRun = new ScaleAnimation { Scale = 1.6, Length = functionalTestLength }.Animate(view, token);

		await heartbeatRun.WaitAsync(waitTimeout, token);
		heartbeatRun.IsCompletedSuccessfully.Should().BeTrue();
		scaleRun.IsCompleted.Should().BeFalse("Heartbeat ends when Scale starts, not at its own end");
		await scaleRun.WaitAsync(waitTimeout, token);
		await WaitForTickerFramesAsync(view, 5, token);

		AssertValuesEqual(view, startingValues);
		view.AnimationIsRunning(KeyframeAnimationExtensions.AnimationHandle).Should().BeFalse();
	}, TestContext.Current.CancellationToken);

	[Theory(Timeout = (int)TestDuration.Long)]
	[InlineData(nameof(SwingAnimation))]
	[InlineData(nameof(TadaAnimation))]
	[InlineData(nameof(WobbleAnimation))]
	public Task RotateAnimation_StartedWhileASeekerRotatesTheView_StopsItAndTurnsRelativeToTheOriginalRotation(string seekerName) => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		var originalRotation = view.Rotation;
		var firstChange = WaitForAnyChangeAsync(view, startingValues, token);
		var seekerRun = CreateSeeker(seekerName).Animate(view, token);
		await firstChange;

		using var rotations = new ExtremeValueRecorder(view, VisualElement.RotationProperty);
		var rotateRun = new RotateAnimation { Rotation = 180, Length = functionalTestLength }.Animate(view, token);

		view.AnchorY.Should().Be(originalAnchorY, "stopping the seeker returns the anchor it moved before Rotate starts");
		await seekerRun.WaitAsync(waitTimeout, token);
		seekerRun.IsCompletedSuccessfully.Should().BeTrue();
		rotateRun.IsCompleted.Should().BeFalse("the seeker ends when Rotate starts, not at its own end");
		await rotateRun.WaitAsync(waitTimeout, token);
		await WaitForTickerFramesAsync(view, 5, token);

		rotations.Max.Should().BeApproximately(originalRotation + 180, precision);
		AssertValuesEqual(view, startingValues);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task FadeAnimation_StartedWhileTadaRuns_LeavesTadaRunningToItsFullLength() => RunOnSingleThreadAsync(async token =>
	{
		const uint tadaLength = 640;
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		var firstChange = WaitForAnyChangeAsync(view, startingValues, token);
		var elapsedMillisecondsBeforeTada = view.GetElapsedAnimationMilliseconds();
		var tadaRun = new TadaAnimation { Length = tadaLength }.Animate(view, token);
		await firstChange;

		await new FadeAnimation { Opacity = 0.2, Length = 96 }.Animate(view, token).WaitAsync(waitTimeout, token);
		tadaRun.IsCompleted.Should().BeFalse("Tada animates other properties than the opacity, so Fade does not stop it");
		await tadaRun.WaitAsync(waitTimeout, token);

		(view.GetElapsedAnimationMilliseconds() - elapsedMillisecondsBeforeTada).Should().BeGreaterThanOrEqualTo(tadaLength);
		AssertValuesEqual(view, startingValues);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task ScaleAnimation_StartedWhileASeekerRunsThatAnotherSeekersStartAlsoStarted_StopsTheSeekerItEnded() => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		Task? seekerStartedDuringTheStart = null;
		var appRotation = view.RotateToAsync(view.Rotation, seekerLength, Easing.Linear, token);
		_ = appRotation.ContinueWith(_ => seekerStartedDuringTheStart = new TadaAnimation { Length = seekerLength }.Animate(view, token), token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

		// Tada's start aborts the app's RotateToAsync, whose continuation starts a seeker that Tada's own tween then aborts, after Tada recorded its run
		var seekerRun = new TadaAnimation { Length = seekerLength }.Animate(view, token);
		var scaleRun = new ScaleAnimation { Scale = 1.6, Length = functionalTestLength }.Animate(view, token);

		await seekerRun.WaitAsync(waitTimeout, token);
		scaleRun.IsCompleted.Should().BeFalse("the seeker ends when Scale starts, not at its own end");
		await Task.WhenAll(seekerStartedDuringTheStart ?? throw new InvalidOperationException("The aborted RotateToAsync did not start a seeker."), scaleRun).WaitAsync(waitTimeout, token);
		await WaitForTickerFramesAsync(view, 5, token);

		AssertValuesEqual(view, startingValues);
	}, TestContext.Current.CancellationToken);

	[Theory(Timeout = (int)TestDuration.Long)]
	[InlineData(nameof(ScaleAnimation), nameof(HeartbeatAnimation))]
	[InlineData(nameof(RotateAnimation), nameof(SwingAnimation))]
	public Task ToAnimation_StartedWhileTheContinuationOfTheAppsOwnToAnimationStartsASeeker_StopsThatSeeker(string toAnimationName, string seekerName) => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		Task? seekerStartedByTheApp = null;
		var appCode = AnimateThenStartASeekerAsync();

		// The To animation's start aborts the app's animation, whose continuation starts the seeker inside that abort
		var toRun = CreateToAnimation(toAnimationName).Animate(view, token);
		var seekerRun = seekerStartedByTheApp ?? throw new InvalidOperationException("The aborted app animation did not start a seeker.");

		await seekerRun.WaitAsync(waitTimeout, token);
		seekerRun.IsCompletedSuccessfully.Should().BeTrue();
		toRun.IsCompleted.Should().BeFalse("the seeker ends when the To animation starts, not at its own end");
		await Task.WhenAll(toRun, appCode).WaitAsync(waitTimeout, token);
		await WaitForTickerFramesAsync(view, 5, token);

		AssertValuesEqual(view, startingValues);

		async Task AnimateThenStartASeekerAsync()
		{
			await (toAnimationName is nameof(ScaleAnimation)
				? view.ScaleToAsync(view.Scale, seekerLength, Easing.Linear, token)
				: view.RotateToAsync(view.Rotation, seekerLength, Easing.Linear, token));
			seekerStartedByTheApp = (seekerName is nameof(HeartbeatAnimation) ? new HeartbeatAnimation { Length = seekerLength } : CreateSeeker(seekerName)).Animate(view, token);
		}
	}, TestContext.Current.CancellationToken);

	static BaseAnimation CreateToAnimation(string toAnimationName) => toAnimationName switch
	{
		nameof(ScaleAnimation) => new ScaleAnimation { Scale = 1.6, Length = functionalTestLength },
		nameof(RotateAnimation) => new RotateAnimation { Rotation = 180, Length = functionalTestLength },
		_ => throw new NotSupportedException(toAnimationName)
	};

	static BaseAnimation CreateSeeker(string seekerName) => seekerName switch
	{
		nameof(SwingAnimation) => new SwingAnimation { Length = seekerLength },
		nameof(TadaAnimation) => new TadaAnimation { Length = seekerLength },
		nameof(WobbleAnimation) => new WobbleAnimation { Length = seekerLength },
		_ => throw new NotSupportedException(seekerName)
	};
}