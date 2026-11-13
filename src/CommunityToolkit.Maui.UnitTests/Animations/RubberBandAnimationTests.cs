using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

public class RubberBandAnimationTests : BaseKeyframeAnimationTests<RubberBandAnimation>
{
	const double originalScaleX = 1.1;
	const double originalScaleY = 0.9;
	const double floatingPointNoise = 1e-9;

	static readonly (double Begin, double End, double From, double To)[] scaleXSegments =
	[
		(0.0, 0.3, 1, 1.25), (0.3, 0.4, 1.25, 0.75), (0.4, 0.5, 0.75, 1.15), (0.5, 0.65, 1.15, 0.95), (0.65, 0.75, 0.95, 1.05), (0.75, 1.0, 1.05, 1)
	];

	static readonly (double Begin, double End, double From, double To)[] scaleYSegments =
	[
		(0.0, 0.3, 1, 0.75), (0.3, 0.4, 0.75, 1.25), (0.4, 0.5, 1.25, 0.85), (0.5, 0.65, 0.85, 1.05), (0.65, 0.75, 1.05, 0.95), (0.75, 1.0, 0.95, 1)
	];

	protected override IReadOnlySet<string> AnimatedProperties { get; } = new HashSet<string> { nameof(VisualElement.ScaleX), nameof(VisualElement.ScaleY) };

