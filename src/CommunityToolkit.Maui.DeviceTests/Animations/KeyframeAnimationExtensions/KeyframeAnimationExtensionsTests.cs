using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Maui.Animations;

namespace CommunityToolkit.Maui.DeviceTests;

// Every attention seeker runs under one animation handle, so starting one aborts the running one, which returns the view first
[Category(TestCategory.KeyframeAnimationExtensions)]
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
	const double translateTargetY = 45;
	const double bounceDistance = 30;
	const double originalScale = 0.8;
	const double originalScaleX = 0.9;
	const double originalScaleY = 1.2;
	const string throwingHandlerMessage = "Thrown by a PropertyChanged handler.";

	// So long that only the test ends the seeker within the wait, never the seeker's own end, also when the UI thread stalls for seconds
	const uint seekerLengthFarBeyondTheWait = 60_000;

	// Far longer than any run it times
	const uint tickerTimeSamplerLength = 60_000;

	// A run that has had its whole length may have ended on its own; a frame can arrive this late on a slow emulator
	static readonly TimeSpan slowestFrameInterval = TimeSpan.FromMilliseconds(100);

	// Swing animates Rotation and AnchorY, Wobble TranslationX and Rotation: each run has a property of its own and one they share
	static readonly IReadOnlyList<(BindableProperty Property, double Value)> originalValues =
		[(VisualElement.RotationProperty, originalRotation), (VisualElement.AnchorYProperty, originalAnchorY), (VisualElement.TranslationXProperty, originalTranslationX)];

	static readonly IReadOnlyList<(BindableProperty Property, double Value)> rotationAndAnchor =
		[(VisualElement.RotationProperty, originalRotation), (VisualElement.AnchorYProperty, originalAnchorY)];

	static readonly IReadOnlyList<(BindableProperty Property, double Value)> rotationAnchorAndScale =
		[(VisualElement.RotationProperty, originalRotation), (VisualElement.AnchorYProperty, originalAnchorY), (VisualElement.ScaleProperty, originalScale)];

	static readonly IReadOnlyList<(BindableProperty Property, double Value)> scaleAndRotation =
		[(VisualElement.ScaleProperty, originalScale), (VisualElement.RotationProperty, originalRotation)];

	static readonly IReadOnlyList<(BindableProperty Property, double Value)> scaleOnly =
		[(VisualElement.ScaleProperty, originalScale)];

	static readonly IReadOnlyList<(BindableProperty Property, double Value)> scaleXAndScaleY =
		[(VisualElement.ScaleXProperty, originalScaleX), (VisualElement.ScaleYProperty, originalScaleY)];

	static readonly IReadOnlyList<(BindableProperty Property, double Value)> translationXAndRotation =
		[(VisualElement.TranslationXProperty, originalTranslationX), (VisualElement.RotationProperty, originalRotation)];

	static readonly IReadOnlyList<(BindableProperty Property, double Value)> scaleRotationAndTranslationY =
		[(VisualElement.ScaleProperty, originalScale), (VisualElement.RotationProperty, originalRotation), (VisualElement.TranslationYProperty, originalTranslationY)];

	[Fact]
	public async Task AttentionSeekerStartedWhileAnotherRunsReturnsBothPropertySetsToTheirOriginalValues()
	{
		SetupBuilder();

		var view = CreateAnimatedView(originalValues);
		var swing = new SwingAnimation { RotationAngle = swingAngle, Length = runLength };
		var wobble = new WobbleAnimation { Distance = wobbleDistance, RotationAngle = wobbleRotationAngle, Length = runLength };

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			Task? wobblePeak = null;
			Task? wobbleTiltPeak = null;
			Task? wobbleRun = null;
			var rotationsWrittenByStartingWobble = new List<double>();
			try
			{
				var swingRun = await StartAndActMidRunAsync(view, VisualElement.RotationProperty, rotation => rotation >= originalRotation + (swingAngle / 3), () => swing.Animate(view), () =>
				{
					wobblePeak = WaitForValueAsync(view, VisualElement.TranslationXProperty, translationX => Math.Abs(translationX - (originalTranslationX - wobbleDistance)) <= valueTolerance);
					wobbleTiltPeak = WaitForValueAsync(view, VisualElement.RotationProperty, rotation => Math.Abs(rotation - (originalRotation - wobbleRotationAngle)) <= valueTolerance);
					view.PropertyChanged += RecordRotation;
					wobbleRun = wobble.Animate(view);
					view.PropertyChanged -= RecordRotation;

					// Android's ValueAnimator.start() ticks the new run inside Animate, so Rotation may already be a few milliseconds into Wobble.
					// The first write is exact: Swing's abort returned the view before Wobble read its original values.
					Assert.True(rotationsWrittenByStartingWobble.Count > 0 && rotationsWrittenByStartingWobble[0] == originalRotation, $"Swing's abort wrote the rotations {string.Join(", ", rotationsWrittenByStartingWobble)}; the first must be the original {originalRotation}.");
					Assert.True(view.AnchorY == originalAnchorY, $"AnchorY is {view.AnchorY} right after Wobble started, expected the original {originalAnchorY}, which only Swing's abort restores.");
				});

				await swingRun.WaitAsync(waitTimeout);
				Assert.True(swingRun.IsCompletedSuccessfully, $"Swing ended {swingRun.Status}; a replaced attention seeker completes normally.");

				await wobbleRun!.WaitAsync(waitTimeout);
				await Task.WhenAll(wobblePeak!, wobbleTiltPeak!);
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
			Task? swingPeak = null;
			Task? swingRun = null;
			var restoreWrites = 0;
			var swingPeakReached = false;
			var swingAnimateReturned = false;
			try
			{
				var rotateRun = await StartAndActMidRunAsync(view, VisualElement.RotationProperty, rotation => rotation >= originalRotation + 20 && rotation < originalRotation + rotateBy, () => rotate.Animate(view), () =>
				{
					swingPeak = WaitForValueAsync(view, VisualElement.RotationProperty, rotation => Math.Abs(rotation - (originalRotation + swingAngle)) <= valueTolerance);
					view.PropertyChanged += CountRestoreWritesUntilSwingPeaks;
					swingRun = swing.Animate(view);
					swingAnimateReturned = true;
				});

				await Task.WhenAll(rotateRun, swingRun!).WaitAsync(waitTimeout);
				await swingPeak!;
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
			Task? bounceHopsAboveTheTranslation = null;
			Task? bounceRun = null;
			var translateRun = (Task<bool>)await StartAndActMidRunAsync(view, VisualElement.TranslationXProperty, IsTranslatingX,
				() => StartTranslateToFromTheOriginalTranslation(view, translateTargetX, translateTargetY, 3 * runLength), () =>
				{
					// The hops ride on top of the translation, wherever a stalled UI thread has let it get to when the first hop peaks
					bounceHopsAboveTheTranslation = WaitForValueAsync(view, VisualElement.TranslationYProperty, translationY => TranslationYOfTheTranslationAt(view.TranslationX) - translationY >= bounceDistance / 2);
					bounceRun = bounce.Animate(view);
				});

			await bounceRun!.WaitAsync(waitTimeout);
			await bounceHopsAboveTheTranslation!;

			// TranslateToAsync reports true when something interrupted it
			var translateWasInterrupted = await translateRun.WaitAsync(waitTimeout);
			Assert.False(translateWasInterrupted, "Bounce interrupted the TranslateToAsync that was running.");

			AssertTranslationAtTheAppsTarget(view, "after Bounce ended and the TranslateToAsync reached its target");

			var mismatch = FindNativeMismatch(handler, view);
			Assert.True(mismatch is null, mismatch);
		});
	}

	[Fact]
	public async Task BounceOutlastingTranslateToHopsOnTopOfTheTranslationsTargetAndLeavesBothAxesThere()
	{
		SetupBuilder();

		var view = CreateAnimatedView([(VisualElement.TranslationXProperty, originalTranslationX), (VisualElement.TranslationYProperty, originalTranslationY)]);
		var bounce = new BounceAnimation { Distance = bounceDistance, Length = 2 * runLength };

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			Task? firstHopPeak = null;
			Task? bounceRun = null;
			var translateRun = (Task<bool>)await StartAndActMidRunAsync(view, VisualElement.TranslationXProperty, IsTranslatingX,
				() => StartTranslateToFromTheOriginalTranslation(view, translateTargetX, translateTargetY, runLength / 3), () =>
				{
					firstHopPeak = WaitForValueAsync(view, VisualElement.TranslationYProperty, translationY => Math.Abs(translationY - (translateTargetY - bounceDistance)) <= valueTolerance);
					bounceRun = bounce.Animate(view);
				});

			var translateWasInterrupted = await translateRun.WaitAsync(waitTimeout);
			await bounceRun!.WaitAsync(waitTimeout);
			await firstHopPeak!;

			Assert.False(translateWasInterrupted, "Bounce interrupted the TranslateToAsync that was running.");
			AssertTranslationAtTheAppsTarget(view, "after Bounce, which outlasted the TranslateToAsync, ended");

			var mismatch = FindNativeMismatch(handler, view);
			Assert.True(mismatch is null, mismatch);
		});
	}

	[Fact]
	public async Task BounceDuringAHorizontalTranslateToHoldsTheFirstHopsPeakUntilItFalls()
	{
		const string frameSamplerHandle = "CommunityToolkit.Maui.DeviceTests.BounceHeightSampler";

		SetupBuilder();

		var view = CreateAnimatedView([(VisualElement.TranslationXProperty, originalTranslationX), (VisualElement.TranslationYProperty, originalTranslationY)]);
		// The bounce is long enough for its pause at the peak to catch a frame on a slow device
		var bounce = new BounceAnimation { Distance = bounceDistance, Length = 2 * runLength };
		var timesNearTheFirstHopsPeak = FindTimesAHopIsAtLeast(0.9, bounce.Length, riseStart: 0, peakStart: 0.25, fallStart: 0.2875, fallEnd: 0.4125);

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			var frames = new List<BounceFrame>();

			await RepeatAttemptsThatMissedTheRunAsync(async () =>
			{
				frames.Clear();
				Task? bounceRun = null;

				// With half the translation ahead, the TranslateToAsync still writes TranslationY every frame while the bounce pauses at its first peak
				var translateRun = (Task<bool>)await StartAndActMidRunAsync(view, VisualElement.TranslationXProperty, translationX => translationX >= originalTranslationX + 10 && translationX <= (originalTranslationX + translateTargetX) / 2,
					() => StartTranslateToFromTheOriginalTranslation(view, translateTargetX, originalTranslationY, 4 * runLength), () =>
					{
						bounceRun = bounce.Animate(view);

						// Committed right after the bounce, in its turn, the sampler reads the bounce's time; ticking last in every frame, it sees the height that frame shows
						CommitTickerTimeSampler(view, frameSamplerHandle, timeSinceTheBounceStarted =>
							frames.Add(new(timeSinceTheBounceStarted, originalTranslationY - view.TranslationY, view.TranslationX < translateTargetX)));
					});

				try
				{
					await bounceRun!.WaitAsync(waitTimeout);
				}
				finally
				{
					view.AbortAnimation(frameSamplerHandle);
				}

				// A stalled UI thread can carry the bounce past its peak in one frame; only a frame in that span shows whether the peak holds under the TranslateToAsync
				if (!frames.Any(frame => frame.IsTranslating && timesNearTheFirstHopsPeak.Contains(frame.Time)))
				{
					await AssertTheTranslateToEndedAtTheAppsTargetAsync();
					return false;
				}

				var heightPerFrame = frames.Select(frame => frame.Height).ToList();
				var framesAtTheFirstHopsPeak = heightPerFrame.Select((height, frame) => (height, frame)).Where(sample => sample.height >= 0.9 * bounceDistance).Select(sample => sample.frame).ToList();

				Assert.NotEmpty(framesAtTheFirstHopsPeak);
				Assert.True(framesAtTheFirstHopsPeak[^1] - framesAtTheFirstHopsPeak[0] == framesAtTheFirstHopsPeak.Count - 1,
					$"The view left the first hop's peak and came back to it. Height per frame: {string.Join(", ", heightPerFrame.Select(FormatNativeValue))}");

				await AssertTheTranslateToEndedAtTheAppsTargetAsync();
				return true;

				async Task AssertTheTranslateToEndedAtTheAppsTargetAsync()
				{
					var translateWasInterrupted = await translateRun.WaitAsync(waitTimeout);
					Assert.False(translateWasInterrupted, "Bounce interrupted the TranslateToAsync that was running.");
					Assert.True(Math.Abs(view.TranslationX - translateTargetX) <= valueTolerance, $"TranslationX is {view.TranslationX}, expected the app's target {translateTargetX}.");
					Assert.True(Math.Abs(view.TranslationY - originalTranslationY) <= valueTolerance, $"TranslationY is {view.TranslationY}, expected the app's target {originalTranslationY}.");

					var mismatch = FindNativeMismatch(handler, view);
					Assert.True(mismatch is null, mismatch);
				}
			}, () => $"no frame fell within {timesNearTheFirstHopsPeak} of the bounce while the TranslateToAsync ran. Frames: {DescribeFrames(frames)}");
		});
	}

	[Fact]
	public async Task TranslateToStartedDuringABounceKeepsTheHopsVisibleOnTopOfTheTranslation()
	{
		const string bounceClockHandle = "CommunityToolkit.Maui.DeviceTests.BounceClock";
		const string frameSamplerHandle = "CommunityToolkit.Maui.DeviceTests.HeightAboveTheTranslationSampler";

		SetupBuilder();

		var view = CreateAnimatedView([(VisualElement.TranslationXProperty, originalTranslationX), (VisualElement.TranslationYProperty, originalTranslationY)]);
		var bounce = new BounceAnimation { Distance = bounceDistance, Length = 2 * runLength };

		// The third hop never reaches a quarter of the first hop's height
		TimeSpanOfTheBounce[] timesTheHopsShowAQuarterOfTheFirstHop =
		[
			FindTimesAHopIsAtLeast(0.25, bounce.Length, riseStart: 0, peakStart: 0.25, fallStart: 0.2875, fallEnd: 0.4125),
			FindTimesAHopIsAtLeast(0.5, bounce.Length, riseStart: 0.4125, peakStart: 0.625, fallStart: 0.625, fallEnd: 0.75)
		];

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			var frames = new List<BounceFrame>();

			await RepeatAttemptsThatMissedTheRunAsync(async () =>
			{
				frames.Clear();
				var timeSinceTheBounceStarted = 0.0;
				var translateTargetYNow = double.NaN;
				Task<bool>? translateRun = null;
				Task? bounceRunOfThisAttempt = null;
				Task bounceRun;

				try
				{
					// Still rising to the first hop's peak, so the first two hops, the high ones, are still ahead
					bounceRun = await StartAndActMidRunAsync(view, VisualElement.TranslationYProperty,
						translationYs => translationYs[^1] <= originalTranslationY - 1 && translationYs.All(translationY => translationY > originalTranslationY - bounceDistance + valueTolerance),
						() =>
						{
							bounceRunOfThisAttempt = StartBounceFromTheOriginalTranslation(view, bounce);
							CommitTickerTimeSampler(view, bounceClockHandle, time => timeSinceTheBounceStarted = time);
							return bounceRunOfThisAttempt;
						}, () =>
						{
							// Translating to the height the view has now keeps the translation's own TranslationY still, so any height a frame shows comes from the hops
							translateTargetYNow = view.TranslationY;
							translateRun = view.TranslateToAsync(translateTargetX, translateTargetYNow, 2 * runLength, Easing.Linear);

							// Committed after both runs, the sampler ticks last in every frame and so sees the height that frame shows; the clock ticked earlier in the same frame
							new Microsoft.Maui.Controls.Animation(_ =>
							{
								if (!bounceRunOfThisAttempt!.IsCompleted)
								{
									frames.Add(new(timeSinceTheBounceStarted, translateTargetYNow - view.TranslationY, view.TranslationX < translateTargetX));
								}
							}).Commit(view, frameSamplerHandle, length: tickerTimeSamplerLength);
						});

					await bounceRun.WaitAsync(waitTimeout);
				}
				finally
				{
					view.AbortAnimation(frameSamplerHandle);
					view.AbortAnimation(bounceClockHandle);
				}

				// A stalled UI thread can carry the bounce past the hops in one frame; only a frame in their span shows whether they stay on top of the translation
				if (!frames.Any(frame => frame.IsTranslating && timesTheHopsShowAQuarterOfTheFirstHop.Any(times => times.Contains(frame.Time))))
				{
					await AssertTheTranslateToEndedAtTheAppsTargetAsync();
					return false;
				}

				var didTheHopsShow = frames.Any(frame => frame.Height >= bounceDistance / 4);
				Assert.True(didTheHopsShow,
					$"The hops never showed while TranslateToAsync ran. Height above the translation per frame: {string.Join(", ", frames.Select(frame => FormatNativeValue(frame.Height)))}");

				await AssertTheTranslateToEndedAtTheAppsTargetAsync();
				return true;

				async Task AssertTheTranslateToEndedAtTheAppsTargetAsync()
				{
					var translateWasInterrupted = await translateRun!.WaitAsync(waitTimeout);
					Assert.False(translateWasInterrupted, "Bounce interrupted the TranslateToAsync that started during it.");
					Assert.True(Math.Abs(view.TranslationX - translateTargetX) <= valueTolerance, $"TranslationX is {view.TranslationX}, expected the app's target {translateTargetX}.");
					Assert.True(Math.Abs(view.TranslationY - translateTargetYNow) <= valueTolerance, $"TranslationY is {view.TranslationY}, expected the app's target {translateTargetYNow}.");

					var mismatch = FindNativeMismatch(handler, view);
					Assert.True(mismatch is null, mismatch);
				}
			}, () => $"no frame fell within {string.Join(" or ", timesTheHopsShowAQuarterOfTheFirstHop)} of the bounce while the TranslateToAsync ran. Frames: {DescribeFrames(frames)}");
		});
	}

	// Short of the target, so a translation that already ended cannot pass for one in progress
	static bool IsTranslatingX(double translationX) => translationX >= originalTranslationX + 10 && translationX < translateTargetX;

	// TranslateToAsync moves both axes with one linear animation, so the TranslationX of a frame tells the TranslationY the translation itself has in it
	static double TranslationYOfTheTranslationAt(double translationX) =>
		originalTranslationY + ((translationX - originalTranslationX) * (translateTargetY - originalTranslationY) / (translateTargetX - originalTranslationX));

	// A missed attempt's TranslateToAsync left the view at its target
	static Task<bool> StartTranslateToFromTheOriginalTranslation(VisualElement view, double targetX, double targetY, uint length)
	{
		view.TranslationX = originalTranslationX;
		view.TranslationY = originalTranslationY;
		return view.TranslateToAsync(targetX, targetY, length, Easing.Linear);
	}

	// A missed attempt's TranslateToAsync left the view translated
	static Task StartBounceFromTheOriginalTranslation(VisualElement view, BounceAnimation bounce)
	{
		view.TranslationX = originalTranslationX;
		view.TranslationY = originalTranslationY;
		return bounce.Animate(view);
	}

	// MAUI gives every animation committed in the same turn the same milliseconds in each frame, and a linear animation's progress is its time: a sampler
	// committed with a run reads that run's time, which a stopwatch cannot, because platforms differ in how much of a stall before the first frame their ticker credits
	static void CommitTickerTimeSampler(VisualElement view, string handle, Action<double> readTimeSinceCommitted) =>
		new Microsoft.Maui.Controls.Animation(progress => readTimeSinceCommitted(progress * tickerTimeSamplerLength)).Commit(view, handle, length: tickerTimeSamplerLength);

	// From Bounce's keyframes: a hop rises with CubicOut (1 - (1 - x)³), may hold, then falls with CubicIn (x³); the times are fractions of the bounce
	static TimeSpanOfTheBounce FindTimesAHopIsAtLeast(double shareOfItsHeight, uint bounceLength, double riseStart, double peakStart, double fallStart, double fallEnd)
	{
		var shareOfTheRiseOrFallAboveIt = Math.Cbrt(1 - shareOfItsHeight);
		return new(bounceLength * (peakStart - ((peakStart - riseStart) * shareOfTheRiseOrFallAboveIt)), bounceLength * (fallStart + ((fallEnd - fallStart) * shareOfTheRiseOrFallAboveIt)));
	}

	static string DescribeFrames(IEnumerable<BounceFrame> frames) =>
		string.Join(", ", frames.Select(frame => string.Create(CultureInfo.InvariantCulture, $"{frame.Time:F0} ms: {FormatNativeValue(frame.Height)}{(frame.IsTranslating ? string.Empty : " (translation ended)")}")));

	readonly record struct BounceFrame(double Time, double Height, bool IsTranslating);

	readonly record struct TimeSpanOfTheBounce(double Start, double End)
	{
		// Android's ticker can play a frame inside the Commit that starts it, which the run committed first gets alone
		const double tickerMismatchMilliseconds = 20;

		public bool Contains(double time) => time >= Start + tickerMismatchMilliseconds && time <= End - tickerMismatchMilliseconds;

		public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Start:F0}-{End:F0} ms");
	}

	static void AssertTranslationAtTheAppsTarget(VisualElement view, string when)
	{
		Assert.True(Math.Abs(view.TranslationX - translateTargetX) <= valueTolerance, $"TranslationX is {view.TranslationX} {when}, expected the app's target {translateTargetX}.");
		Assert.True(Math.Abs(view.TranslationY - translateTargetY) <= valueTolerance, $"TranslationY is {view.TranslationY} {when}, expected the app's target {translateTargetY}.");
	}

	[Fact]
	public async Task AttentionSeekerCancelledWhileItsStartWaitsOnTheBusyUIThreadDoesNotStopTheRunningOneOrAnimateTheView()
	{
		SetupBuilder();

		var view = CreateAnimatedView(scaleRotationAndTranslationY);

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			using var runningTokenSource = new CancellationTokenSource();
			using var cancelledTokenSource = new CancellationTokenSource();
			var midRun = WaitForValueAsync(view, VisualElement.ScaleProperty, scale => Math.Abs(scale - originalScale) > 0.02);
			var runningRun = new TadaAnimation { Length = seekerLengthFarBeyondTheWait }.Animate(view, runningTokenSource.Token);
			await midRun;

			var translationYChanges = 0;
			view.PropertyChanged += CountTranslationYChanges;
			try
			{
				// The pool thread's dispatch only queues the start, and this turn keeps the UI thread from running it until the token is cancelled
				Task? cancelledRun = null;
				var queuedTheStart = Task.Run(() =>
				{
					cancelledRun = new BounceAnimation { Distance = bounceDistance, Length = runLength }.Animate(view, cancelledTokenSource.Token);
				}).Wait(waitTimeout);
				Assert.True(queuedTheStart, "The pool thread did not queue the start.");
				cancelledTokenSource.Cancel();

				var exception = await Record.ExceptionAsync(() => cancelledRun!.WaitAsync(waitTimeout));
				Assert.True(exception is OperationCanceledException && cancelledRun!.IsCanceled, $"The cancelled run ended {cancelledRun.Status} ({exception?.GetType().Name}), expected Canceled.");
				Assert.False(runningRun.IsCompleted, "The cancelled run's start stopped the animation that was running.");

				await WaitForTickerFramesAsync(view);
				Assert.True(view.AnimationIsRunning(KeyframeAnimationExtensions.AnimationHandle), "The animation that was running no longer runs.");
				Assert.True(translationYChanges is 0, $"The cancelled run changed TranslationY {translationYChanges} time(s).");
			}
			finally
			{
				view.PropertyChanged -= CountTranslationYChanges;
			}

			runningTokenSource.Cancel();
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runningRun.WaitAsync(waitTimeout));
			await AssertStaysAtOriginalValuesAsync(handler, view, scaleRotationAndTranslationY, nativeStateAtRest, "after the animation that was running was cancelled");

			void CountTranslationYChanges(object? sender, PropertyChangedEventArgs e)
			{
				if (e.PropertyName == VisualElement.TranslationYProperty.PropertyName)
				{
					translationYChanges++;
				}
			}
		});
	}

	[Fact]
	public async Task ScaleAnimationStartedWhileHeartbeatRunsStopsItAndReturnsTheViewToItsOriginalValues()
	{
		SetupBuilder();

		var view = CreateAnimatedView(scaleOnly);
		var heartbeat = new HeartbeatAnimation { MaximumScale = 1.25, Length = 3 * runLength };
		var scale = new ScaleAnimation { Scale = 1.6, Length = runLength };

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			Task? scaleRun = null;
			var heartbeatRun = await StartAndActMidRunAsync(view, VisualElement.ScaleProperty, currentScale => currentScale >= originalScale + 0.05, () => heartbeat.Animate(view),
				() => scaleRun = scale.Animate(view));

			await heartbeatRun.WaitAsync(waitTimeout);
			Assert.True(heartbeatRun.IsCompletedSuccessfully, $"Heartbeat ended {heartbeatRun.Status}; a stopped attention seeker completes normally.");
			Assert.False(scaleRun!.IsCompleted, "Heartbeat did not end when Scale started, but at its own end.");

			await scaleRun.WaitAsync(waitTimeout);
			await AssertStaysAtOriginalValuesAsync(handler, view, scaleOnly, nativeStateAtRest, "after Scale stopped Heartbeat");
		});
	}

	[Fact]
	public async Task ScaleAnimationWhoseStartAbortsTheAppsScaleToAsyncStopsTheHeartbeatThatItsContinuationStarted()
	{
		SetupBuilder();

		var view = CreateAnimatedView(scaleOnly);
		var heartbeat = new HeartbeatAnimation { MaximumScale = 1.25, Length = 3 * runLength };
		var scale = new ScaleAnimation { Scale = 1.6, Length = runLength };

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			Task? heartbeatRun = null;
			var appScale = view.ScaleToAsync(originalScale, 3 * runLength, Easing.Linear);
			var appCode = appScale.ContinueWith(_ => heartbeatRun = heartbeat.Animate(view), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

			// Scale's start aborts the app's ScaleToAsync, whose continuation starts Heartbeat inside that abort
			var scaleRun = scale.Animate(view);
			Assert.True(heartbeatRun is not null, "The app's continuation did not run inside the abort of its ScaleToAsync.");

			await heartbeatRun.WaitAsync(waitTimeout);
			Assert.True(heartbeatRun.IsCompletedSuccessfully, $"Heartbeat ended {heartbeatRun.Status}; a stopped attention seeker completes normally.");
			Assert.False(scaleRun.IsCompleted, "Heartbeat did not end when Scale started, but at its own end.");

			await Task.WhenAll(scaleRun, appCode).WaitAsync(waitTimeout);
			await AssertStaysAtOriginalValuesAsync(handler, view, scaleOnly, nativeStateAtRest, "after Scale stopped Heartbeat");
		});
	}

	// The seeker runs far longer than the wait, so only the app's animation taking over ends it in time
	[Theory]
	[InlineData(nameof(HeartbeatAnimation))]
	[InlineData(nameof(TadaAnimation))]
	[InlineData(nameof(RubberBandAnimation))]
	[InlineData(nameof(SwingAnimation))]
	[InlineData(nameof(WobbleAnimation))]
	public async Task AppsToAnimationStartedWhileASeekerAnimatesItsPropertyTakesOverAndTheViewKeepsItsTarget(string seekerName)
	{
		SetupBuilder();

		var (seeker, seekerValues, appsProperty, appsTarget, startAppsAnimation) = CreateSeekerAndAppsToAnimation(seekerName, seekerLengthFarBeyondTheWait);
		var view = CreateAnimatedView(seekerValues);
		var originalAppsValue = seekerValues.Single(value => value.Property == appsProperty).Value;
		IReadOnlyList<(BindableProperty Property, double Value)> expectedValues = [.. seekerValues.Select(value => value.Property == appsProperty ? (value.Property, appsTarget) : value)];

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			var seekerMoved = WaitForValueAsync(view, appsProperty, value => Math.Abs(value - originalAppsValue) > valueTolerance);
			var seekerRun = seeker.Animate(view);
			await seekerMoved;

			var appsRun = startAppsAnimation(view, appsTarget);

			await seekerRun.WaitAsync(waitTimeout);
			Assert.True(seekerRun.IsCompletedSuccessfully, $"{seekerName} ended {seekerRun.Status}; a seeker that the app's animation takes over completes normally.");

			// .NET MAUI's To animations report true when something interrupted them
			var appsRunWasInterrupted = await appsRun.WaitAsync(waitTimeout);
			Assert.False(appsRunWasInterrupted, $"{seekerName} interrupted the app's animation that started during it.");

			await AssertStaysAtValuesAsync(handler, view, expectedValues, $"after the app's animation took over from {seekerName}");
		});
	}

	static (BaseAnimation Seeker, IReadOnlyList<(BindableProperty Property, double Value)> SeekerValues, BindableProperty AppsProperty, double AppsTarget, Func<VisualElement, double, Task<bool>> StartAppsAnimation)
		CreateSeekerAndAppsToAnimation(string seekerName, uint seekerLength) => seekerName switch
		{
			nameof(HeartbeatAnimation) => (new HeartbeatAnimation { Length = seekerLength }, scaleOnly, VisualElement.ScaleProperty, 1.5,
				(view, target) => view.ScaleToAsync(target, runLength / 2, Easing.Linear)),
			nameof(TadaAnimation) => (new TadaAnimation { Length = seekerLength }, scaleAndRotation, VisualElement.RotationProperty, originalRotation + 90,
				(view, target) => view.RotateToAsync(target, runLength / 2, Easing.Linear)),
			nameof(RubberBandAnimation) => (new RubberBandAnimation { Length = seekerLength }, scaleXAndScaleY, VisualElement.ScaleYProperty, 1.5,
				(view, target) => view.ScaleYToAsync(target, runLength / 2, Easing.Linear)),
			nameof(SwingAnimation) => (new SwingAnimation { RotationAngle = swingAngle, Length = seekerLength }, rotationAndAnchor, VisualElement.RotationProperty, originalRotation + 90,
				(view, target) => view.RotateToAsync(target, runLength / 2, Easing.Linear)),
			nameof(WobbleAnimation) => (new WobbleAnimation { Distance = wobbleDistance, RotationAngle = wobbleRotationAngle, Length = seekerLength }, translationXAndRotation, VisualElement.RotationProperty, originalRotation + 90,
				(view, target) => view.RotateToAsync(target, runLength / 2, Easing.Linear)),
			_ => throw new NotSupportedException(seekerName)
		};

	static async Task AssertStaysAtValuesAsync(IPlatformViewHandler handler, VisualElement view, IReadOnlyList<(BindableProperty Property, double Value)> expectedValues, string when)
	{
		AssertAtValues(handler, view, expectedValues, when);
		await WaitForTickerFramesAsync(view);
		AssertAtValues(handler, view, expectedValues, $"a few frames {when}");
	}

	static void AssertAtValues(IPlatformViewHandler handler, VisualElement view, IReadOnlyList<(BindableProperty Property, double Value)> expectedValues, string when)
	{
		foreach (var (property, expectedValue) in expectedValues)
		{
			var value = (double)view.GetValue(property);
			Assert.True(Math.Abs(value - expectedValue) <= valueTolerance, $"The view's {property.PropertyName} is {value} {when}, expected {expectedValue}.");
		}

		var mismatch = FindNativeMismatch(handler, view);
		Assert.True(mismatch is null, $"{mismatch} ({when})");
	}

	// Swing moves the anchor while it creates its keyframes, so a PropertyChanged handler can start a seeker that Swing's own animation then aborts
	[Fact]
	public async Task RotateAnimationStopsASwingWhoseAnchorChangeStartedAnotherSeeker()
	{
		SetupBuilder();

		var view = CreateAnimatedView(rotationAnchorAndScale);
		var swing = new SwingAnimation { RotationAngle = swingAngle, Length = seekerLengthFarBeyondTheWait };
		var rotate = new RotateAnimation { Rotation = rotateBy, Length = runLength };

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			Task? heartbeatStartedByTheAnchorChange = null;
			view.PropertyChanged += StartHeartbeatWhenTheAnchorMovesToTheTopEdge;
			Task swingRun;

			try
			{
				swingRun = swing.Animate(view);
			}
			finally
			{
				view.PropertyChanged -= StartHeartbeatWhenTheAnchorMovesToTheTopEdge;
			}

			await (heartbeatStartedByTheAnchorChange ?? throw new InvalidOperationException("The anchor change did not start Heartbeat.")).WaitAsync(waitTimeout);
			await WaitForTickerFramesAsync(view);
			Assert.True(view.AnchorY == 0, $"AnchorY is {view.AnchorY} while Swing runs, expected its top-edge anchor 0.");

			var rotateRun = rotate.Animate(view);

			Assert.True(view.AnchorY == originalAnchorY, $"AnchorY is {view.AnchorY} right after Rotate started, expected the original {originalAnchorY}: Rotate must stop the running Swing.");

			// Swing runs twice as long as the wait, so only Rotate can have ended it in time, however long the UI thread stalls (and lets Rotate end too)
			await swingRun.WaitAsync(waitTimeout);
			Assert.True(swingRun.IsCompletedSuccessfully, $"Swing ended {swingRun.Status}; a stopped attention seeker completes normally.");

			await rotateRun.WaitAsync(waitTimeout);
			await AssertStaysAtOriginalValuesAsync(handler, view, rotationAnchorAndScale, nativeStateAtRest, "after Rotate stopped the Swing");

			void StartHeartbeatWhenTheAnchorMovesToTheTopEdge(object? sender, PropertyChangedEventArgs e)
			{
				if (heartbeatStartedByTheAnchorChange is null && e.PropertyName == VisualElement.AnchorYProperty.PropertyName && view.AnchorY == 0)
				{
					heartbeatStartedByTheAnchorChange = new HeartbeatAnimation { Length = runLength }.Animate(view);
				}
			}
		});
	}

	// Swing's start aborts the app's RotateToAsync, whose continuation starts a Swing that the first Swing's own animation then aborts
	[Fact]
	public async Task SwingWhoseStartAbortsARotateToWhoseContinuationStartsAnotherSwingKeepsTheTopEdgeAnchorUntilItEnds()
	{
		SetupBuilder();

		var view = CreateAnimatedView(rotationAndAnchor);
		var swing = new SwingAnimation { RotationAngle = swingAngle, Length = 3 * runLength };

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			var timeTheLastSwingRan = TimeSpan.Zero;

			await RepeatAttemptsThatMissedTheRunAsync(async () =>
			{
				Task? swingStartedByTheContinuation = null;
				var appRotation = view.RotateToAsync(originalRotation, 2 * runLength);
				_ = appRotation.ContinueWith(_ => swingStartedByTheContinuation = new SwingAnimation { RotationAngle = swingAngle, Length = runLength }.Animate(view), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

				var sinceTheSwingStarted = Stopwatch.StartNew();
				var swingRun = swing.Animate(view);

				await (swingStartedByTheContinuation ?? throw new InvalidOperationException("The aborted RotateToAsync did not start a Swing.")).WaitAsync(waitTimeout);
				await WaitForTickerFramesAsync(view);

				// Only a Swing that had its whole length can have reached its own end by now, which a stalled UI thread allows
				timeTheLastSwingRan = sinceTheSwingStarted.Elapsed;
				if (!view.AnimationIsRunning(KeyframeAnimationExtensions.AnimationHandle) && timeTheLastSwingRan >= TimeSpan.FromMilliseconds(swing.Length) - slowestFrameInterval)
				{
					await swingRun.WaitAsync(waitTimeout);
					await AssertStaysAtOriginalValuesAsync(handler, view, rotationAndAnchor, nativeStateAtRest, "after the surviving Swing, which ended before the test looked, completed");
					return false;
				}

				Assert.False(swingRun.IsCompleted, "The surviving Swing ended before its length.");
				Assert.True(view.AnchorY == 0, $"AnchorY is {view.AnchorY} while the surviving Swing runs, expected its top-edge anchor 0.");
				AssertPlatformViewFollows(handler, view, nativeStateAtRest, "while the surviving Swing runs");

				await swingRun.WaitAsync(waitTimeout);
				await AssertStaysAtOriginalValuesAsync(handler, view, rotationAndAnchor, nativeStateAtRest, "after the surviving Swing completed");
				return true;
			}, () => $"the surviving Swing had ended after {timeTheLastSwingRan.TotalMilliseconds:F0} ms when the test looked.");
		});
	}

	// MAUI calls the finished callback from its ticker, so an exception escaping it would crash the app
	[Fact]
	public async Task SeekerWhoseRestoreThrowsAtTheEndFaultsItsTaskAndRestoresEveryProperty()
	{
		SetupBuilder();

		var view = CreateAnimatedView(scaleAndRotation);
		var tada = new TadaAnimation { Length = runLength };

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			var restoreThrew = MoveTheViewAfterTheLastFrameAndThrowWhenTheScaleIsRestored(view);

			var run = tada.Animate(view);

			var exception = await Record.ExceptionAsync(() => run.WaitAsync(waitTimeout));
			Assert.True(exception is InvalidOperationException { Message: throwingHandlerMessage }, $"The run ended with {exception?.GetType().Name ?? "no exception"}, expected the exception of the PropertyChanged handler.");
			Assert.True(restoreThrew(), "The PropertyChanged handler did not throw while Tada restored the view.");

			await AssertRestoredAfterTheThrowingPropertyAsync(handler, view, nativeStateAtRest, "after the run's restore threw");
		});
	}

	[Fact]
	public async Task SeekerCancelledWhileItsRestoreThrowsFaultsItsTaskWithoutThrowingIntoCancel()
	{
		SetupBuilder();

		var view = CreateAnimatedView(scaleAndRotation);
		var tada = new TadaAnimation { Length = runLength };

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			using var cancellation = new CancellationTokenSource();
			var hasThrown = false;

			try
			{
				// On the UI thread, as from a button's Clicked handler: the cancelled run restores the view inside Cancel
				var run = await StartAndActMidRunAsync(view, VisualElement.ScaleProperty, scale => Math.Abs(scale - originalScale) > 0.02, () => tada.Animate(view, cancellation.Token), () =>
				{
					view.PropertyChanged += ThrowWhenTheScaleIsRestored;
					var cancelException = Record.Exception(cancellation.Cancel);
					Assert.True(cancelException is null, $"Cancel threw {cancelException}; the exception of the handler belongs to the run's Task.");
				});

				var exception = await Record.ExceptionAsync(() => run.WaitAsync(waitTimeout));
				Assert.True(exception is InvalidOperationException { Message: throwingHandlerMessage }, $"The run ended with {exception?.GetType().Name ?? "no exception"}, expected the exception of the PropertyChanged handler.");
				Assert.True(hasThrown, "The PropertyChanged handler did not throw while the cancelled run restored the view.");
			}
			finally
			{
				view.PropertyChanged -= ThrowWhenTheScaleIsRestored;
			}

			await AssertRestoredAfterTheThrowingPropertyAsync(handler, view, nativeStateAtRest, "after the cancelled run's restore threw");

			void ThrowWhenTheScaleIsRestored(object? sender, PropertyChangedEventArgs e)
			{
				if (!hasThrown && e.PropertyName == VisualElement.ScaleProperty.PropertyName && view.Scale == originalScale)
				{
					hasThrown = true;
					throw new InvalidOperationException(throwingHandlerMessage);
				}
			}
		});
	}

	// A frame before the last can write the original rotation too, so every such write moves the view and the last move follows the last frame, which the restore then undoes.
	// The animation's handle is gone only while the restore runs, which is when the handler throws, once: MAUI leaves a property whose PropertyChanged handler threw unable to change.
	static Func<bool> MoveTheViewAfterTheLastFrameAndThrowWhenTheScaleIsRestored(VisualElement view)
	{
		var hasMovedTheView = false;
		var hasThrown = false;
		view.PropertyChanged += HandlePropertyChanged;

		return () => hasThrown;

		void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			var isTweenRunning = view.AnimationIsRunning(KeyframeAnimationExtensions.AnimationHandle);

			if (isTweenRunning && e.PropertyName == VisualElement.RotationProperty.PropertyName && view.Rotation == originalRotation)
			{
				hasMovedTheView = true;
				view.Scale = 3;
				view.Rotation = 99;
			}
			else if (!isTweenRunning && hasMovedTheView && !hasThrown && e.PropertyName == VisualElement.ScaleProperty.PropertyName && view.Scale == originalScale)
			{
				hasThrown = true;
				throw new InvalidOperationException(throwingHandlerMessage);
			}
		}
	}

	// The handler that threw kept .NET MAUI from mapping that one property to the platform view, which nothing but the app can do afterwards
	async Task AssertRestoredAfterTheThrowingPropertyAsync(IPlatformViewHandler handler, VisualElement view, string nativeStateAtRest, string when)
	{
		Assert.True(Math.Abs(view.Rotation - originalRotation) <= valueTolerance, $"The view's Rotation is {view.Rotation} {when}, expected the original {originalRotation}, which the restore writes after the property whose handler threw.");
		Assert.True(Math.Abs(view.Scale - originalScale) <= valueTolerance, $"The view's Scale is {view.Scale} {when}, expected the original {originalScale}.");
		Assert.False(view.AnimationIsRunning(KeyframeAnimationExtensions.AnimationHandle), $"The keyframe animation still runs {when}.");

		handler.UpdateValue(VisualElement.ScaleProperty.PropertyName);
		handler.UpdateValue(VisualElement.RotationProperty.PropertyName);
		await AssertStaysAtOriginalValuesAsync(handler, view, scaleAndRotation, nativeStateAtRest, when);
	}
}
