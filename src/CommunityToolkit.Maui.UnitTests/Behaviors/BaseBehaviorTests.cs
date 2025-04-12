using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public class BaseBehaviorTests
{
	[Fact]
	public void AttachAndDetachCallsShouldCorrectlyAssignView()
	{
		var label = new Label();
		var mockBehavior = new MockBehavior();

		mockBehavior.AssertViewIsNull();
		mockBehavior.IsAttached.Should().BeFalse();

		label.Behaviors.Add(mockBehavior);

		mockBehavior.AssertViewIsEqual(label);
		mockBehavior.IsAttached.Should().BeTrue();

		label.Behaviors.Remove(mockBehavior);

		mockBehavior.AssertViewIsNull();
		mockBehavior.IsAttached.Should().BeFalse();
	}

	[Fact]
	public void ViewPropertyChangesShouldBeHandledWithinBehavior()
	{
		var label = new Label();
		var mockBehavior = new MockBehavior();

		label.Behaviors.Add(mockBehavior);

		mockBehavior.PropertyChanges.Should().BeEmpty();

		label.Text = "Text";

		mockBehavior.PropertyChanges.Should().ContainSingle();
		var change = mockBehavior.PropertyChanges.Single();

		change.Should().NotBeNull();
		change.PropertyName.Should().Be(nameof(Label.Text));
	}
}