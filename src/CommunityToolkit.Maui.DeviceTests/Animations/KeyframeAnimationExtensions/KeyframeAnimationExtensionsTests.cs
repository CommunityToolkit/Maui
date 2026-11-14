using System.ComponentModel;
using CommunityToolkit.Maui.Animations;

namespace CommunityToolkit.Maui.DeviceTests;

// Every attention seeker runs under one animation handle, so starting one aborts the running one, which returns the view first
[Category(TestCategory.Animations)]
[Collection(TestCollections.DisableParallelization)]
public class KeyframeAnimationExtensionsTests : AnimationTestBase
{
	const double valueTolerance = 0.001;
	const double originalRotation = 10;
	const double originalAnchorY = 0.25;
	const double originalTranslationX = 5;
	const double originalTranslationY = 5;
	const double swingAngle = 15;
	const double wobbleDistance = 25;
	const double wobbleRotationAngle = 5;
	const double rotateBy = 180;
	const double translateTargetX = 60;
	const double bounceDistance = 30;

	// Swing animates Rotation and AnchorY, Wobble TranslationX and Rotation: each run has a property of its own and one they share
	static readonly IReadOnlyList<(BindableProperty Property, double Value)> originalValues =
		[(VisualElement.RotationProperty, originalRotation), (VisualElement.AnchorYProperty, originalAnchorY), (VisualElement.TranslationXProperty, originalTranslationX)];

	static readonly IReadOnlyList<(BindableProperty Property, double Value)> rotationAndAnchor =
		[(VisualElement.RotationProperty, originalRotation), (VisualElement.AnchorYProperty, originalAnchorY)];