	[Fact]
	public void EnsureDefaults()
	{
		var animation = new RubberBandAnimation();

		Assert.Equal(BaseAnimationDefaults.Easing, animation.Easing);
		Assert.Equal(RubberBandAnimationDefaults.Length, animation.Length);
		Assert.Equal(1000u, animation.Length);
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_StretchesScaleXWideAndThinAroundItsOriginalValue() => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateFunctionalAnimation();
		var view = CreateAnimatedViewWithNonDefaultValues();
		var scaleX = new ExtremeValueRecorder(view, VisualElement.ScaleXProperty);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		scaleX.Max.Should().BeApproximately(originalScaleX * 1.25, precision);
		scaleX.Min.Should().BeApproximately(originalScaleX * 0.75, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_StretchesScaleYTallAndFlatAroundItsOriginalValue() => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateFunctionalAnimation();
		var view = CreateAnimatedViewWithNonDefaultValues();
		var scaleY = new ExtremeValueRecorder(view, VisualElement.ScaleYProperty);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		scaleY.Max.Should().BeApproximately(originalScaleY * 1.25, precision);
		scaleY.Min.Should().BeApproximately(originalScaleY * 0.75, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_ScaleXWobblesThroughTheExpectedFactorsInOrder() => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateFunctionalAnimation();
		var view = CreateAnimatedViewWithNonDefaultValues();
		using var recorder = new PropertyRecorder(view);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		var factors = GetTurningPoints(recorder.ValuesOf(nameof(VisualElement.ScaleX))).Select(scaleX => scaleX / originalScaleX);
		factors.Should().HaveCount(5);
		factors.Zip(new[] { 1.25, 0.75, 1.15, 0.95, 1.05 }).Should().OnlyContain(pair => Math.Abs(pair.First - pair.Second) <= precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_ScaleYWobblesThroughTheExpectedFactorsInOrder() => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateFunctionalAnimation();
		var view = CreateAnimatedViewWithNonDefaultValues();
		using var recorder = new PropertyRecorder(view);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		var factors = GetTurningPoints(recorder.ValuesOf(nameof(VisualElement.ScaleY))).Select(scaleY => scaleY / originalScaleY);
		factors.Should().HaveCount(5);
		factors.Zip(new[] { 0.75, 1.25, 0.85, 1.05, 0.95 }).Should().OnlyContain(pair => Math.Abs(pair.First - pair.Second) <= precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_FinishesWithAMovementBackToRestInsteadOfSnapping() => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateFunctionalAnimation();
		var view = CreateAnimatedViewWithNonDefaultValues();
		using var recorder = new PropertyRecorder(view);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		var scaleXValues = recorder.ValuesOf(nameof(VisualElement.ScaleX)).ToList();
		scaleXValues[^1].Should().BeApproximately(originalScaleX, precision);
		var lastScaleXPeak = originalScaleX * 1.05;
		var lastScaleXPeakIndex = scaleXValues.FindLastIndex(scaleX => Math.Abs(scaleX - lastScaleXPeak) <= precision);
		lastScaleXPeakIndex.Should().BeGreaterThanOrEqualTo(0);
		scaleXValues.Skip(lastScaleXPeakIndex + 1).Should().Contain(scaleX => scaleX > originalScaleX + precision && scaleX < lastScaleXPeak - precision);

		var scaleYValues = recorder.ValuesOf(nameof(VisualElement.ScaleY)).ToList();
		scaleYValues[^1].Should().BeApproximately(originalScaleY, precision);
		var lastScaleYTrough = originalScaleY * 0.95;
		var lastScaleYTroughIndex = scaleYValues.FindLastIndex(scaleY => Math.Abs(scaleY - lastScaleYTrough) <= precision);
		lastScaleYTroughIndex.Should().BeGreaterThanOrEqualTo(0);
		scaleYValues.Skip(lastScaleYTroughIndex + 1).Should().Contain(scaleY => scaleY < originalScaleY - precision && scaleY > lastScaleYTrough + precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_FollowsTheKeyframesOnEveryFrame() => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateFunctionalAnimation();
		var view = CreateAnimatedViewWithNonDefaultValues();
		var scaleXFrames = new List<(long Milliseconds, double Value)>();
		var scaleYFrames = new List<(long Milliseconds, double Value)>();
		view.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(VisualElement.ScaleX))
			{
				scaleXFrames.Add((view.GetElapsedAnimationMilliseconds(), view.ScaleX));
			}
			else if (e.PropertyName == nameof(VisualElement.ScaleY))
			{
				scaleYFrames.Add((view.GetElapsedAnimationMilliseconds(), view.ScaleY));
			}
		};

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		AssertFramesFollowSegments(scaleXFrames, scaleXSegments, originalScaleX, animation.Length);
		AssertFramesFollowSegments(scaleYFrames, scaleYSegments, originalScaleY, animation.Length);
	}, TestContext.Current.CancellationToken);

	static void AssertFramesFollowSegments(List<(long Milliseconds, double Value)> frames, (double Begin, double End, double From, double To)[] segments, double original, uint length)
	{
		frames.Should().NotBeEmpty();
		foreach (var frame in CarryValuesForwardToEveryTick(frames, original, length))
		{
			// Valid because the fresh view's mock clock starts at 0 and the functional animation uses linear easing.
			var progress = Math.Min(1.0, frame.Milliseconds / (double)length);
			var factor = 1.0;
			foreach (var (begin, end, from, to) in segments)
			{
				if (progress >= end)
				{
					factor = to;
				}
				else if (progress > begin)
				{
					factor = from + ((to - from) * Easing.SinInOut.Ease((progress - begin) / (end - begin)));
					break;
				}
			}

			frame.Value.Should().BeApproximately(original * factor, precision, $"at {frame.Milliseconds} ms");
		}
	}

	const int frameIntervalMilliseconds = 16;

	static IEnumerable<(long Milliseconds, double Value)> CarryValuesForwardToEveryTick(List<(long Milliseconds, double Value)> frames, double valueBeforeFirstChange, uint length)
	{
		// Checks ticks that raise no event too. On one tick a finishing segment writes its end value and the next segment its first, so the last write wins.
		var lastValueByTick = frames.GroupBy(frame => frame.Milliseconds).ToDictionary(group => group.Key, group => group.Last().Value);
		var lastTick = (long)Math.Ceiling(length / (double)frameIntervalMilliseconds) * frameIntervalMilliseconds;
		var carriedValue = valueBeforeFirstChange;
		for (var tick = 0L; tick <= lastTick; tick += frameIntervalMilliseconds)
		{
			if (lastValueByTick.TryGetValue(tick, out var value))
			{
				carriedValue = value;
			}

			yield return (tick, carriedValue);
		}
	}

	static List<double> GetTurningPoints(IEnumerable<double> values)
	{
		var distinctValues = new List<double>();
		foreach (var value in values)
		{
			if (distinctValues.Count == 0 || Math.Abs(value - distinctValues[^1]) > floatingPointNoise)
			{
				distinctValues.Add(value);
			}
		}

		var turningPoints = new List<double>();
		for (var i = 1; i < distinctValues.Count - 1; i++)
		{
			if ((distinctValues[i] - distinctValues[i - 1]) * (distinctValues[i + 1] - distinctValues[i]) < 0)
			{
				turningPoints.Add(distinctValues[i]);
			}
		}

		return turningPoints;
	}
}
