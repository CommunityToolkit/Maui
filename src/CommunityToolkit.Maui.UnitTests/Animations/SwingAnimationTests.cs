using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Core;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

public class SwingAnimationTests : BaseKeyframeAnimationTests<SwingAnimation>
{
	const double originalRotation = 30;
	const double originalAnchorY = 0.7;

	protected override IReadOnlySet<string> AnimatedProperties { get; } = new HashSet<string> { nameof(VisualElement.Rotation), nameof(VisualElement.AnchorY) };

	[Fact]
	public void EnsureDefaults()
	{
		var animation = new SwingAnimation();

		Assert.Equal(BaseAnimationDefaults.Easing, animation.Easing);
		Assert.Equal(SwingAnimationDefaults.Length, animation.Length);
		Assert.Equal(SwingAnimationDefaults.RotationAngle, animation.RotationAngle);
		Assert.Equal(1000u, animation.Length);
		Assert.Equal(15.0, animation.RotationAngle);
	}

	[Theory]
	[InlineData(double.NaN)]
	[InlineData(double.PositiveInfinity)]
	[InlineData(double.NegativeInfinity)]
	public void NonFiniteValues_AreIgnored(double nonFiniteValue)
	{
		var animation = new SwingAnimation { RotationAngle = nonFiniteValue };

		Assert.Equal(SwingAnimationDefaults.RotationAngle, animation.RotationAngle);
	}

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_HangsFromTheTopEdgeWheneverItRotates() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new SwingAnimation { Length = functionalTestLength };
		var view = CreateAnimatedViewWithNonDefaultValues();
		using var recorder = new PropertyRecorder(view);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		var anchorY = originalAnchorY;
		var rotationChangeCount = 0;
		foreach (var (name, value) in recorder.Changes)
		{
			if (name == nameof(VisualElement.AnchorY))
			{
				anchorY = value;
			}
			else if (name == nameof(VisualElement.Rotation))
			{
				rotationChangeCount++;
				anchorY.Should().BeApproximately(0, precision);
			}
		}

		rotationChangeCount.Should().BeGreaterThan(0);
		recorder.ValuesOf(nameof(VisualElement.AnchorY)).Last().Should().BeApproximately(originalAnchorY, precision);
		recorder.ValuesOf(nameof(VisualElement.AnchorX)).Should().BeEmpty();
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_ReachesTheFirstSwingAndTheLargestBackSwing() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new SwingAnimation { Length = functionalTestLength };
		var view = CreateAnimatedViewWithNonDefaultValues();

		var rotations = new ExtremeValueRecorder(view, VisualElement.RotationProperty);
		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		rotations.Max.Should().BeApproximately(originalRotation + animation.RotationAngle, precision);
		rotations.Min.Should().BeApproximately(originalRotation - (2 * animation.RotationAngle / 3), precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_SwingsByTheDecreasingAnglesInOrder() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new SwingAnimation { Length = functionalTestLength };
		var view = CreateAnimatedViewWithNonDefaultValues();
		using var recorder = new PropertyRecorder(view);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		var angle = animation.RotationAngle;
		var turningOffsets = GetTurningPoints(recorder.ValuesOf(nameof(VisualElement.Rotation))).Select(rotation => rotation - originalRotation).ToList();
		turningOffsets.Should().HaveCount(4);
		turningOffsets.Zip([angle, -2 * angle / 3, angle / 3, -angle / 3]).Should().OnlyContain(pair => Math.Abs(pair.First - pair.Second) <= precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_SwingsBackToRestInsteadOfSnapping() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new SwingAnimation { Length = functionalTestLength };
		var view = CreateAnimatedViewWithNonDefaultValues();
		using var recorder = new PropertyRecorder(view);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		var rotations = recorder.ValuesOf(nameof(VisualElement.Rotation)).ToList();
		var lastTurningPoint = originalRotation - (animation.RotationAngle / 3);
		var lastTurningPointIndex = rotations.FindLastIndex(rotation => Math.Abs(rotation - lastTurningPoint) <= precision);
		lastTurningPointIndex.Should().BeGreaterThanOrEqualTo(0);
		rotations.Skip(lastTurningPointIndex + 1).Should().Contain(rotation => rotation > lastTurningPoint + precision && rotation < originalRotation - precision);
	}, TestContext.Current.CancellationToken);

