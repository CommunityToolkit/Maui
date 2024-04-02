using CommunityToolkit.Maui.Behaviors;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public abstract class BaseBehaviorTest<TBehavior, TView>(TBehavior behavior, TView view) : BaseTest
	where TBehavior : ICommunityToolkitBehavior<TView>
	where TView : VisualElement
{
	readonly ICommunityToolkitBehavior<TView> behavior = behavior;
	readonly TView view = view;

	[Fact]
	public void EnsureTrySetBindingContext()
	{
		var trueResult = behavior.TrySetBindingContext(view, new Binding
		{
			Path = BindableObject.BindingContextProperty.PropertyName,
			Source = view
		});
		
		Assert.True(trueResult);
		
		var falseResult = behavior.TrySetBindingContext(view, new Binding
		{
			Path = BindableObject.BindingContextProperty.PropertyName,
			Source = view
		});
		
		Assert.False(falseResult);
	}
	
	[Fact]
	public void EnsureTryRemoveBindingContext()
	{
		var falseResult = behavior.TryRemoveBindingContext(view);
		
		Assert.False(falseResult);
		
		behavior.TrySetBindingContext(view, new Binding
		{
			Path = BindableObject.BindingContextProperty.PropertyName,
			Source = view
		});
		
		var trueResult = behavior.TryRemoveBindingContext(view);
		
		Assert.True(trueResult);
	}

	protected class MockValidationBehavior : ValidationBehavior<string>
	{
		public string? ExpectedValue { get; init; }
		public bool SimulateValidationDelay { get; init; } = false;

		protected override async ValueTask<bool> ValidateAsync(string? value, CancellationToken token)
		{
			if (SimulateValidationDelay)
			{
				await Task.Delay(500, token);
			}

			return value == ExpectedValue;
		}
	}
}