	[Fact]
	public async Task AttentionSeekerStartedWhileAnotherRunsReturnsBothPropertySetsToTheirOriginalValues()
	{
		SetupBuilder();

		var view = CreateAnimatedView(originalValues);
		var swing = new SwingAnimation { RotationAngle = swingAngle, Length = runLength };
		var wobble = new WobbleAnimation { Distance = wobbleDistance, RotationAngle = wobbleRotationAngle, Length = runLength };

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			var swinging = WaitForValueAsync(view, VisualElement.RotationProperty, rotation => rotation >= originalRotation + (swingAngle / 3));
			var swingRun = swing.Animate(view);
			await swinging;

			var wobblePeak = WaitForValueAsync(view, VisualElement.TranslationXProperty, translationX => Math.Abs(translationX - (originalTranslationX - wobbleDistance)) <= valueTolerance);
			var wobbleTiltPeak = WaitForValueAsync(view, VisualElement.RotationProperty, rotation => Math.Abs(rotation - (originalRotation - wobbleRotationAngle)) <= valueTolerance);
			var rotationsWrittenByStartingWobble = new List<double>();
			view.PropertyChanged += RecordRotation;
			try
			{
				var wobbleRun = wobble.Animate(view);
				view.PropertyChanged -= RecordRotation;

				// Android's ValueAnimator.start() ticks the new run inside Animate, so Rotation may already be a few milliseconds into Wobble.
				// The first write is exact: Swing's abort returned the view before Wobble read its original values.
				Assert.True(rotationsWrittenByStartingWobble.Count > 0 && rotationsWrittenByStartingWobble[0] == originalRotation, $"Swing's abort wrote the rotations {string.Join(", ", rotationsWrittenByStartingWobble)}; the first must be the original {originalRotation}.");
				Assert.True(view.AnchorY == originalAnchorY, $"AnchorY is {view.AnchorY} right after Wobble started, expected the original {originalAnchorY}, which only Swing's abort restores.");

				await swingRun.WaitAsync(waitTimeout);
				Assert.True(swingRun.IsCompletedSuccessfully, $"Swing ended {swingRun.Status}; a replaced attention seeker completes normally.");

				await wobbleRun.WaitAsync(waitTimeout);
				await Task.WhenAll(wobblePeak, wobbleTiltPeak);
				AssertAtOriginalValues(handler, view, originalValues, nativeStateAtRest, "after Wobble completed");
			}
			finally
			{
				view.PropertyChanged -= RecordRotation;
			}

			void RecordRotation(object? sender, PropertyChangedEventArgs e)
			{
				if (e.PropertyName == VisualElement.RotationProperty.PropertyName)
				{
					rotationsWrittenByStartingWobble.Add(view.Rotation);
				}
			}
		});
	}

	[Fact]
	public async Task AttentionSeekerStartedWhileALongerRotateAnimationRunsStopsItAndReturnsTheViewToItsOriginalValues()
	{
		SetupBuilder();

		var view = CreateAnimatedView(rotationAndAnchor);
		var rotate = new RotateAnimation { Rotation = rotateBy, Length = 2 * runLength };
		var swing = new SwingAnimation { RotationAngle = swingAngle, Length = runLength };

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			var rotating = WaitForValueAsync(view, VisualElement.RotationProperty, rotation => rotation >= originalRotation + 20);
			var rotateRun = rotate.Animate(view);
			await rotating;

			var swingPeak = WaitForValueAsync(view, VisualElement.RotationProperty, rotation => Math.Abs(rotation - (originalRotation + swingAngle)) <= valueTolerance);
			var restoreWrites = 0;
			var swingPeakReached = false;
			var swingAnimateReturned = false;
			view.PropertyChanged += CountRestoreWritesUntilSwingPeaks;
			try
			{
				var swingRun = swing.Animate(view);
				swingAnimateReturned = true;

				await Task.WhenAll(rotateRun, swingRun).WaitAsync(waitTimeout);
				await swingPeak;
			}
			finally
			{
				view.PropertyChanged -= CountRestoreWritesUntilSwingPeaks;
			}

			Assert.True(restoreWrites is 0, $"The Rotate animation wrote its original rotation {restoreWrites} time(s) after Swing took over; Swing must take over without a restore write.");

			await AssertStaysAtOriginalValuesAsync(handler, view, rotationAndAnchor, nativeStateAtRest, "after Swing replaced the Rotate animation");

			// A restore the Rotate animation made after Swing took over would be posted, landing after Animate returned; Swing's own frames reach the original rotation only at its end.
			void CountRestoreWritesUntilSwingPeaks(object? sender, PropertyChangedEventArgs e)
			{
				if (!swingAnimateReturned || swingPeakReached || e.PropertyName != VisualElement.RotationProperty.PropertyName)
				{
					return;
				}

				var rotation = view.Rotation;
				if (rotation == originalRotation)
				{
					restoreWrites++;
				}

				swingPeakReached = Math.Abs(rotation - (originalRotation + swingAngle)) <= valueTolerance;
			}
		});
	}

	[Fact]
	public async Task BounceStartedWhileTranslateToRunsLeavesTheTranslateRunningToItsTarget()
	{
		SetupBuilder();

		var view = CreateAnimatedView([(VisualElement.TranslationXProperty, originalTranslationX), (VisualElement.TranslationYProperty, originalTranslationY)]);
		var bounce = new BounceAnimation { Distance = bounceDistance, Length = runLength };

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			var translating = WaitForValueAsync(view, VisualElement.TranslationXProperty, translationX => translationX >= originalTranslationX + 10);
			var translateRun = view.TranslateToAsync(translateTargetX, originalTranslationY, 3 * runLength, Easing.Linear);
			await translating;

			var bouncePeak = WaitForValueAsync(view, VisualElement.TranslationYProperty, translationY => Math.Abs(translationY - (originalTranslationY - bounceDistance)) <= valueTolerance);
			var bounceRun = bounce.Animate(view);

			await bounceRun.WaitAsync(waitTimeout);
			await bouncePeak;

			// TranslateToAsync reports true when something interrupted it
			var translateWasInterrupted = await translateRun.WaitAsync(waitTimeout);
			Assert.False(translateWasInterrupted, "Bounce interrupted the TranslateToAsync that was running.");

			Assert.True(Math.Abs(view.TranslationX - translateTargetX) <= valueTolerance, $"TranslationX is {view.TranslationX}, expected the target {translateTargetX}.");
			Assert.True(Math.Abs(view.TranslationY - originalTranslationY) <= valueTolerance, $"TranslationY is {view.TranslationY}, expected its original {originalTranslationY}.");

			var mismatch = FindNativeMismatch(handler, view);
			Assert.True(mismatch is null, mismatch);
		});
	}
}
