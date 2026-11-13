using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.UnitTests.Mocks;
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
		var swayOffsets = TurningPoints(recorder.ValuesOf(nameof(VisualElement.TranslationX))).Select(translationX => translationX - originalTranslationX).ToList();
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
		var tiltOffsets = TurningPoints(recorder.ValuesOf(nameof(VisualElement.Rotation))).Select(rotation => rotation - originalRotation).ToList();
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

		var firstSwayOffset = TurningPoints(recorder.ValuesOf(nameof(VisualElement.TranslationX))).First() - originalTranslationX;
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
	public Task Animate_FollowsTheKeyframeCurvesOnEveryTick() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new WobbleAnimation { Length = functionalTestLength };
		var view = CreateAnimatedViewWithNonDefaultValues();
		var distance = animation.Distance;
		var angle = animation.RotationAngle;
		var translationSamples = TickSampler.Start(view, VisualElement.TranslationXProperty);
		var rotationSamples = TickSampler.Start(view, VisualElement.RotationProperty);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		double[] fractions = [0, 0.15, 0.3, 0.45, 0.6, 0.75, 1.0];
		translationSamples.AssertFollows(animation.Length, fractions, [0, -distance, 0.8 * distance, -0.6 * distance, 0.4 * distance, -0.2 * distance, 0], originalTranslationX);
		rotationSamples.AssertFollows(animation.Length, fractions, [0, -angle, 0.6 * angle, -0.6 * angle, 0.4 * angle, -0.2 * angle, 0], originalRotation);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_CustomEasing_WarpsTheProgressOfTheWholeRun() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new WobbleAnimation { Length = functionalTestLength, Easing = Easing.CubicIn };
		var view = CreateAnimatedViewWithNonDefaultValues();
		var distance = animation.Distance;
		var angle = animation.RotationAngle;
		var translationSamples = TickSampler.Start(view, VisualElement.TranslationXProperty);
		var rotationSamples = TickSampler.Start(view, VisualElement.RotationProperty);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		double[] fractions = [0, 0.15, 0.3, 0.45, 0.6, 0.75, 1.0];
		translationSamples.AssertFollows(animation.Length, fractions, [0, -distance, 0.8 * distance, -0.6 * distance, 0.4 * distance, -0.2 * distance, 0], originalTranslationX, Easing.CubicIn);
		rotationSamples.AssertFollows(animation.Length, fractions, [0, -angle, 0.6 * angle, -0.6 * angle, 0.4 * angle, -0.2 * angle, 0], originalRotation, Easing.CubicIn);
	}, TestContext.Current.CancellationToken);

	static void AssertReturnsGraduallyAfterLastTurningPoint(List<double> values, double lastTurningPoint, double restingValue)
	{
		var lastTurningPointIndex = values.FindLastIndex(value => Math.Abs(value - lastTurningPoint) <= precision);
		lastTurningPointIndex.Should().BeGreaterThanOrEqualTo(0);
		values.Skip(lastTurningPointIndex + 1).Should().Contain(value => value > lastTurningPoint + precision && value < restingValue - precision);
	}

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