	[Theory(Timeout = (int)TestDuration.Long)]
	[InlineData(40)]
	[InlineData(-20)]
	public Task Animate_HonorsCustomRotationAngle(double rotationAngle) => RunOnSingleThreadAsync(async token =>
	{
		var animation = new SwingAnimation { RotationAngle = rotationAngle, Length = functionalTestLength };
		var view = CreateAnimatedViewWithNonDefaultValues();
		using var recorder = new PropertyRecorder(view);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		var firstTurningOffset = GetTurningPoints(recorder.ValuesOf(nameof(VisualElement.Rotation))).First() - originalRotation;
		firstTurningOffset.Should().BeApproximately(rotationAngle, precision);
		recorder.ValuesOf(nameof(VisualElement.Rotation)).Min().Should().BeApproximately(originalRotation + Math.Min(rotationAngle, -2 * rotationAngle / 3), precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_AfterAnInterruptedRotateAnimation_SwingsAroundTheOriginalRotation() => RunOnSingleThreadAsync(async token =>
	{
		var view = CreateAnimatedViewWithNonDefaultValues();
		var rotated = WaitForPropertyAsync(view, VisualElement.RotationProperty, token, rotation => rotation > originalRotation + 20);
		var rotateRun = new RotateAnimation { Length = 2000 }.Animate(view, token);
		await rotated;
		view.CancelAnimations();
		await rotateRun.WaitAsync(waitTimeout, token);
		view.Rotation.Should().BeGreaterThan(originalRotation + 20);

		var animation = new SwingAnimation { Length = functionalTestLength };
		using var recorder = new PropertyRecorder(view);
		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		recorder.ValuesOf(nameof(VisualElement.Rotation)).Max().Should().BeApproximately(originalRotation + animation.RotationAngle, precision);
		recorder.ValuesOf(nameof(VisualElement.Rotation)).Min().Should().BeApproximately(originalRotation - (2 * animation.RotationAngle / 3), precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_CancelledMidSwing_ReturnsRotationWhileStillHangingFromTheTopEdge() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new SwingAnimation { Length = functionalTestLength };
		var view = CreateAnimatedViewWithNonDefaultValues();
		using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
		using var recorder = new PropertyRecorder(view);

		var swung = WaitForPropertyAsync(view, VisualElement.RotationProperty, token, rotation => rotation > originalRotation + 5);
		var run = animation.Animate(view, cancellationTokenSource.Token);
		await swung;
		cancellationTokenSource.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(waitTimeout, token));

		var anchorY = originalAnchorY;
		foreach (var (name, value) in recorder.Changes)
		{
			if (name == nameof(VisualElement.AnchorY))
			{
				anchorY = value;
			}
			else if (name == nameof(VisualElement.Rotation))
			{
				anchorY.Should().BeApproximately(0, precision);
			}
		}

		view.Rotation.Should().BeApproximately(originalRotation, precision);
		view.AnchorY.Should().BeApproximately(originalAnchorY, precision);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_FollowsTheKeyframeCurveOnEveryTick() => AssertFollowsTheKeyframeCurveOnEveryTick(null, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_CustomEasing_WarpsTheProgressOfTheWholeRun() => AssertFollowsTheKeyframeCurveOnEveryTick(Easing.CubicIn, TestContext.Current.CancellationToken);

	Task AssertFollowsTheKeyframeCurveOnEveryTick(Easing? easing, CancellationToken testToken) => RunOnSingleThreadAsync(async token =>
	{
		var animation = new SwingAnimation { Length = functionalTestLength, Easing = easing ?? BaseAnimationDefaults.Easing };
		var view = CreateAnimatedViewWithNonDefaultValues();
		var angle = animation.RotationAngle;
		using var recorder = new FrameRecorder(view, VisualElement.RotationProperty);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		var rotations = SinInOutSegments([0, 0.2, 0.4, 0.6, 0.8, 1.0], [.. new[] { 0, angle, -2 * angle / 3, angle / 3, -angle / 3, 0 }.Select(offset => originalRotation + offset)]);
		AssertEveryFrameFollowsTheKeyframes(recorder, VisualElement.RotationProperty, rotations, animation.Length, animation.Easing);
	}, testToken);
}
