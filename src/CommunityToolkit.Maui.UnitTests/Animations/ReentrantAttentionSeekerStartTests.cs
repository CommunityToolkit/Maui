using CommunityToolkit.Maui.Animations;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

// App code that runs while an attention seeker starts can start another seeker, which the starting seeker's own tween then aborts
public class ReentrantAttentionSeekerStartTests : BaseAnimationTest
{
	const uint seekerLength = 2000;

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task SwingAnimation_WhoseStartAbortsARotateToAsyncWhoseContinuationStartsAnotherSwing_KeepsItsTopEdgeAnchorUntilItEnds() => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		Task? swingStartedDuringTheStart = null;
		var appRotation = view.RotateToAsync(view.Rotation, seekerLength, Easing.Linear, token);
		_ = appRotation.ContinueWith(_ => swingStartedDuringTheStart = new SwingAnimation { Length = seekerLength }.Animate(view, token), token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

		// The Swing's start aborts the app's RotateToAsync, whose continuation starts a Swing that the first Swing's own tween then aborts
		var swingRun = new SwingAnimation { Length = seekerLength }.Animate(view, token);

		await (swingStartedDuringTheStart ?? throw new InvalidOperationException("The aborted RotateToAsync did not start a Swing.")).WaitAsync(waitTimeout, token);
		await WaitForTickerFramesAsync(view, 3, token);

		swingRun.IsCompleted.Should().BeFalse();
		view.AnchorY.Should().Be(0, "the running Swing pivots on the view's top edge");

		await swingRun.WaitAsync(waitTimeout, token);
		await WaitForTickerFramesAsync(view, 5, token);

		AssertValuesEqual(view, startingValues);
	}, TestContext.Current.CancellationToken);
}
