using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
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
		var turningOffsets = TurningPoints(recorder.ValuesOf(nameof(VisualElement.Rotation))).Select(rotation => rotation - originalRotation).ToList();
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

		var firstTurningOffset = TurningPoints(recorder.ValuesOf(nameof(VisualElement.Rotation))).First() - originalRotation;
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
	public Task Animate_FollowsTheKeyframeCurveOnEveryTick() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new SwingAnimation { Length = functionalTestLength };
		var view = CreateAnimatedViewWithNonDefaultValues();
		var angle = animation.RotationAngle;
		var samples = TickSampler.Start(view, VisualElement.RotationProperty);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		samples.AssertFollows(animation.Length, [0, 0.2, 0.4, 0.6, 0.8, 1.0], [0, angle, -2 * angle / 3, angle / 3, -angle / 3, 0], originalRotation);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_CustomEasing_WarpsTheProgressOfTheWholeRun() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new SwingAnimation { Length = functionalTestLength, Easing = Easing.CubicIn };
		var view = CreateAnimatedViewWithNonDefaultValues();
		var angle = animation.RotationAngle;
		var samples = TickSampler.Start(view, VisualElement.RotationProperty);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		samples.AssertFollows(animation.Length, [0, 0.2, 0.4, 0.6, 0.8, 1.0], [0, angle, -2 * angle / 3, angle / 3, -angle / 3, 0], originalRotation, Easing.CubicIn);
	}, TestContext.Current.CancellationToken);

	static List<double> TurningPoints(IEnumerable<double> values)
	{
		var distinctValues = new List<double>();
		foreach (var value in values)
		{
			if (distinctValues.Count == 0 || Math.Abs(distinctValues[^1] - value) > 1e-9)
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

	sealed class TickSampler
	{
		readonly BindableProperty property;
		readonly long startMilliseconds;
		readonly SortedDictionary<long, double> lastValuePerTick = [];

		TickSampler(VisualElement view, BindableProperty property)
		{
			this.property = property;
			startMilliseconds = view.GetElapsedAnimationMilliseconds();
			view.PropertyChanged += (_, e) =>
			{
				if (e.PropertyName == property.PropertyName)
				{
					lastValuePerTick[view.GetElapsedAnimationMilliseconds() - startMilliseconds] = (double)view.GetValue(property);
				}
			};
		}

		public static TickSampler Start(VisualElement view, BindableProperty property) => new(view, property);

		public void AssertFollows(uint length, double[] fractions, double[] offsets, double originalValue, Easing? timelineEasing = null)
		{
			lastValuePerTick.Should().NotBeEmpty();
			foreach (var (elapsed, value) in lastValuePerTick)
			{
				var progress = (timelineEasing ?? Easing.Linear).Ease(Math.Min(1.0, elapsed / (double)length));
				var segment = 0;
				while (segment < fractions.Length - 2 && progress > fractions[segment + 1])
				{
					segment++;
				}

				var local = (progress - fractions[segment]) / (fractions[segment + 1] - fractions[segment]);
				var expected = originalValue + offsets[segment] + ((offsets[segment + 1] - offsets[segment]) * Easing.SinInOut.Ease(local));
				value.Should().BeApproximately(expected, precision, $"{property.PropertyName} at {elapsed} ms");
			}
		}
	}
}
