using System.ComponentModel;
using CommunityToolkit.Maui.Behaviors;
using FluentAssertions;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

public class MockBehavior : BaseBehavior<Label>
{
	internal bool IsAttached { get; private set; }
	
	protected override void OnAttachedTo(Label bindable)
	{
		base.OnAttachedTo(bindable);
		IsAttached = true;
	}

	protected override void OnDetachingFrom(Label bindable)
	{
		base.OnDetachingFrom(bindable);
		IsAttached = false;
	}

	internal void AssertViewIsEqual(Label label) => View.Should().Be(label);

	internal void AssertViewIsNull() => View.Should().BeNull();

	protected override void OnViewPropertyChanged(Label sender, PropertyChangedEventArgs e)
	{
		base.OnViewPropertyChanged(sender, e);
		
		PropertyChanges.Add(e);
	}

	internal List<PropertyChangedEventArgs> PropertyChanges { get; } = [];
}