using System.ComponentModel;
using CommunityToolkit.Maui.Behaviors;
using FluentAssertions;

namespace CommunityToolkit.Maui.UnitTests.Mocks;

public class NumericValidationBehavior : Behavior<Entry>
{
	void OnViewPropertyChanged(Entry sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(Entry.Text))
		{
			bool isValid = double.TryParse(sender.Text, out double _);
			sender.TextColor = isValid ? Colors.Black : Colors.Red;
		}
	}
}

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