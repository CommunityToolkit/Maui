using CommunityToolkit.Maui.Behaviors;
using Nito.AsyncEx;
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

		var attachedBehavior = Assert.Single(view.Behaviors);
		Assert.Null(attachedBehavior.BindingContext);
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

		var wasSuccessful = view.Behaviors.Remove(attachedBehavior);

		Assert.True(wasSuccessful);
		Assert.Equal(view.BindingContext, attachedBehavior.BindingContext);
	}

	protected override void Dispose(bool isDisposing)
	{
		base.Dispose(isDisposing);

		view.Behaviors.Clear();
	}

	// A ValidationBehavior whose Value changes while it attaches, as when the view already has text, handles the change once OnAttachedTo finishes:
	// on the UI thread in an app, but on a thread-pool thread in a unit test, which has no SynchronizationContext. There it races the test on a
	// BindableObject, which is not thread-safe.
	protected static void RunOnUIThread(Action action) => AsyncContext.Run(action);

	protected static void RunOnUIThread(Func<Task> action) => AsyncContext.Run(action);

	protected sealed class MockValidationBehavior : ValidationBehavior<string>
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

	sealed class MockViewModel;
}