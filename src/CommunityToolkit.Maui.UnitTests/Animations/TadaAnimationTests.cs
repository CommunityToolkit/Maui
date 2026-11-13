using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Core;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

public class TadaAnimationTests : BaseKeyframeAnimationTests<TadaAnimation>
{
	const double originalScale = 0.8;
	const double originalRotation = 30;

	protected override IReadOnlySet<string> AnimatedProperties { get; } = new HashSet<string> { nameof(VisualElement.Scale), nameof(VisualElement.Rotation) };

	[Fact]
	public void EnsureDefaults()
	{
		var animation = new TadaAnimation();

		Assert.Equal(BaseAnimationDefaults.Easing, animation.Easing);
		Assert.Equal(TadaAnimationDefaults.Length, animation.Length);
		Assert.Equal(TadaAnimationDefaults.MinimumScale, animation.MinimumScale);
		Assert.Equal(TadaAnimationDefaults.MaximumScale, animation.MaximumScale);
		Assert.Equal(TadaAnimationDefaults.RotationAngle, animation.RotationAngle);
		Assert.Equal(1000u, animation.Length);
		Assert.Equal(0.9, animation.MinimumScale);
		Assert.Equal(1.1, animation.MaximumScale);
		Assert.Equal(3.0, animation.RotationAngle);
		Assert.Equal(BindingMode.OneWay, TadaAnimation.MinimumScaleProperty.DefaultBindingMode);
		Assert.Equal(BindingMode.OneWay, TadaAnimation.MaximumScaleProperty.DefaultBindingMode);
		Assert.Equal(BindingMode.OneWay, TadaAnimation.RotationAngleProperty.DefaultBindingMode);
	}

