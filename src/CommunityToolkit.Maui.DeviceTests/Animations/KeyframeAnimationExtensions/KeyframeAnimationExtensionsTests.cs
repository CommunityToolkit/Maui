using System.ComponentModel;
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
	const string throwingHandlerMessage = "Thrown by a PropertyChanged handler.";

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
			var translateRun = view.TranslateToAsync(translateTargetX, translateTargetY, 3 * runLength, Easing.Linear);
			await translating;

			// The hops ride on top of the translation, which has barely left the original translation when the first hop peaks
			var bounceHopsAboveTheOriginal = WaitForValueAsync(view, VisualElement.TranslationYProperty, translationY => translationY <= originalTranslationY - (bounceDistance / 2));
			var bounceRun = bounce.Animate(view);

			await bounceRun.WaitAsync(waitTimeout);
			await bounceHopsAboveTheOriginal;

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
			var translating = WaitForValueAsync(view, VisualElement.TranslationXProperty, translationX => translationX >= originalTranslationX + 10);
			var translateRun = view.TranslateToAsync(translateTargetX, translateTargetY, runLength / 3, Easing.Linear);
			await translating;

			var firstHopPeak = WaitForValueAsync(view, VisualElement.TranslationYProperty, translationY => Math.Abs(translationY - (translateTargetY - bounceDistance)) <= valueTolerance);
			var bounceRun = bounce.Animate(view);

			var translateWasInterrupted = await translateRun.WaitAsync(waitTimeout);
			await bounceRun.WaitAsync(waitTimeout);
			await firstHopPeak;

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

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			var translating = WaitForValueAsync(view, VisualElement.TranslationXProperty, translationX => translationX >= originalTranslationX + 10);
			var translateRun = view.TranslateToAsync(translateTargetX, originalTranslationY, 4 * runLength, Easing.Linear);
			await translating;

			var heightPerFrame = new List<double>();
			var bounceRun = bounce.Animate(view);

			// Committed after both runs, the sampler ticks last in every frame and so sees the height that frame shows
			new Microsoft.Maui.Controls.Animation(_ => heightPerFrame.Add(originalTranslationY - view.TranslationY)).Commit(view, frameSamplerHandle, length: 60_000);
			try
			{
				await bounceRun.WaitAsync(waitTimeout);
			}
			finally
			{
				view.AbortAnimation(frameSamplerHandle);
			}

			var framesAtTheFirstHopsPeak = heightPerFrame.Select((height, frame) => (height, frame)).Where(sample => sample.height >= 0.9 * bounceDistance).Select(sample => sample.frame).ToList();
			Assert.NotEmpty(framesAtTheFirstHopsPeak);
			Assert.True(framesAtTheFirstHopsPeak[^1] - framesAtTheFirstHopsPeak[0] == framesAtTheFirstHopsPeak.Count - 1,
				$"The view left the first hop's peak and came back to it. Height per frame: {string.Join(", ", heightPerFrame.Select(FormatNativeValue))}");

			var translateWasInterrupted = await translateRun.WaitAsync(waitTimeout);
			Assert.False(translateWasInterrupted, "Bounce interrupted the TranslateToAsync that was running.");
			Assert.True(Math.Abs(view.TranslationX - translateTargetX) <= valueTolerance, $"TranslationX is {view.TranslationX}, expected the app's target {translateTargetX}.");
			Assert.True(Math.Abs(view.TranslationY - originalTranslationY) <= valueTolerance, $"TranslationY is {view.TranslationY}, expected the app's target {originalTranslationY}.");

			var mismatch = FindNativeMismatch(handler, view);
			Assert.True(mismatch is null, mismatch);
		});
	}

	[Fact]
	public async Task TranslateToStartedDuringABounceKeepsTheHopsVisibleOnTopOfTheTranslation()
	{
		const string frameSamplerHandle = "CommunityToolkit.Maui.DeviceTests.HeightAboveTheTranslationSampler";

		SetupBuilder();

		var view = CreateAnimatedView([(VisualElement.TranslationXProperty, originalTranslationX), (VisualElement.TranslationYProperty, originalTranslationY)]);
		var bounce = new BounceAnimation { Distance = bounceDistance, Length = 2 * runLength };

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			var hopping = WaitForValueAsync(view, VisualElement.TranslationYProperty, translationY => translationY <= originalTranslationY - 1);
			var bounceRun = bounce.Animate(view);
			await hopping;

			// Translating to the height the view has now keeps the translation's own TranslationY still, so any height a frame shows comes from the hops
			var translateTargetYNow = view.TranslationY;
			var translateRun = view.TranslateToAsync(translateTargetX, translateTargetYNow, 2 * runLength, Easing.Linear);

			// Committed after both runs, the sampler ticks last in every frame and so sees the height that frame shows
			var heightAboveTheTranslationPerFrame = new List<double>();
			new Microsoft.Maui.Controls.Animation(_ =>
			{
				if (!bounceRun.IsCompleted)
				{
					heightAboveTheTranslationPerFrame.Add(translateTargetYNow - view.TranslationY);
				}
			}).Commit(view, frameSamplerHandle, length: 60_000);
			try
			{
				await bounceRun.WaitAsync(waitTimeout);
			}
			finally
			{
				view.AbortAnimation(frameSamplerHandle);
			}

			Assert.True(heightAboveTheTranslationPerFrame.Any(height => height >= bounceDistance / 4),
				$"The hops never showed while TranslateToAsync ran. Height above the translation per frame: {string.Join(", ", heightAboveTheTranslationPerFrame.Select(FormatNativeValue))}");

			var translateWasInterrupted = await translateRun.WaitAsync(waitTimeout);
			Assert.False(translateWasInterrupted, "Bounce interrupted the TranslateToAsync that started during it.");
			Assert.True(Math.Abs(view.TranslationX - translateTargetX) <= valueTolerance, $"TranslationX is {view.TranslationX}, expected the app's target {translateTargetX}.");
			Assert.True(Math.Abs(view.TranslationY - translateTargetYNow) <= valueTolerance, $"TranslationY is {view.TranslationY}, expected the app's target {translateTargetYNow}.");

			var mismatch = FindNativeMismatch(handler, view);
			Assert.True(mismatch is null, mismatch);
		});
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
			var runningRun = new TadaAnimation { Length = 3 * runLength }.Animate(view, runningTokenSource.Token);
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
			var beating = WaitForValueAsync(view, VisualElement.ScaleProperty, currentScale => currentScale >= originalScale + 0.05);
			var heartbeatRun = heartbeat.Animate(view);
			await beating;

			var scaleRun = scale.Animate(view);

			await heartbeatRun.WaitAsync(waitTimeout);
			Assert.True(heartbeatRun.IsCompletedSuccessfully, $"Heartbeat ended {heartbeatRun.Status}; a stopped attention seeker completes normally.");
			Assert.False(scaleRun.IsCompleted, "Heartbeat did not end when Scale started, but at its own end.");

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

	// Swing moves the anchor while it creates its keyframes, so a PropertyChanged handler can start a seeker that Swing's own tween then aborts
	[Fact]
	public async Task RotateAnimationStopsASwingWhoseAnchorChangeStartedAnotherSeeker()
	{
		SetupBuilder();

		var view = CreateAnimatedView(rotationAnchorAndScale);
		var swing = new SwingAnimation { RotationAngle = swingAngle, Length = 3 * runLength };
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
			await swingRun.WaitAsync(waitTimeout);
			Assert.False(rotateRun.IsCompleted, "Swing did not end when Rotate started, but at its own end.");

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

	// Swing's start aborts the app's RotateToAsync, whose continuation starts a Swing that the first Swing's own tween then aborts
	[Fact]
	public async Task SwingWhoseStartAbortsARotateToWhoseContinuationStartsAnotherSwingKeepsTheTopEdgeAnchorUntilItEnds()
	{
		SetupBuilder();

		var view = CreateAnimatedView(rotationAndAnchor);
		var swing = new SwingAnimation { RotationAngle = swingAngle, Length = 3 * runLength };

		await RunLoadedAsync(view, async (handler, nativeStateAtRest) =>
		{
			Task? swingStartedByTheContinuation = null;
			var appRotation = view.RotateToAsync(originalRotation, 2 * runLength);
			_ = appRotation.ContinueWith(_ => swingStartedByTheContinuation = new SwingAnimation { RotationAngle = swingAngle, Length = runLength }.Animate(view), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

			var swingRun = swing.Animate(view);

			await (swingStartedByTheContinuation ?? throw new InvalidOperationException("The aborted RotateToAsync did not start a Swing.")).WaitAsync(waitTimeout);
			await WaitForTickerFramesAsync(view);

			Assert.False(swingRun.IsCompleted, "The surviving Swing ended before its length.");
			Assert.True(view.AnchorY == 0, $"AnchorY is {view.AnchorY} while the surviving Swing runs, expected its top-edge anchor 0.");
			AssertPlatformViewFollows(handler, view, nativeStateAtRest, "while the surviving Swing runs");

			await swingRun.WaitAsync(waitTimeout);
			await AssertStaysAtOriginalValuesAsync(handler, view, rotationAndAnchor, nativeStateAtRest, "after the surviving Swing completed");
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
			var midRun = WaitForValueAsync(view, VisualElement.ScaleProperty, scale => Math.Abs(scale - originalScale) > 0.02);
			var run = tada.Animate(view, cancellation.Token);
			await midRun;

			var hasThrown = false;
			view.PropertyChanged += ThrowWhenTheScaleIsRestored;

			try
			{
				// On the UI thread, as from a button's Clicked handler: the cancelled run restores the view inside Cancel
				var cancelException = Record.Exception(cancellation.Cancel);
				Assert.True(cancelException is null, $"Cancel threw {cancelException}; the exception of the handler belongs to the run's Task.");

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
	// The tween's handle is gone only while the restore runs, which is when the handler throws, once: MAUI leaves a property whose PropertyChanged handler threw unable to change.
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
