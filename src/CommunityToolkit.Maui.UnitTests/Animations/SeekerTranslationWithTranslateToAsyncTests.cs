using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

// A TranslateToAsync keeps running under an attention seeker that animates the same axis, and writes the axis as an absolute value too
public class SeekerTranslationWithTranslateToAsyncTests : BaseAnimationTest
{
	const double translationDistance = 100;
	const uint shortRunLength = 96;
	const uint longRunLength = 320;

	public static TheoryData<string, bool> SeekersAndWhetherTheyOutlastTheTranslation { get; } = new()
	{
		{ nameof(BounceAnimation), false }, { nameof(BounceAnimation), true },
		{ nameof(ShakeAnimation), false }, { nameof(ShakeAnimation), true },
		{ nameof(WobbleAnimation), false }, { nameof(WobbleAnimation), true },
	};

	public static TheoryData<string> TranslatingSeekerNames { get; } = [nameof(BounceAnimation), nameof(ShakeAnimation), nameof(WobbleAnimation)];

	public static TheoryData<string, bool> SeekersAndWhetherAPropertyChangedHandlerBatchesTheView { get; } = new()
	{
		{ nameof(BounceAnimation), false }, { nameof(BounceAnimation), true },
		{ nameof(ShakeAnimation), false }, { nameof(ShakeAnimation), true },
		{ nameof(WobbleAnimation), false }, { nameof(WobbleAnimation), true },
	};

	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(SeekersAndWhetherTheyOutlastTheTranslation))]
	public Task Seeker_StartedWhileTranslateToAsyncRuns_LeavesBothAxesAtTheAppsTarget(string seekerName, bool doesSeekerOutlastTheTranslation) => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var (targetX, targetY) = (view.TranslationX + translationDistance, view.TranslationY + translationDistance);

		var seekerRun = await StartTranslationThenSeekerAsync(view, seekerName, doesSeekerOutlastTheTranslation ? longRunLength : shortRunLength, doesSeekerOutlastTheTranslation ? shortRunLength : longRunLength, token);

		await seekerRun.Seeker.WaitAsync(waitTimeout, token);
		var wasTranslationInterrupted = await seekerRun.Translation.WaitAsync(waitTimeout, token);

		wasTranslationInterrupted.Should().BeFalse();
		view.TranslationX.Should().BeApproximately(targetX, precision);
		view.TranslationY.Should().BeApproximately(targetY, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task BounceAnimation_OutlastingTheTranslation_HopsOnTopOfTheTranslationsTarget() => RunOnSingleThreadAsync(async token =>
	{
		const double distance = 30;
		var view = CreateAnimatedViewWithNonDefaultValues();
		var targetY = view.TranslationY + translationDistance;

		var run = await StartTranslationThenSeekerAsync(view, nameof(BounceAnimation), 2 * longRunLength, shortRunLength, token, distance);
		await run.Translation.WaitAsync(waitTimeout, token);
		using var translationYRecorder = new ExtremeValueRecorder(view, VisualElement.TranslationYProperty);
		await run.Seeker.WaitAsync(waitTimeout, token);

		translationYRecorder.Min.Should().BeApproximately(targetY - distance, 0.01, "the first hop rises by the distance from the translation's target");
		view.TranslationY.Should().BeApproximately(targetY, precision);
	}, TestContext.Current.CancellationToken);

	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(TranslatingSeekerNames))]
	public Task Seeker_CancelledAfterTheTranslationEnded_RestoresWithoutTheSeekersOffset(string seekerName) => RunOnSingleThreadAsync(async token =>
	{
		using var seekerTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
		var view = CreateAnimatedViewWithNonDefaultValues();
		var (targetX, targetY) = (view.TranslationX + translationDistance, view.TranslationY + translationDistance);

		var run = await StartTranslationThenSeekerAsync(view, seekerName, 10 * longRunLength, shortRunLength, seekerTokenSource.Token);
		await run.Translation.WaitAsync(waitTimeout, token);
		await WaitForFramesAsync(view, 8, token);
		seekerTokenSource.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.Seeker.WaitAsync(waitTimeout, token));
		view.TranslationX.Should().BeApproximately(targetX, precision);
		view.TranslationY.Should().BeApproximately(targetY, precision);
	}, TestContext.Current.CancellationToken);

	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(TranslatingSeekerNames))]
	public Task Seeker_RetriggeredAfterTheTranslationEnded_EndsAtTheAppsTarget(string seekerName) => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var (targetX, targetY) = (view.TranslationX + translationDistance, view.TranslationY + translationDistance);

		var run = await StartTranslationThenSeekerAsync(view, seekerName, 10 * longRunLength, shortRunLength, token);
		await run.Translation.WaitAsync(waitTimeout, token);
		await WaitForFramesAsync(view, 8, token);
		var retriggeredRun = CreateSeeker(seekerName, shortRunLength).Animate(view, token);

		await Task.WhenAll(run.Seeker, retriggeredRun).WaitAsync(waitTimeout, token);
		view.TranslationX.Should().BeApproximately(targetX, precision);
		view.TranslationY.Should().BeApproximately(targetY, precision);
	}, TestContext.Current.CancellationToken);

	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(TranslatingSeekerNames))]
	public Task Seeker_CancelledInTheTurnTheAppSetsTheTranslation_KeepsTheAppsTranslation(string seekerName) => RunOnSingleThreadAsync(async token =>
	{
		const double appTranslation = 55;
		using var seekerTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
		var view = CreateAnimatedViewWithNonDefaultValues();
		var translationProperty = GetTranslationProperty(seekerName);
		var startingTranslation = (double)view.GetValue(translationProperty);

		var movedTask = WaitForPropertyAsync(view, translationProperty, token, translation => Math.Abs(translation - startingTranslation) > precision);
		var run = CreateSeeker(seekerName, longRunLength).Animate(view, seekerTokenSource.Token);
		await movedTask;

		view.SetValue(translationProperty, appTranslation);
		seekerTokenSource.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(waitTimeout, token));
		((double)view.GetValue(translationProperty)).Should().Be(appTranslation, "the app wrote the translation after the seeker's last frame, so nothing but the app's value is left to keep");
	}, TestContext.Current.CancellationToken);

	// Copilot's numbers: the hop puts the view at -105 on the third frame, so the translation's last write of -105 changes nothing and raises no PropertyChanged
	[Fact(Timeout = (int)TestDuration.Long)]
	public Task BounceAnimation_WhenTheTranslationsLastWriteEqualsTheBouncedValue_EndsAtTheTranslationsTarget() => RunOnSingleThreadAsync(async token =>
	{
		const double targetTranslationY = -105;
		var view = new Label();
		view.EnableAnimations();
		var translationYAtEachFrame = new List<double>();
		Task? bounceRun = null;

		using (view.ObserveAnimationFrames(_ =>
		{
			translationYAtEachFrame.Add(view.TranslationY);
			bounceRun ??= new BounceAnimation { Distance = 30, Length = 256 }.Animate(view, token);
		}))
		{
			var wasTranslationInterrupted = await view.TranslateToAsync(0, targetTranslationY, 64, Easing.Linear, token).WaitAsync(waitTimeout, token);
			wasTranslationInterrupted.Should().BeFalse();
			await (bounceRun ?? throw new InvalidOperationException("No frame started the Bounce")).WaitAsync(waitTimeout, token);
		}

		translationYAtEachFrame[2].Should().Be(targetTranslationY, "the third frame's hop already puts the view where the translation's last write does");
		view.TranslationY.Should().Be(targetTranslationY);
	}, TestContext.Current.CancellationToken);

	// A translation that starts during a seeker ticks after it in every frame, so its absolute write is the last one before the frame shows.
	// A PropertyChanged handler that batches the view raises BatchCommitted inside the seeker's own write, which must not count as another writer's
	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(SeekersAndWhetherAPropertyChangedHandlerBatchesTheView))]
	public Task TranslateToAsync_StartedWhileASeekerRuns_ShowsTheSeekersOffsetOnTopOfTheTranslationEveryFrame(string seekerName, bool doesAPropertyChangedHandlerBatchTheView) => RunOnSingleThreadAsync(async token =>
	{
		const int framesBeforeTheTranslation = 3;
		const uint seekerLength = 320;
		const uint translationLength = 160;
		var property = GetTranslationProperty(seekerName);
		var seekerOffsets = await RecordSeekerOffsetsAtEachFrameAsync(seekerName, seekerLength, property, token);

		var view = CreateAnimatedViewWithNonDefaultValues();
		if (doesAPropertyChangedHandlerBatchTheView)
		{
			view.PropertyChanged += (_, e) =>
			{
				if (e.PropertyName == property.PropertyName)
				{
					using (view.Batch())
					{
					}
				}
			};
		}

		var targetTranslation = (double)view.GetValue(property) + translationDistance;
		var (targetX, targetY) = (view.TranslationX + translationDistance, view.TranslationY + translationDistance);
		var valuesAtEachFrame = new List<double>();
		var translationStart = double.NaN;
		Task<bool>? translation = null;

		using (view.ObserveAnimationFrames(_ =>
		{
			valuesAtEachFrame.Add((double)view.GetValue(property));
			if (valuesAtEachFrame.Count == framesBeforeTheTranslation)
			{
				translationStart = (double)view.GetValue(property);
				translation = view.TranslateToAsync(targetX, targetY, translationLength, Easing.Linear, token);
			}
		}))
		{
			await CreateSeeker(seekerName, seekerLength).Animate(view, token).WaitAsync(waitTimeout, token);
		}

		var wasTranslationInterrupted = await (translation ?? throw new InvalidOperationException("No frame started the translation")).WaitAsync(waitTimeout, token);
		wasTranslationInterrupted.Should().BeFalse();
		valuesAtEachFrame.Should().HaveCount(seekerOffsets.Count);
		for (var frame = framesBeforeTheTranslation; frame < seekerOffsets.Count; frame++)
		{
			var translationProgress = Math.Min(1, (frame - framesBeforeTheTranslation + 1) * 16 / (double)translationLength);
			var translationValue = translationStart + ((targetTranslation - translationStart) * translationProgress);
			valuesAtEachFrame[frame].Should().BeApproximately(translationValue + seekerOffsets[frame], precision, $"frame {frame} shows the translation plus the seeker's offset");
		}

		((double)view.GetValue(property)).Should().BeApproximately(targetTranslation, precision);
	}, TestContext.Current.CancellationToken);

	// Cancelled mid-hop, the seeker must stop adding its last offset to the frames the translation still writes
	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(TranslatingSeekerNames))]
	public Task Seeker_CancelledWhileTheTranslationRuns_LeavesTheTranslationToReachItsTarget(string seekerName) => RunOnSingleThreadAsync(async token =>
	{
		const int seekerFramesBeforeCancelling = 4;
		using var seekerTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
		var view = CreateAnimatedViewWithNonDefaultValues();
		var (targetX, targetY) = (view.TranslationX + translationDistance, view.TranslationY + translationDistance);
		var translation = view.TranslateToAsync(targetX, targetY, longRunLength, Easing.Linear, token);
		await WaitForFramesAsync(view, 2, token);

		var seekerFrames = 0;
		using var midRunCancellation = view.ObserveAnimationFrames(_ =>
		{
			if (++seekerFrames == seekerFramesBeforeCancelling)
			{
				seekerTokenSource.Cancel();
			}
		});
		var seekerRun = CreateSeeker(seekerName, 2 * longRunLength).Animate(view, seekerTokenSource.Token);

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => seekerRun.WaitAsync(waitTimeout, token));
		var wasTranslationInterrupted = await translation.WaitAsync(waitTimeout, token);
		wasTranslationInterrupted.Should().BeFalse();
		view.TranslationX.Should().BeApproximately(targetX, precision);
		view.TranslationY.Should().BeApproximately(targetY, precision);
	}, TestContext.Current.CancellationToken);

	[Theory(Timeout = (int)TestDuration.Long)]
	[MemberData(nameof(TranslatingSeekerNames))]
	public Task Seeker_RetriggeredWhileTheTranslationRuns_LeavesTheTranslationToReachItsTarget(string seekerName) => RunOnSingleThreadAsync(async token =>
	{
		const int seekerFramesBeforeRetriggering = 4;
		var view = CreateAnimatedViewWithNonDefaultValues();
		var (targetX, targetY) = (view.TranslationX + translationDistance, view.TranslationY + translationDistance);
		var translation = view.TranslateToAsync(targetX, targetY, 2 * longRunLength, Easing.Linear, token);
		await WaitForFramesAsync(view, 2, token);

		var seekerFrames = 0;
		Task? retriggeredRun = null;
		using var midRunRetrigger = view.ObserveAnimationFrames(_ =>
		{
			if (++seekerFrames == seekerFramesBeforeRetriggering)
			{
				retriggeredRun = CreateSeeker(seekerName, longRunLength).Animate(view, token);
			}
		});
		var firstRun = CreateSeeker(seekerName, longRunLength).Animate(view, token);

		await firstRun.WaitAsync(waitTimeout, token);
		var wasTranslationInterrupted = await translation.WaitAsync(waitTimeout, token);
		await (retriggeredRun ?? throw new InvalidOperationException("No frame retriggered the seeker")).WaitAsync(waitTimeout, token);
		wasTranslationInterrupted.Should().BeFalse();
		view.TranslationX.Should().BeApproximately(targetX, precision);
		view.TranslationY.Should().BeApproximately(targetY, precision);
	}, TestContext.Current.CancellationToken);

	// The composer reads another writer's write of an equal value from the boxed value BindableObject stores, because an equal write raises no PropertyChanged
	[Fact]
	public void BindableObject_WriteOfTheCurrentValue_StoresANewBoxWithoutRaisingPropertyChanged()
	{
		var view = new Label { TranslationY = 5 };
		var storedValue = view.GetValue(VisualElement.TranslationYProperty);
		var translationYChanges = 0;
		view.PropertyChanged += (_, e) => translationYChanges += e.PropertyName == nameof(VisualElement.TranslationY) ? 1 : 0;

		view.GetValue(VisualElement.TranslationYProperty).Should().BeSameAs(storedValue, "reading twice returns the one stored box");
		view.TranslationY = 5;

		translationYChanges.Should().Be(0);
		view.GetValue(VisualElement.TranslationYProperty).Should().NotBeSameAs(storedValue);
	}

	static async Task<IReadOnlyList<double>> RecordSeekerOffsetsAtEachFrameAsync(string seekerName, uint seekerLength, BindableProperty property, CancellationToken token)
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var originalValue = (double)view.GetValue(property);
		var offsets = new List<double>();

		using (view.ObserveAnimationFrames(_ => offsets.Add((double)view.GetValue(property) - originalValue)))
		{
			await CreateSeeker(seekerName, seekerLength).Animate(view, token).WaitAsync(waitTimeout, token);
		}

		return offsets;
	}

	static async Task<(Task Seeker, Task<bool> Translation)> StartTranslationThenSeekerAsync(Label view, string seekerName, uint seekerLength, uint translationLength, CancellationToken token, double distance = 30)
	{
		var startingTranslationX = view.TranslationX;
		var translatingTask = WaitForPropertyAsync(view, VisualElement.TranslationXProperty, token, translationX => translationX - startingTranslationX > 5);
		var translation = view.TranslateToAsync(startingTranslationX + translationDistance, view.TranslationY + translationDistance, translationLength, Easing.Linear, token);
		await translatingTask;

		return (CreateSeeker(seekerName, seekerLength, distance).Animate(view, token), translation);
	}

	static BindableProperty GetTranslationProperty(string seekerName) => seekerName is nameof(BounceAnimation) ? VisualElement.TranslationYProperty : VisualElement.TranslationXProperty;

	static BaseAnimation CreateSeeker(string seekerName, uint length, double distance = 30) => seekerName switch
	{
		nameof(BounceAnimation) => new BounceAnimation { Distance = distance, Length = length },
		nameof(ShakeAnimation) => new ShakeAnimation { Length = length },
		nameof(WobbleAnimation) => new WobbleAnimation { Length = length },
		_ => throw new NotSupportedException(seekerName)
	};
}