	[Theory]
	[InlineData(double.NaN)]
	[InlineData(double.PositiveInfinity)]
	[InlineData(double.NegativeInfinity)]
	public void NonFiniteValues_AreIgnored(double nonFiniteValue)
	{
		var animation = new TadaAnimation
		{
			MinimumScale = nonFiniteValue,
			MaximumScale = nonFiniteValue,
			RotationAngle = nonFiniteValue
		};

		Assert.Equal(TadaAnimationDefaults.MinimumScale, animation.MinimumScale);
		Assert.Equal(TadaAnimationDefaults.MaximumScale, animation.MaximumScale);
		Assert.Equal(TadaAnimationDefaults.RotationAngle, animation.RotationAngle);
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_ReachesTheScaleAndRotationExtremesRelativeToTheOriginalValues() => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateFunctionalAnimation();
		var view = CreateAnimatedViewWithNonDefaultValues();
		using var scales = new ExtremeValueRecorder(view, VisualElement.ScaleProperty);
		using var rotations = new ExtremeValueRecorder(view, VisualElement.RotationProperty);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		scales.Min.Should().BeApproximately(originalScale * animation.MinimumScale, precision);
		scales.Max.Should().BeApproximately(originalScale * animation.MaximumScale, precision);
		rotations.Min.Should().BeApproximately(originalRotation - animation.RotationAngle, precision);
		rotations.Max.Should().BeApproximately(originalRotation + animation.RotationAngle, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_HonorsCustomValues() => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateFunctionalAnimation();
		animation.MinimumScale = 0.5;
		animation.MaximumScale = 1.5;
		animation.RotationAngle = 10;
		var view = CreateAnimatedViewWithNonDefaultValues();
		using var scales = new ExtremeValueRecorder(view, VisualElement.ScaleProperty);
		using var rotations = new ExtremeValueRecorder(view, VisualElement.RotationProperty);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		scales.Min.Should().BeApproximately(originalScale * 0.5, precision);
		scales.Max.Should().BeApproximately(originalScale * 1.5, precision);
		rotations.Min.Should().BeApproximately(originalRotation - 10, precision);
		rotations.Max.Should().BeApproximately(originalRotation + 10, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_ShrinksBeforeItGrows() => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateFunctionalAnimation();
		var view = CreateAnimatedViewWithNonDefaultValues();
		using var recorder = new PropertyRecorder(view);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		var scales = recorder.ValuesOf(nameof(VisualElement.Scale)).ToList();
		var shrunkScale = originalScale * animation.MinimumScale;
		var grownScale = originalScale * animation.MaximumScale;
		var firstShrunkIndex = scales.FindIndex(scale => Math.Abs(scale - shrunkScale) <= precision);
		var firstGrownIndex = scales.FindIndex(scale => Math.Abs(scale - grownScale) <= precision);
		firstShrunkIndex.Should().BeGreaterThanOrEqualTo(0);
		firstGrownIndex.Should().BeGreaterThan(firstShrunkIndex);
		scales.Take(firstShrunkIndex).Should().OnlyContain(scale => scale <= originalScale + precision && scale >= shrunkScale - precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_TiltsLeftFirstThenWigglesAlternatingBetweenBothSidesFourTimes() => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateFunctionalAnimation();
		var view = CreateAnimatedViewWithNonDefaultValues();
		using var recorder = new PropertyRecorder(view);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		var tiltSides = new List<int>();
		foreach (var rotation in recorder.ValuesOf(nameof(VisualElement.Rotation)))
		{
			var offset = rotation - originalRotation;
			if (Math.Abs(offset) < animation.RotationAngle - precision)
			{
				continue;
			}

			var side = Math.Sign(offset);
			if (tiltSides.Count is 0 || tiltSides[^1] != side)
			{
				tiltSides.Add(side);
			}
		}

		tiltSides.Should().Equal(-1, 1, -1, 1, -1, 1, -1, 1);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_EndsWithAMovementBackToTheOriginalValuesInsteadOfSnapping() => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateFunctionalAnimation();
		var view = CreateAnimatedViewWithNonDefaultValues();
		using var recorder = new PropertyRecorder(view);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		var rotations = recorder.ValuesOf(nameof(VisualElement.Rotation)).ToList();
		var scales = recorder.ValuesOf(nameof(VisualElement.Scale)).ToList();
		rotations[^1].Should().BeApproximately(originalRotation, precision);
		scales[^1].Should().BeApproximately(originalScale, precision);
		rotations.Last(rotation => Math.Abs(rotation - originalRotation) > precision).Should().BeInRange(originalRotation + precision, originalRotation + animation.RotationAngle - precision);
		scales.Last(scale => Math.Abs(scale - originalScale) > precision).Should().BeInRange(originalScale + precision, (originalScale * animation.MaximumScale) - precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_StartedAfterInterruptedScaleAndRotateAnimations_MovesRelativeToTheValuesBeforeThem() => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var scaleRun = new ScaleAnimation { Length = 400 }.Animate(view, token);
		var rotateRun = new RotateAnimation { Length = 400 }.Animate(view, token);
		await WaitForPropertyAsync(view, VisualElement.RotationProperty, token, rotation => Math.Abs(rotation - originalRotation) > 5);
		view.CancelAnimations();
		await Task.WhenAll(scaleRun, rotateRun).WaitAsync(waitTimeout, token);
		Math.Abs(view.Scale - originalScale).Should().BeGreaterThan(0.01);
		Math.Abs(view.Rotation - originalRotation).Should().BeGreaterThan(1);

		var animation = CreateFunctionalAnimation();
		using var recorder = new PropertyRecorder(view);
		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		var scales = recorder.ValuesOf(nameof(VisualElement.Scale)).ToList();
		var rotations = recorder.ValuesOf(nameof(VisualElement.Rotation)).ToList();
		scales.Min().Should().BeApproximately(originalScale * animation.MinimumScale, precision);
		scales.Max().Should().BeApproximately(originalScale * animation.MaximumScale, precision);
		rotations.Min().Should().BeApproximately(originalRotation - animation.RotationAngle, precision);
		rotations.Max().Should().BeApproximately(originalRotation + animation.RotationAngle, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_HoldsTheShrunkTiltedKeyframeUntilTwentyPercentOfLength() => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateFunctionalAnimation();
		var view = CreateAnimatedViewWithNonDefaultValues();
		using var recorder = new FrameRecorder(view, VisualElement.ScaleProperty, VisualElement.RotationProperty);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		var lastFrameBeforeTwentyPercent = ((int)Math.Ceiling(0.2 * animation.Length / frameMilliseconds) - 1) * frameMilliseconds;
		((double)lastFrameBeforeTwentyPercent).Should().BeGreaterThanOrEqualTo(0.1 * animation.Length);
		var endOfThatFrame = recorder.Frames.Last(frame => frame.Milliseconds <= lastFrameBeforeTwentyPercent);
		endOfThatFrame.ValueOf(VisualElement.ScaleProperty).Should().BeApproximately(originalScale * animation.MinimumScale, precision);
		endOfThatFrame.ValueOf(VisualElement.RotationProperty).Should().BeApproximately(originalRotation - animation.RotationAngle, precision);
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
		using var recorder = new FrameRecorder(view, VisualElement.ScaleProperty, VisualElement.RotationProperty);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		var shrunkScale = originalScale * animation.MinimumScale;
		var grownScale = originalScale * animation.MaximumScale;
		var left = originalRotation - animation.RotationAngle;
		var right = originalRotation + animation.RotationAngle;
		var scaleSegments = SinInOutSegments([0, 0.1, 0.2, 0.3, 0.9, 1], [originalScale, shrunkScale, shrunkScale, grownScale, grownScale, originalScale]);
		var rotationSegments = SinInOutSegments([0, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1], [originalRotation, left, left, right, left, right, left, right, left, right, originalRotation]);
		AssertEveryFrameFollowsTheKeyframes(recorder, VisualElement.ScaleProperty, scaleSegments, animation.Length, easing);
		AssertEveryFrameFollowsTheKeyframes(recorder, VisualElement.RotationProperty, rotationSegments, animation.Length, easing);
	}, testToken);
}
