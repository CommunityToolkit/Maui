using CommunityToolkit.Maui.Animations;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

public class AnimatedPropertyTrackerTests : BaseTest
{
	[Fact]
	public void Start_AtRest_CapturesTheCurrentValueAsOriginal()
	{
		var tracker = AnimatedPropertyTracker.For(new Label(), VisualElement.RotationProperty);

		var (_, originalValue) = tracker.Start(30);

		originalValue.Should().Be(30);
	}

	[Fact]
	public void Start_WhileAnotherAnimationRuns_KeepsTheValueBeforeTheFirstAnimation()
	{
		var tracker = AnimatedPropertyTracker.For(new Label(), VisualElement.RotationProperty);
		var (firstId, _) = tracker.Start(30);

		var (secondId, originalValue) = tracker.Start(75);

		originalValue.Should().Be(30);
		tracker.IsLatest(firstId).Should().BeFalse();
		tracker.IsLatest(secondId).Should().BeTrue();
	}

	[Fact]
	public void Start_AfterAnInterruptedAnimationLeftAPartialValue_KeepsTheValueBeforeTheInterruptedAnimation()
	{
		var tracker = AnimatedPropertyTracker.For(new Label(), VisualElement.RotationProperty);
		var (id, _) = tracker.Start(30);
		tracker.End(id, 75);

		var (_, originalValue) = tracker.Start(75);

		originalValue.Should().Be(30);
	}

	[Fact]
	public void Start_AfterTheValueChangedFollowingAnInterruptedAnimation_CapturesTheNewValue()
	{
		var tracker = AnimatedPropertyTracker.For(new Label(), VisualElement.RotationProperty);
		var (id, _) = tracker.Start(30);
		tracker.End(id, 75);

		var (_, originalValue) = tracker.Start(10);

		originalValue.Should().Be(10);
	}

	[Fact]
	public void Start_AfterACompletedAnimation_CapturesTheCurrentValue()
	{
		var tracker = AnimatedPropertyTracker.For(new Label(), VisualElement.RotationProperty);
		var (id, _) = tracker.Start(30);
		tracker.End(id, 30);

		var (_, originalValue) = tracker.Start(50);

		originalValue.Should().Be(50);
	}

	[Fact]
	public void End_OfTheInterruptedAnimationAlone_DoesNotMakeTheViewRestForTheAnimationThatInterruptedIt()
	{
		var tracker = AnimatedPropertyTracker.For(new Label(), VisualElement.RotationProperty);
		var (firstId, _) = tracker.Start(30);
		tracker.Start(75);
		tracker.End(firstId, 75);

		var (_, originalValue) = tracker.Start(75);

		originalValue.Should().Be(30);
	}

	[Fact]
	public void For_SeparateViewsAreTrackedIndependently()
	{
		var view = new Label();
		var otherView = new Label();
		AnimatedPropertyTracker.For(view, VisualElement.RotationProperty).Start(30);

		var (_, otherOriginalValue) = AnimatedPropertyTracker.For(otherView, VisualElement.RotationProperty).Start(60);

		otherOriginalValue.Should().Be(60);
	}

	[Fact]
	public void End_IgnoresASecondEndOfTheSameAnimation()
	{
		var tracker = AnimatedPropertyTracker.For(new Label(), VisualElement.RotationProperty);
		var (animationId, _) = tracker.Start(30);

		tracker.End(animationId, 75);
		tracker.End(animationId, 10);
		var (_, originalValue) = tracker.Start(10);

		originalValue.Should().Be(10);
	}

	[Fact]
	public void For_SeparatePropertiesOfOneViewAreTrackedIndependently()
	{
		var view = new Label();
		AnimatedPropertyTracker.For(view, VisualElement.RotationProperty).Start(30);

		var (_, originalScale) = AnimatedPropertyTracker.For(view, VisualElement.ScaleProperty).Start(0.8);

		originalScale.Should().Be(0.8);
	}

	[Fact]
	public void For_ReturnsTheSameTrackerForTheSameViewAndProperty()
	{
		var view = new Label();

		AnimatedPropertyTracker.For(view, VisualElement.RotationProperty).Should().BeSameAs(AnimatedPropertyTracker.For(view, VisualElement.RotationProperty));
	}
}
