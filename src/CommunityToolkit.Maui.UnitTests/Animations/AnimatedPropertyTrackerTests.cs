using CommunityToolkit.Maui.Animations;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Animations;

public class AnimatedPropertyTrackerTests : BaseTest
{
	[Fact]
	public void StartAnimationAndGetOriginalValue_AtRest_CapturesTheCurrentValueAsOriginal()
	{
		var tracker = AnimatedPropertyTracker.GetOrCreate(new Label(), VisualElement.RotationProperty);

		var (_, originalValue) = tracker.StartAnimationAndGetOriginalValue(30);

		originalValue.Should().Be(30);
	}

	[Fact]
	public void StartAnimationAndGetOriginalValue_WhileAnotherAnimationRuns_KeepsTheValueBeforeTheFirstAnimation()
	{
		var tracker = AnimatedPropertyTracker.GetOrCreate(new Label(), VisualElement.RotationProperty);
		var (firstId, _) = tracker.StartAnimationAndGetOriginalValue(30);

		var (secondId, originalValue) = tracker.StartAnimationAndGetOriginalValue(75);

		originalValue.Should().Be(30);
		tracker.IsLatest(firstId).Should().BeFalse();
		tracker.IsLatest(secondId).Should().BeTrue();
	}

	[Fact]
	public void StartAnimationAndGetOriginalValue_AfterAnInterruptedAnimationLeftAPartialValue_KeepsTheValueBeforeTheInterruptedAnimation()
	{
		var tracker = AnimatedPropertyTracker.GetOrCreate(new Label(), VisualElement.RotationProperty);
		var (id, _) = tracker.StartAnimationAndGetOriginalValue(30);
		tracker.EndAnimation(id, 75);

		var (_, originalValue) = tracker.StartAnimationAndGetOriginalValue(75);

		originalValue.Should().Be(30);
	}

	[Fact]
	public void StartAnimationAndGetOriginalValue_AfterTheValueChangedFollowingAnInterruptedAnimation_CapturesTheNewValue()
	{
		var tracker = AnimatedPropertyTracker.GetOrCreate(new Label(), VisualElement.RotationProperty);
		var (id, _) = tracker.StartAnimationAndGetOriginalValue(30);
		tracker.EndAnimation(id, 75);

		var (_, originalValue) = tracker.StartAnimationAndGetOriginalValue(10);

		originalValue.Should().Be(10);
	}

	[Fact]
	public void StartAnimationAndGetOriginalValue_AfterACompletedAnimation_CapturesTheCurrentValue()
	{
		var tracker = AnimatedPropertyTracker.GetOrCreate(new Label(), VisualElement.RotationProperty);
		var (id, _) = tracker.StartAnimationAndGetOriginalValue(30);
		tracker.EndAnimation(id, 30);

		var (_, originalValue) = tracker.StartAnimationAndGetOriginalValue(50);

		originalValue.Should().Be(50);
	}

	[Fact]
	public void EndAnimation_OfTheInterruptedAnimationAlone_DoesNotMakeTheViewRestForTheAnimationThatInterruptedIt()
	{
		var tracker = AnimatedPropertyTracker.GetOrCreate(new Label(), VisualElement.RotationProperty);
		var (firstId, _) = tracker.StartAnimationAndGetOriginalValue(30);
		tracker.StartAnimationAndGetOriginalValue(75);
		tracker.EndAnimation(firstId, 75);

		var (_, originalValue) = tracker.StartAnimationAndGetOriginalValue(75);

		originalValue.Should().Be(30);
	}

	[Fact]
	public void StartAnimationAndGetOriginalValue_AfterAnInterruptedAnimationHandedTheValueOverToTheApp_CapturesTheValueTheAppHolds()
	{
		var tracker = AnimatedPropertyTracker.GetOrCreate(new Label(), VisualElement.RotationProperty);
		var (id, _) = tracker.StartAnimationAndGetOriginalValue(30);
		tracker.EndAnimation(id, 75);
		tracker.RecordHandOverToApp(id);

		var (_, originalValue) = tracker.StartAnimationAndGetOriginalValue(75);

		originalValue.Should().Be(75);
	}

	[Fact]
	public void RecordHandOverToApp_OfAnAnimationAnotherOneReplaced_KeepsTheOriginalValueBeforeTheFirstAnimation()
	{
		var tracker = AnimatedPropertyTracker.GetOrCreate(new Label(), VisualElement.RotationProperty);
		var (firstId, _) = tracker.StartAnimationAndGetOriginalValue(30);
		var (secondId, _) = tracker.StartAnimationAndGetOriginalValue(75);
		tracker.EndAnimation(firstId, 75);

		tracker.EndAnimation(secondId, 75);
		tracker.RecordHandOverToApp(firstId);
		var (_, originalValue) = tracker.StartAnimationAndGetOriginalValue(75);

		originalValue.Should().Be(30);
	}

	[Fact]
	public void GetOrCreate_SeparateViewsAreTrackedIndependently()
	{
		var view = new Label();
		var otherView = new Label();
		AnimatedPropertyTracker.GetOrCreate(view, VisualElement.RotationProperty).StartAnimationAndGetOriginalValue(30);

		var (_, otherOriginalValue) = AnimatedPropertyTracker.GetOrCreate(otherView, VisualElement.RotationProperty).StartAnimationAndGetOriginalValue(60);

		otherOriginalValue.Should().Be(60);
	}

	[Fact]
	public void EndAnimation_IgnoresASecondEndOfTheSameAnimation()
	{
		var tracker = AnimatedPropertyTracker.GetOrCreate(new Label(), VisualElement.RotationProperty);
		var (animationId, _) = tracker.StartAnimationAndGetOriginalValue(30);

		tracker.EndAnimation(animationId, 75);
		tracker.EndAnimation(animationId, 10);
		var (_, originalValue) = tracker.StartAnimationAndGetOriginalValue(10);

		originalValue.Should().Be(10);
	}

	[Fact]
	public void GetOrCreate_SeparatePropertiesOfOneViewAreTrackedIndependently()
	{
		var view = new Label();
		AnimatedPropertyTracker.GetOrCreate(view, VisualElement.RotationProperty).StartAnimationAndGetOriginalValue(30);

		var (_, originalScale) = AnimatedPropertyTracker.GetOrCreate(view, VisualElement.ScaleProperty).StartAnimationAndGetOriginalValue(0.8);

		originalScale.Should().Be(0.8);
	}

	[Fact]
	public void GetOrCreate_ReturnsTheSameTrackerForTheSameViewAndProperty()
	{
		var view = new Label();

		AnimatedPropertyTracker.GetOrCreate(view, VisualElement.RotationProperty).Should().BeSameAs(AnimatedPropertyTracker.GetOrCreate(view, VisualElement.RotationProperty));
	}
}
