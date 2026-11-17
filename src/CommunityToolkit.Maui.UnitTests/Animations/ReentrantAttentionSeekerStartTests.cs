using System.ComponentModel;
using CommunityToolkit.Maui.Animations;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

// App code that runs while an attention seeker starts can start another seeker, which the starting seeker's own tween then aborts
public class ReentrantAttentionSeekerStartTests : BaseAnimationTest
{
	const double originalAnchorY = 0.7;
	const uint seekerLength = 2000;

	[Theory(Timeout = (int)TestDuration.Long)]
	[InlineData(nameof(HeartbeatAnimation))]
	[InlineData(nameof(SwingAnimation))]
	public Task SwingAnimation_WhoseAnchorChangeStartsAnotherSeeker_KeepsItsTopEdgeAnchorAndRotateAnimationStillStopsIt(string seekerStartedByTheAnchorChange) => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		Task? seekerStartedDuringSwingsStart = null;
		view.PropertyChanged += StartASeekerWhenTheAnchorMovesToTheTopEdge;
		Task swingRun;

		try
		{
			// Swing moves the anchor while it creates its keyframes, before its tween starts and aborts the seeker the handler started
			swingRun = new SwingAnimation { Length = seekerLength }.Animate(view, token);
		}
		finally
		{
			view.PropertyChanged -= StartASeekerWhenTheAnchorMovesToTheTopEdge;
		}

		await (seekerStartedDuringSwingsStart ?? throw new InvalidOperationException("The anchor change did not start a seeker.")).WaitAsync(waitTimeout, token);
		await WaitForTickerFramesAsync(view, 3, token);

		swingRun.IsCompleted.Should().BeFalse();
		view.AnchorY.Should().Be(0, "the running Swing pivots on the view's top edge");

		var rotateRun = new RotateAnimation { Rotation = 180, Length = functionalTestLength }.Animate(view, token);

		view.AnchorY.Should().Be(originalAnchorY, "Rotate stops the running Swing, which returns the anchor before Rotate starts");
		await swingRun.WaitAsync(waitTimeout, token);
		rotateRun.IsCompleted.Should().BeFalse("Swing ends when Rotate starts, not at its own end");
		await rotateRun.WaitAsync(waitTimeout, token);
		await WaitForTickerFramesAsync(view, 5, token);

		AssertValuesEqual(view, startingValues);

		void StartASeekerWhenTheAnchorMovesToTheTopEdge(object? sender, PropertyChangedEventArgs e)
		{
			if (seekerStartedDuringSwingsStart is null && e.PropertyName == VisualElement.AnchorYProperty.PropertyName && view.AnchorY == 0)
			{
				seekerStartedDuringSwingsStart = CreateSeeker(seekerStartedByTheAnchorChange).Animate(view, token);
			}
		}
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task SwingAnimation_WhoseStartAbortsARotateToAsyncWhoseContinuationStartsAnotherSwing_KeepsItsTopEdgeAnchorUntilItEnds() => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);
		Task? swingStartedDuringTheStart = null;
		var appRotation = view.RotateToAsync(view.Rotation, seekerLength, Easing.Linear, token);
		_ = appRotation.ContinueWith(_ => swingStartedDuringTheStart = new SwingAnimation { Length = seekerLength }.Animate(view, token), token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

		// The Swing's start aborts the app's RotateToAsync, whose continuation starts a Swing that the first Swing's own tween then aborts.
		var swingRun = new SwingAnimation { Length = 3 * functionalTestLength }.Animate(view, token);

		await (swingStartedDuringTheStart ?? throw new InvalidOperationException("The aborted RotateToAsync did not start a Swing.")).WaitAsync(waitTimeout, token);
		await WaitForTickerFramesAsync(view, 3, token);

		swingRun.IsCompleted.Should().BeFalse();
		view.AnchorY.Should().Be(0, "the running Swing pivots on the view's top edge");

		await swingRun.WaitAsync(waitTimeout, token);
		await WaitForTickerFramesAsync(view, 5, token);

		AssertValuesEqual(view, startingValues);
	}, TestContext.Current.CancellationToken);

	static BaseAnimation CreateSeeker(string seekerName) => seekerName switch
	{
		nameof(HeartbeatAnimation) => new HeartbeatAnimation { Length = seekerLength },
		nameof(SwingAnimation) => new SwingAnimation { Length = seekerLength },
		_ => throw new NotSupportedException(seekerName)
	};
}
