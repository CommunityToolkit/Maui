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
		Assert.IsType<Behavior>(behavior, exactMatch: false);
	}

	[Fact]
	public void VerifyBindingContextNotAutomaticallySetWhenViewAttached()
	{
		if (behavior is BasePlatformBehavior<TView, object>)
		{
			// OnAttached is only called in platform-specific code for Microsoft.Maui.Controls.PlatformBehavior<TView, TPlatformView>
			// I.e. OnAttached is called on net8.0-ios, but not on net8.0 for Microsoft.Maui.Controls.PlatformBehavior<TView, TPlatformView>
			return;
		}

		view.Behaviors.Add((Behavior)behavior);

		Assert.Single(view.Behaviors);
		Assert.Null(view.Behaviors[0].BindingContext);
	}

	[Fact]
	public void EnsureTryRemoveBindingContextUnchangedWhenViewAttached()
	{
		if (behavior is BasePlatformBehavior<TView, object>)
		{
			// OnAttached is only called in platform-specific code for Microsoft.Maui.Controls.PlatformBehavior<TView, TPlatformView>
			// Therefore, this test will always fail because ICommunityToolkitBehavior<TView>.View will always be null when Unit Testing on net8.0
			// I.e. OnAttached is called on net8.0-ios, but not on net8.0 for Microsoft.Maui.Controls.PlatformBehavior<TView, TPlatformView>
			return;
		}

		// Ensure empty by default
		Assert.Empty(view.Behaviors);

		var attachedBehavior = (Behavior)behavior;

		view.Behaviors.Add(attachedBehavior);
		view.Behaviors[0].BindingContext = view.BindingContext;

		Assert.Equal(view.BindingContext, attachedBehavior.BindingContext);

		var wasSuccessful = view.Behaviors.TryRemove(attachedBehavior);

		Assert.True(wasSuccessful);
		Assert.Equal(view.BindingContext, attachedBehavior.BindingContext);
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