using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

public class BounceAnimationTests : BaseKeyframeAnimationTests<BounceAnimation>
{
	const double originalTranslationY = 7;
	const double floatingPointNoise = 1e-9;

	protected override IReadOnlySet<string> AnimatedProperties { get; } = new HashSet<string> { nameof(VisualElement.TranslationY) };

	[Fact]
	public void EnsureDefaults()
	{
		var animation = new BounceAnimation();

		Assert.Equal(BaseAnimationDefaults.Easing, animation.Easing);
		Assert.Equal(BounceAnimationDefaults.Length, animation.Length);
		Assert.Equal(BounceAnimationDefaults.Distance, animation.Distance);
		Assert.Equal(800u, animation.Length);
		Assert.Equal(30.0, animation.Distance);
	}

	[Theory]
	[InlineData(double.NaN)]
	[InlineData(double.PositiveInfinity)]
	[InlineData(double.NegativeInfinity)]
	public void NonFiniteDistance_IsIgnored(double nonFiniteValue)
	{
		var animation = new BounceAnimation { Distance = nonFiniteValue };

		Assert.Equal(BounceAnimationDefaults.Distance, animation.Distance);
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_HopsUpByDistanceAndNeverFallsBelowRest() => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateFunctionalAnimation();
		var view = CreateAnimatedViewWithNonDefaultValues();
		var translationY = new ExtremeValueRecorder(view, VisualElement.TranslationYProperty);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		translationY.Min.Should().BeApproximately(originalTranslationY - animation.Distance, precision);
		translationY.Max.Should().BeLessThanOrEqualTo(originalTranslationY + precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_HonorsCustomDistance() => RunOnSingleThreadAsync(async token =>
	{
		const double distance = 50;

		var animation = CreateFunctionalAnimation();
		animation.Distance = distance;
		var view = CreateAnimatedViewWithNonDefaultValues();
		var translationY = new ExtremeValueRecorder(view, VisualElement.TranslationYProperty);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		translationY.Min.Should().BeApproximately(originalTranslationY - distance, precision);
		translationY.Max.Should().BeLessThanOrEqualTo(originalTranslationY + precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_NegativeDistance_BouncesDownward() => RunOnSingleThreadAsync(async token =>
	{
		const double distance = -20;

		var animation = CreateFunctionalAnimation();
		animation.Distance = distance;
		var view = CreateAnimatedViewWithNonDefaultValues();
		var translationY = new ExtremeValueRecorder(view, VisualElement.TranslationYProperty);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		translationY.Max.Should().BeApproximately(originalTranslationY - distance, precision);
		translationY.Min.Should().BeGreaterThanOrEqualTo(originalTranslationY - precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_MakesThreeHopsOfDecreasingHeightsThatReturnToRest() => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateFunctionalAnimation();
		var view = CreateAnimatedViewWithNonDefaultValues();
		using var recorder = new PropertyRecorder(view);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		var heights = GetTurningPoints(recorder.ValuesOf(nameof(VisualElement.TranslationY)).Select(translationY => originalTranslationY - translationY));
		var expectedHeights = new[] { animation.Distance, 0, animation.Distance / 2, 0, animation.Distance * 2 / 15 };
		heights.Should().HaveCount(expectedHeights.Length);
		heights.Zip(expectedHeights).Should().OnlyContain(pair => Math.Abs(pair.First - pair.Second) <= precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_FinishesWithAFallBackToRestInsteadOfSnapping() => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateFunctionalAnimation();
		var view = CreateAnimatedViewWithNonDefaultValues();
		using var recorder = new PropertyRecorder(view);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		var translations = recorder.ValuesOf(nameof(VisualElement.TranslationY)).ToList();
		translations[^1].Should().BeApproximately(originalTranslationY, precision);
		var lastHopHeight = animation.Distance * 2 / 15;
		var heights = translations.Select(translationY => originalTranslationY - translationY).ToList();
		var lastPeakIndex = heights.FindLastIndex(height => Math.Abs(height - lastHopHeight) <= precision);
		lastPeakIndex.Should().BeGreaterThanOrEqualTo(0);
		heights.Skip(lastPeakIndex + 1).Should().Contain(height => height > precision && height < lastHopHeight - precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_FollowsTheKeyframesOnEveryFrame() => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateFunctionalAnimation();
		var view = CreateAnimatedViewWithNonDefaultValues();
		var frames = new List<(long Milliseconds, double Value)>();
		view.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(VisualElement.TranslationY))
			{
				frames.Add((view.GetElapsedAnimationMilliseconds(), view.TranslationY));
			}
		};

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		var distance = animation.Distance;
		(double Begin, double End, double From, double To, Easing Easing)[] segments =
		[
			(0.0, 0.25, 0, distance, Easing.CubicOut),
			(0.2875, 0.4125, distance, 0, Easing.CubicIn),
			(0.4125, 0.625, 0, distance / 2, Easing.CubicOut),
			(0.625, 0.75, distance / 2, 0, Easing.CubicIn),
			(0.75, 0.875, 0, distance * 2 / 15, Easing.CubicOut),
			(0.875, 1.0, distance * 2 / 15, 0, Easing.CubicIn)
		];

		frames.Should().NotBeEmpty();
		foreach (var frame in CarryValuesForwardToEveryTick(frames, originalTranslationY, animation.Length))
		{
			// Valid because the fresh view's mock clock starts at 0 and the functional animation uses linear easing.
			var progress = Math.Min(1.0, frame.Milliseconds / (double)animation.Length);
			var height = 0.0;
			foreach (var (begin, end, from, to, easing) in segments)
			{
				if (progress >= end)
				{
					height = to;
				}
				else if (progress > begin)
				{
					height = from + ((to - from) * easing.Ease((progress - begin) / (end - begin)));
					break;
				}
			}

			frame.Value.Should().BeApproximately(originalTranslationY - height, precision, $"at {frame.Milliseconds} ms");
		}
	}, TestContext.Current.CancellationToken);

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
