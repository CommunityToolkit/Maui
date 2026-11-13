using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Core;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

public class WobbleAnimationTests : BaseKeyframeAnimationTests<WobbleAnimation>
{
	const double originalTranslationX = 12;
	const double originalRotation = 30;

	protected override IReadOnlySet<string> AnimatedProperties { get; } = new HashSet<string> { nameof(VisualElement.TranslationX), nameof(VisualElement.Rotation) };

	[Fact]
	public void EnsureDefaults()
	{
		var animation = new WobbleAnimation();

		Assert.Equal(BaseAnimationDefaults.Easing, animation.Easing);
		Assert.Equal(WobbleAnimationDefaults.Length, animation.Length);
		Assert.Equal(WobbleAnimationDefaults.Distance, animation.Distance);
		Assert.Equal(WobbleAnimationDefaults.RotationAngle, animation.RotationAngle);
		Assert.Equal(1000u, animation.Length);
		Assert.Equal(25.0, animation.Distance);
		Assert.Equal(5.0, animation.RotationAngle);
	}

	[Theory]
	[InlineData(double.NaN)]
	[InlineData(double.PositiveInfinity)]
	[InlineData(double.NegativeInfinity)]
	public void NonFiniteValues_AreIgnored(double nonFiniteValue)
	{
		var animation = new WobbleAnimation
		{
			Distance = nonFiniteValue,
			RotationAngle = nonFiniteValue
		};

		Assert.Equal(WobbleAnimationDefaults.Distance, animation.Distance);
		Assert.Equal(WobbleAnimationDefaults.RotationAngle, animation.RotationAngle);
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_ReachesTheFirstSwayAndTiltAndTheLargestReturns() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new WobbleAnimation { Length = functionalTestLength };
		var view = CreateAnimatedViewWithNonDefaultValues();

		var translations = new ExtremeValueRecorder(view, VisualElement.TranslationXProperty);
		var rotations = new ExtremeValueRecorder(view, VisualElement.RotationProperty);
		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		translations.Min.Should().BeApproximately(originalTranslationX - animation.Distance, precision);
		translations.Max.Should().BeApproximately(originalTranslationX + (0.8 * animation.Distance), precision);
		rotations.Min.Should().BeApproximately(originalRotation - animation.RotationAngle, precision);
		rotations.Max.Should().BeApproximately(originalRotation + (0.6 * animation.RotationAngle), precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_SwaysByTheDecreasingDistancesInOrder() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new WobbleAnimation { Length = functionalTestLength };
		var view = CreateAnimatedViewWithNonDefaultValues();
		using var recorder = new PropertyRecorder(view);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		var distance = animation.Distance;
		var swayOffsets = GetTurningPoints(recorder.ValuesOf(nameof(VisualElement.TranslationX))).Select(translationX => translationX - originalTranslationX).ToList();
		swayOffsets.Should().HaveCount(5);
		swayOffsets.Zip([-distance, 0.8 * distance, -0.6 * distance, 0.4 * distance, -0.2 * distance]).Should().OnlyContain(pair => Math.Abs(pair.First - pair.Second) <= precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_TiltsByTheDecreasingAnglesInOrder() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new WobbleAnimation { Length = functionalTestLength };
		var view = CreateAnimatedViewWithNonDefaultValues();
		using var recorder = new PropertyRecorder(view);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		var angle = animation.RotationAngle;
		var tiltOffsets = GetTurningPoints(recorder.ValuesOf(nameof(VisualElement.Rotation))).Select(rotation => rotation - originalRotation).ToList();
		tiltOffsets.Should().HaveCount(5);
		tiltOffsets.Zip([-angle, 0.6 * angle, -0.6 * angle, 0.4 * angle, -0.2 * angle]).Should().OnlyContain(pair => Math.Abs(pair.First - pair.Second) <= precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_SwaysBackToRestInsteadOfSnapping() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new WobbleAnimation { Length = functionalTestLength };
		var view = CreateAnimatedViewWithNonDefaultValues();
		using var recorder = new PropertyRecorder(view);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		AssertReturnsGraduallyAfterLastTurningPoint(recorder.ValuesOf(nameof(VisualElement.TranslationX)).ToList(), originalTranslationX - (0.2 * animation.Distance), originalTranslationX);
		AssertReturnsGraduallyAfterLastTurningPoint(recorder.ValuesOf(nameof(VisualElement.Rotation)).ToList(), originalRotation - (0.2 * animation.RotationAngle), originalRotation);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_HonorsCustomDistanceAndRotationAngle() => RunOnSingleThreadAsync(async token =>
	{
		const double distance = 40;
		const double rotationAngle = 10;

		var animation = new WobbleAnimation { Distance = distance, RotationAngle = rotationAngle, Length = functionalTestLength };
		var view = CreateAnimatedViewWithNonDefaultValues();

		var translations = new ExtremeValueRecorder(view, VisualElement.TranslationXProperty);
		var rotations = new ExtremeValueRecorder(view, VisualElement.RotationProperty);
		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		translations.Min.Should().BeApproximately(originalTranslationX - distance, precision);
		translations.Max.Should().BeApproximately(originalTranslationX + (0.8 * distance), precision);
		rotations.Min.Should().BeApproximately(originalRotation - rotationAngle, precision);
		rotations.Max.Should().BeApproximately(originalRotation + (0.6 * rotationAngle), precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_NegativeDistance_SwaysToTheRightFirst() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new WobbleAnimation { Distance = -25, Length = functionalTestLength };
		var view = CreateAnimatedViewWithNonDefaultValues();
		using var recorder = new PropertyRecorder(view);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		var firstSwayOffset = GetTurningPoints(recorder.ValuesOf(nameof(VisualElement.TranslationX))).First() - originalTranslationX;
		firstSwayOffset.Should().BeApproximately(25, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_AfterAnInterruptedRotateAnimation_TiltsAroundTheOriginalRotation() => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var rotated = WaitForPropertyAsync(view, VisualElement.RotationProperty, token, rotation => rotation > originalRotation + 20);
		var rotateRun = new RotateAnimation { Length = 2000 }.Animate(view, token);
		await rotated;
		view.CancelAnimations();
		await rotateRun.WaitAsync(waitTimeout, token);
		view.Rotation.Should().BeGreaterThan(originalRotation + 20);

		var animation = new WobbleAnimation { Length = functionalTestLength };
		using var recorder = new PropertyRecorder(view);
		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		recorder.ValuesOf(nameof(VisualElement.Rotation)).Min().Should().BeApproximately(originalRotation - animation.RotationAngle, precision);
		recorder.ValuesOf(nameof(VisualElement.Rotation)).Max().Should().BeApproximately(originalRotation + (0.6 * animation.RotationAngle), precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_FollowsTheKeyframeCurvesOnEveryTick() => AssertFollowsTheKeyframeCurvesOnEveryTick(null, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_CustomEasing_WarpsTheProgressOfTheWholeRun() => AssertFollowsTheKeyframeCurvesOnEveryTick(Easing.CubicIn, TestContext.Current.CancellationToken);

	Task AssertFollowsTheKeyframeCurvesOnEveryTick(Easing? easing, CancellationToken testToken) => RunOnSingleThreadAsync(async token =>
	{
		var animation = new WobbleAnimation { Length = functionalTestLength, Easing = easing ?? BaseAnimationDefaults.Easing };
		var view = CreateAnimatedViewWithNonDefaultValues();
		var distance = animation.Distance;
		var angle = animation.RotationAngle;
		using var recorder = new FrameRecorder(view, VisualElement.TranslationXProperty, VisualElement.RotationProperty);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		double[] fractions = [0, 0.15, 0.3, 0.45, 0.6, 0.75, 1.0];
		var swayOffsets = new[] { 0, -distance, 0.8 * distance, -0.6 * distance, 0.4 * distance, -0.2 * distance, 0 };
		var tiltOffsets = new[] { 0, -angle, 0.6 * angle, -0.6 * angle, 0.4 * angle, -0.2 * angle, 0 };
		AssertEveryFrameFollowsTheKeyframes(recorder, VisualElement.TranslationXProperty, SinInOutSegments(fractions, [.. swayOffsets.Select(offset => originalTranslationX + offset)]), animation.Length, animation.Easing);
		AssertEveryFrameFollowsTheKeyframes(recorder, VisualElement.RotationProperty, SinInOutSegments(fractions, [.. tiltOffsets.Select(offset => originalRotation + offset)]), animation.Length, animation.Easing);
	}, testToken);

	static void AssertReturnsGraduallyAfterLastTurningPoint(List<double> values, double lastTurningPoint, double restingValue)
	{
		var lastTurningPointIndex = values.FindLastIndex(value => Math.Abs(value - lastTurningPoint) <= precision);
		lastTurningPointIndex.Should().BeGreaterThanOrEqualTo(0);
		values.Skip(lastTurningPointIndex + 1).Should().Contain(value => value > lastTurningPoint + precision && value < restingValue - precision);
	}
}
