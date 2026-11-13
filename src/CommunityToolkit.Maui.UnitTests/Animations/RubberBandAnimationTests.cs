using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Core;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

public class RubberBandAnimationTests : BaseKeyframeAnimationTests<RubberBandAnimation>
{
	const double originalScaleX = 1.1;
	const double originalScaleY = 0.9;

	static readonly double[] fractions = [0.0, 0.3, 0.4, 0.5, 0.65, 0.75, 1.0];
	static readonly double[] scaleXFactors = [1, 1.25, 0.75, 1.15, 0.95, 1.05, 1];
	static readonly double[] scaleYFactors = [1, 0.75, 1.25, 0.85, 1.05, 0.95, 1];

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
		using var recorder = new FrameRecorder(view, VisualElement.ScaleXProperty, VisualElement.ScaleYProperty);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		AssertEveryFrameFollowsTheKeyframes(recorder, VisualElement.ScaleXProperty, SinInOutSegments(fractions, [.. scaleXFactors.Select(factor => factor * originalScaleX)]), animation.Length);
		AssertEveryFrameFollowsTheKeyframes(recorder, VisualElement.ScaleYProperty, SinInOutSegments(fractions, [.. scaleYFactors.Select(factor => factor * originalScaleY)]), animation.Length);
	}, TestContext.Current.CancellationToken);
}
