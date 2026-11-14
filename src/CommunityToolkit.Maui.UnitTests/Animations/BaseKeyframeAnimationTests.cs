using System.ComponentModel;
using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

public abstract class BaseKeyframeAnimationTests<TAnimation> : BaseAnimationTests<TAnimation> where TAnimation : BaseAnimation, new()
{
	protected const int frameMilliseconds = 16;

	const double floatingPointNoise = 1e-9;

	protected abstract IReadOnlySet<string> AnimatedProperties { get; }

	protected override void Interrupt(VisualElement view) => view.AbortAnimation(KeyframeAnimationExtensions.AnimationHandle);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_ChangesOnlyItsAnimatedProperties() => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateFunctionalAnimation();
		var view = CreateAnimatedViewWithNonDefaultValues();
		using var recorder = new PropertyRecorder(view);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		var changedProperties = recorder.Changes.Select(change => change.Name).ToHashSet();
		changedProperties.Should().BeSubsetOf(AnimatedProperties);
		AnimatedProperties.Should().BeSubsetOf(changedProperties);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_ZeroLength_CompletesAndReturnsEveryProperty() => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateFunctionalAnimation();
		animation.Length = 0;
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		AssertValuesEqual(view, startingValues);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task AnimationBehavior_TriggeredWhileAnimating_ReturnsEveryPropertyToItsStartingValue() => RunOnSingleThreadAsync(async token =>
	{
		var animation = new ObservedAnimation(CreateFunctionalAnimation());
		var behavior = new AnimationBehavior
		{
			AnimationType = animation
		};
		var view = CreateAnimatedViewWithNonDefaultValues();
		view.Behaviors.Add(behavior);
		var startingValues = CaptureValues(view);

		var firstChange = WaitForAnyChangeAsync(view, startingValues, token);
		behavior.AnimateCommand.Execute(token);
		await firstChange;

		// AnimationBehavior stops the running animation and starts a new one, so two animations end
		behavior.AnimateCommand.Execute(token);
		await animation.WaitForEndedAnimationsAsync(2, token);

		AssertValuesEqual(view, startingValues);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task AnimationBehavior_TriggeredWhileAKeyframeAnimationRunsDirectly_EndsItBeforeStartingItsOwnAnimation() => RunOnSingleThreadAsync(async token =>
	{
		var keyframeAnimation = CreateAnimation();
		keyframeAnimation.Length = 2000;
		var otherAnimation = new ObservedAnimation(new FadeAnimation { Length = functionalTestLength });
		var behavior = new AnimationBehavior
		{
			AnimationType = otherAnimation
		};
		var view = CreateAnimatedViewWithNonDefaultValues();
		view.Behaviors.Add(behavior);
		var startingValues = CaptureValues(view);

		var firstChange = WaitForAnyChangeAsync(view, startingValues, token);
		var keyframeRun = keyframeAnimation.Animate(view, token);
		await firstChange;
		view.AnimationIsRunning(KeyframeAnimationExtensions.AnimationHandle).Should().BeTrue();

		behavior.AnimateCommand.Execute(token);

		view.AnimationIsRunning(KeyframeAnimationExtensions.AnimationHandle).Should().BeFalse();
		AssertAnimatedPropertiesEqual(view, startingValues);
		await keyframeRun.WaitAsync(waitTimeout, token);

		await otherAnimation.WaitForEndedAnimationsAsync(1, token);
		AssertValuesEqual(view, startingValues);
	}, TestContext.Current.CancellationToken);

	[Fact(Timeout = (int)TestDuration.Long)]
	public Task Animate_SpringOutEasing_ReturnsEveryPropertyToItsStartingValue() => RunOnSingleThreadAsync(async token =>
	{
		var animation = CreateFunctionalAnimation();
		animation.Easing = Easing.SpringOut;
		var view = CreateAnimatedViewWithNonDefaultValues();
		var startingValues = CaptureValues(view);

		await animation.Animate(view, token).WaitAsync(waitTimeout, token);

		AssertValuesEqual(view, startingValues);
	}, TestContext.Current.CancellationToken);

	protected void AssertAnimatedPropertiesEqual(VisualElement view, double[] expectedValues)
	{
		var actualValues = CaptureValues(view);

		for (var i = 0; i < animatableProperties.Length; i++)
		{
			if (AnimatedProperties.Contains(animatableProperties[i].Name))
			{
				actualValues[i].Should().BeApproximately(expectedValues[i], precision, animatableProperties[i].Name);
			}
		}
	}

	protected static KeyframeSegment[] SinInOutSegments(double[] fractions, double[] values) =>
		[.. fractions.Zip(values).Zip(fractions.Skip(1).Zip(values.Skip(1)), (start, end) => new KeyframeSegment(start.First, end.First, start.Second, end.Second, Easing.SinInOut))];

	// A segment that has not begun yet leaves the property at the value the previous one ended on, as the animation does.
	// Expects the animation to start on the tick the recorder started (a fresh view's mock clock starts at 0); where one segment ends and the next begins on the same tick, the later write wins.
	protected static double ValueAt(IReadOnlyList<KeyframeSegment> segments, double progress)
	{
		var value = segments[0].From;
		foreach (var (begin, end, from, to, easing) in segments)
		{
			if (progress >= end)
			{
				value = to;
			}
			else if (progress > begin)
			{
				return from + ((to - from) * easing.Ease((progress - begin) / (end - begin)));
			}
			else
			{
				break;
			}
		}

		return value;
	}

	protected static void AssertEveryFrameFollowsTheKeyframes(FrameRecorder recorder, BindableProperty property, IReadOnlyList<KeyframeSegment> segments, uint length, Easing? timelineEasing = null)
	{
		var lastExpectedFrame = (long)Math.Ceiling(length / (double)frameMilliseconds) * frameMilliseconds;
		recorder.Frames.Select(frame => frame.Milliseconds).Should().Contain(Enumerable.Range(1, (int)(lastExpectedFrame / frameMilliseconds)).Select(frameNumber => (long)frameNumber * frameMilliseconds));

		foreach (var frame in recorder.Frames)
		{
			var progress = (timelineEasing ?? Easing.Linear).Ease(Math.Min(1.0, frame.Milliseconds / (double)length));
			frame.ValueOf(property).Should().BeApproximately(ValueAt(segments, progress), precision, $"{property.PropertyName} at {frame.Milliseconds} ms");
		}
	}

	protected static List<double> GetTurningPoints(IEnumerable<double> values)
	{
		var distinctValues = new List<double>();
		foreach (var value in values)
		{
			if (distinctValues.Count is 0 || Math.Abs(value - distinctValues[^1]) > floatingPointNoise)
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

	protected readonly record struct KeyframeSegment(double Begin, double End, double From, double To, Easing Easing);

	protected readonly record struct Frame(long Milliseconds, IReadOnlyDictionary<BindableProperty, double> Values)
	{
		public double ValueOf(BindableProperty property) => Values[property];
	}

	protected readonly record struct Write(long Milliseconds, BindableProperty Property, double Value);

	// Frames record on every ticker frame, not only when PropertyChanged fires, so a value that should change but holds still is seen.
	// Writes record every change in between, because a segment that ends and the next one that begins write within the same frame.
	protected sealed class FrameRecorder : IDisposable
	{
		readonly VisualElement view;
		readonly BindableProperty[] properties;
		readonly long startMilliseconds;
		readonly List<Frame> frames = [];
		readonly List<Write> writes = [];
		readonly IDisposable frameObservation;

		public FrameRecorder(VisualElement view, params BindableProperty[] properties)
		{
			this.view = view;
			this.properties = properties;
			startMilliseconds = view.GetElapsedAnimationMilliseconds();
			frameObservation = view.ObserveAnimationFrames(RecordFrame);
			view.PropertyChanged += HandlePropertyChanged;
		}

		public IReadOnlyList<Frame> Frames => frames;

		public IReadOnlyList<Write> Writes => writes;

		public void Dispose()
		{
			frameObservation.Dispose();
			view.PropertyChanged -= HandlePropertyChanged;
		}

		void RecordFrame(long elapsedMilliseconds) =>
			frames.Add(new Frame(elapsedMilliseconds - startMilliseconds, properties.ToDictionary(property => property, property => (double)view.GetValue(property))));

		void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			var property = properties.FirstOrDefault(property => property.PropertyName == e.PropertyName);
			if (property is not null)
			{
				writes.Add(new Write(view.GetElapsedAnimationMilliseconds() - startMilliseconds, property, (double)view.GetValue(property)));
			}
		}
	}

	protected sealed class PropertyRecorder : IDisposable
	{
		readonly VisualElement view;
		readonly List<(string Name, double Value)> changes = [];

		public PropertyRecorder(VisualElement view)
		{
			this.view = view;
			view.PropertyChanged += HandlePropertyChanged;
		}

		public IReadOnlyList<(string Name, double Value)> Changes => changes;

		public IEnumerable<double> ValuesOf(string propertyName) => changes.Where(change => change.Name == propertyName).Select(change => change.Value);

		public void Dispose() => view.PropertyChanged -= HandlePropertyChanged;

		void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			var property = animatableProperties.FirstOrDefault(property => property.Name == e.PropertyName);
			if (property.Property is not null)
			{
				changes.Add((property.Name, (double)view.GetValue(property.Property)));
			}
		}
	}
}
