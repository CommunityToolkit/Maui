using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Core;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

public class BounceAnimationTests : BaseKeyframeAnimationTests<BounceAnimation>
{
	const double originalTranslationY = 7;

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
		Assert.Equal(BindingMode.OneWay, BounceAnimation.DistanceProperty.DefaultBindingMode);
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
		using var translationY = new ExtremeValueRecorder(view, VisualElement.TranslationYProperty);

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
		using var translationY = new ExtremeValueRecorder(view, VisualElement.TranslationYProperty);

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
		using var translationY = new ExtremeValueRecorder(view, VisualElement.TranslationYProperty);

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
	public Task Animate_FollowsTheKeyframesFrameByFrame() => AssertFollowsTheKeyframesFrameByFrame(Easing.Linear, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_WithEasing_WarpsTheProgressOfTheWholeRun() => AssertFollowsTheKeyframesFrameByFrame(Easing.CubicIn, TestContext.Current.CancellationToken);

	Task AssertFollowsTheKeyframesFrameByFrame(Easing easing, CancellationToken testToken) => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateFunctionalAnimation();
		animation.Easing = easing;
		var view = CreateAnimatedViewWithNonDefaultValues();
		using var recorder = new FrameRecorder(view, VisualElement.TranslationYProperty);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		var distance = animation.Distance;
		KeyframeSegment[] segments =
		[
			Hop(0.0, 0.25, 0, distance, Easing.CubicOut),
			Hop(0.2875, 0.4125, distance, 0, Easing.CubicIn),
			Hop(0.4125, 0.625, 0, distance / 2, Easing.CubicOut),
			Hop(0.625, 0.75, distance / 2, 0, Easing.CubicIn),
			Hop(0.75, 0.875, 0, distance * 2 / 15, Easing.CubicOut),
			Hop(0.875, 1.0, distance * 2 / 15, 0, Easing.CubicIn)
		];
		AssertEveryFrameFollowsTheKeyframes(recorder, VisualElement.TranslationYProperty, segments, animation.Length, easing);
	}, testToken);

	static KeyframeSegment Hop(double begin, double end, double fromHeight, double toHeight, Easing easing) =>
		new(begin, end, originalTranslationY - fromHeight, originalTranslationY - toHeight, easing);
}
