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

		view.AnchorY.Should().Be(originalAnchorY, "stopping Swing returns its anchor before Rotate starts");
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

	static BaseAnimation CreateSeeker(string seekerName) => seekerName switch
	{
		nameof(SwingAnimation) => new SwingAnimation { Length = seekerLength },
		nameof(TadaAnimation) => new TadaAnimation { Length = seekerLength },
		nameof(WobbleAnimation) => new WobbleAnimation { Length = seekerLength },
		_ => throw new NotSupportedException(seekerName)
	};
}