using System.ComponentModel;
using CommunityToolkit.Maui.Animations;
using CommunityToolkit.Maui.Behaviors;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

public abstract class BaseKeyframeAnimationTests<TAnimation> : BaseAnimationTests<TAnimation> where TAnimation : BaseAnimation, new()
{
	protected abstract IReadOnlySet<string> AnimatedProperties { get; }

	// A keyframe animation is stopped by aborting its own handle, which restores the view; CancelAnimations does not reach it
	protected override bool InterruptionLeavesViewMidAnimation => false;

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

	protected sealed class ObservedAnimation(BaseAnimation inner) : BaseAnimation
	{
		readonly EndedAnimations endedAnimations = new();

		public Task WaitForEndedAnimationsAsync(int count, CancellationToken token) => endedAnimations.WaitAsync(count, token);

		public override async Task Animate(VisualElement view, CancellationToken token = default)
		{
			try
			{
				await inner.Animate(view, token);
			}
			finally
			{
				endedAnimations.NotifyEnded();
			}
		}
	}
}
