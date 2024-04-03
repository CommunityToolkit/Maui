using CommunityToolkit.Maui.Behaviors;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Behaviors;

public abstract class BaseBehaviorTest<TBehavior, TView> : BaseTest
	where TBehavior : ICommunityToolkitBehavior<TView>
	where TView : VisualElement
{
	readonly ICommunityToolkitBehavior<TView> behavior;
	readonly TView view;

	protected BaseBehaviorTest(TBehavior behavior, TView view)
	{
		view.BindingContext = new MockViewModel();
		this.view = view;

		this.behavior = behavior;
	}

	[Fact]
	public void EnsureICommunityToolkitBehaviorIsBehavior()
	{
		Assert.IsAssignableFrom<Behavior>(behavior);
	}

	[Fact]
	public void VerifyTrySetBindingContextIsCalledWhenViewAttached()
	{
		view.Behaviors.Add((Behavior)behavior);

		var falseResult = behavior.TrySetBindingContext();
		
		Assert.False(falseResult);
	}
	
	[Fact]
	public void EnsureTryRemoveBindingContext()
	{
		// Ensure false by default
		var falseResult = behavior.TryRemoveBindingContext();
		Assert.False(falseResult);
		
		view.Behaviors.Add((Behavior)behavior);

		Assert.False(behavior.TrySetBindingContext());
		
		var trueResult = behavior.TryRemoveBindingContext();
		
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

	class MockViewModel
	{
		
	}
}