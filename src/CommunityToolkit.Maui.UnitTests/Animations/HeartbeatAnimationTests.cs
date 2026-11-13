using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Core;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

public class HeartbeatAnimationTests : BaseKeyframeAnimationTests<HeartbeatAnimation>
{
	const double originalScale = 0.8;

	protected override IReadOnlySet<string> AnimatedProperties { get; } = new HashSet<string> { nameof(VisualElement.Scale) };

	[Fact]
	public void EnsureDefaults()
	{
		var animation = new HeartbeatAnimation();

		Assert.Equal(BaseAnimationDefaults.Easing, animation.Easing);
		Assert.Equal(HeartbeatAnimationDefaults.Length, animation.Length);
		Assert.Equal(HeartbeatAnimationDefaults.MaximumScale, animation.MaximumScale);
		Assert.Equal(900u, animation.Length);
		Assert.Equal(1.3, animation.MaximumScale);
		Assert.Equal(BindingMode.OneWay, HeartbeatAnimation.MaximumScaleProperty.DefaultBindingMode);
	}

	[Theory]
	[InlineData(double.NaN)]
	[InlineData(double.PositiveInfinity)]
	[InlineData(double.NegativeInfinity)]
	public void NonFiniteValues_AreIgnored(double nonFiniteValue)
	{
		var animation = new HeartbeatAnimation { MaximumScale = nonFiniteValue };

		Assert.Equal(HeartbeatAnimationDefaults.MaximumScale, animation.MaximumScale);
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_PeaksAtTheMaximumScaleAndNeverShrinksBelowTheOriginalScale() => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateFunctionalAnimation();
		var view = CreateAnimatedViewWithNonDefaultValues();
		using var scales = new ExtremeValueRecorder(view, VisualElement.ScaleProperty);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		scales.Max.Should().BeApproximately(originalScale * animation.MaximumScale, precision);
		scales.Min.Should().BeGreaterThanOrEqualTo(originalScale - precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_HonorsCustomMaximumScale() => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateFunctionalAnimation();
		animation.MaximumScale = 1.5;
		var view = CreateAnimatedViewWithNonDefaultValues();
		using var scales = new ExtremeValueRecorder(view, VisualElement.ScaleProperty);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		scales.Max.Should().BeApproximately(originalScale * 1.5, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_BeatsTwiceAndEndsAtTheOriginalScale() => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateFunctionalAnimation();
		var view = CreateAnimatedViewWithNonDefaultValues();
		using var recorder = new PropertyRecorder(view);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		var halfwayScale = originalScale * (1 + animation.MaximumScale) / 2;
		var scales = recorder.ValuesOf(nameof(VisualElement.Scale)).ToList();
		var upwardCrossings = scales.Zip(scales.Skip(1)).Count(pair => pair.First < halfwayScale && pair.Second >= halfwayScale);
		upwardCrossings.Should().Be(2);
		scales[^1].Should().BeApproximately(originalScale, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_SecondBeatPeaksAtTheMaximumScaleToo() => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateFunctionalAnimation();
		var view = CreateAnimatedViewWithNonDefaultValues();
		using var recorder = new PropertyRecorder(view);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		var peakScale = originalScale * animation.MaximumScale;
		var scales = recorder.ValuesOf(nameof(VisualElement.Scale)).ToList();
		var peakIndexes = scales.Select((scale, index) => (scale, index)).Where(sample => Math.Abs(sample.scale - peakScale) <= precision).Select(sample => sample.index).ToList();
		peakIndexes.Should().HaveCountGreaterThanOrEqualTo(2);
		var troughBetweenPeaks = scales.Skip(peakIndexes[0]).Take(peakIndexes[^1] - peakIndexes[0]).Min();
		troughBetweenPeaks.Should().BeApproximately(originalScale, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_EasesBackToTheOriginalScaleInsteadOfSnapping() => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateFunctionalAnimation();
		var view = CreateAnimatedViewWithNonDefaultValues();
		using var recorder = new PropertyRecorder(view);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		var scales = recorder.ValuesOf(nameof(VisualElement.Scale)).ToList();
		scales.Last(scale => Math.Abs(scale - originalScale) > precision).Should().BeInRange(originalScale + precision, (originalScale * animation.MaximumScale) - precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_StartedAfterAnInterruptedScaleAnimation_BeatsRelativeToTheScaleBeforeIt() => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var scaleRun = new ScaleAnimation { Length = 400 }.Animate(view, token);
		await WaitForPropertyAsync(view, VisualElement.ScaleProperty, token, scale => Math.Abs(scale - originalScale) > 0.05);
		view.CancelAnimations();
		await scaleRun.WaitAsync(waitTimeout, token);
		Math.Abs(view.Scale - originalScale).Should().BeGreaterThan(0.01);

		var animation = CreateFunctionalAnimation();
		using var recorder = new PropertyRecorder(view);
		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		var scales = recorder.ValuesOf(nameof(VisualElement.Scale)).ToList();
		scales.Max().Should().BeApproximately(originalScale * animation.MaximumScale, precision);
		scales.Min().Should().BeGreaterThanOrEqualTo(originalScale - precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_PeaksAtTwentyAndSixtyPercentOfLength() => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateFunctionalAnimation();
		var view = CreateAnimatedViewWithNonDefaultValues();
		using var recorder = new FrameRecorder(view, VisualElement.ScaleProperty);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		var peakScale = originalScale * animation.MaximumScale;
		var peakFrames = recorder.Writes.Where(write => Math.Abs(write.Value - peakScale) <= precision).Select(write => write.Milliseconds).Distinct().ToList();
		peakFrames.Should().Equal(
			(long)Math.Ceiling(0.2 * animation.Length / frameMilliseconds) * frameMilliseconds,
			(long)Math.Ceiling(0.6 * animation.Length / frameMilliseconds) * frameMilliseconds);
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
		using var recorder = new FrameRecorder(view, VisualElement.ScaleProperty);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		var peakScale = originalScale * animation.MaximumScale;
		var scaleSegments = SinInOutSegments([0, 0.2, 0.4, 0.6, 1], [originalScale, peakScale, originalScale, peakScale, originalScale]);
		AssertEveryFrameFollowsTheKeyframes(recorder, VisualElement.ScaleProperty, scaleSegments, animation.Length, easing);
	}, testToken);
}
