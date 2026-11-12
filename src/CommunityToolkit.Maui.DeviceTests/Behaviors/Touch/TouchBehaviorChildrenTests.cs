using CommunityToolkit.Maui.Behaviors;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.TouchBehavior)]
[Collection(TestCollections.DisableParallelization)]
public class TouchBehaviorChildrenTests : TouchBehaviorTestBase
{
	[Fact]
	public async Task ChildrenBecomeInputTransparentAndTheLayoutDoesNot()
	{
		SetupBuilder();

		var first = new Label { Text = "one" };
		var second = new Label { Text = "two" };
		var host = CreateHost();
		host.Add(first);
		host.Add(second);
		host.InputTransparent = true;

		await RunAttachedAsync(host, new TouchBehavior(), async _ =>
		{
			Assert.False(host.InputTransparent, "The layout must take the touches itself.");
			Assert.True(first.InputTransparent);
			Assert.True(second.InputTransparent);

			await WaitUntilAsync(() => IsNativeInputTransparent(first) && IsNativeInputTransparent(second), "The platform views of the children still take input.");
		});
	}

	[Fact]
	public async Task ChildAddedWhileAttachedBecomesInputTransparent()
	{
		SetupBuilder();

		var host = CreateHost();
		host.Add(new Label { Text = "one" });
		var late = new Label { Text = "late" };

		await RunAttachedAsync(host, new TouchBehavior(), async _ =>
		{
			host.Add(late);

			Assert.True(late.InputTransparent);
			await WaitUntilAsync(() => late.Handler is not null && IsNativeInputTransparent(late), "The platform view of the new child still takes input.");
		});
	}

	[Fact]
	public async Task ChildrenStayInteractiveWhenTheBehaviorMustNotMakeThemTransparent()
	{
		SetupBuilder();

		var first = new Label { Text = "one" };
		var late = new Label { Text = "late" };
		var host = CreateHost();
		host.Add(first);

		await RunAttachedAsync(host, new TouchBehavior { ShouldMakeChildrenInputTransparent = false }, async _ =>
		{
			host.Add(late);
			await SettleAsync();

			Assert.False(first.InputTransparent);
			Assert.False(late.InputTransparent);
			Assert.False(IsNativeInputTransparent(first));
			Assert.False(IsNativeInputTransparent(late));
		});
	}

	[Fact]
	public async Task ChildAddedAfterTurningTheOptionOffStaysInteractive()
	{
		SetupBuilder();

		var behavior = new TouchBehavior();
		var host = CreateHost();
		host.Add(new Label { Text = "one" });

		await RunAttachedAsync(host, behavior, async _ =>
		{
			behavior.ShouldMakeChildrenInputTransparent = false;

			var late = new Label { Text = "late" };
			host.Add(late);

			Assert.False(late.InputTransparent, "A child added after the option was turned off must take input.");
			await WaitUntilAsync(() => late.Handler is not null, "The new child was not realized.");
			await AssertionExtensions.AssertEventually(() => !IsNativeInputTransparent(late), timeout: 1000, message: "The platform view of the new child ignores input.");
		});
	}

	// MAUI layouts hold IView children, which are Views in practice, so the event is raised with a non-View element directly
	// (Element.OnChildAdded raises ChildAdded for any element).
	[Fact]
	public async Task ChildAddedThatIsNotAViewIsIgnored()
	{
		SetupBuilder();

		var first = new Label { Text = "one" };
		var host = new ChildRaisingGrid { WidthRequest = HostSize, HeightRequest = HostSize };
		host.Add(first);

		await RunAttachedAsync(host, new TouchBehavior(), async _ =>
		{
			var exception = Record.Exception(() => host.RaiseChildAdded(new TapGestureRecognizer()));

			Assert.Null(exception);

			var late = new Label { Text = "late" };
			host.Add(late);
			Assert.True(first.InputTransparent);
			Assert.True(late.InputTransparent);
			await WaitUntilAsync(() => late.Handler is not null && IsNativeInputTransparent(late), "The platform view of the new child still takes input.");
		});
	}

	[Fact]
	public async Task DisabledBehaviorDoesNotMakeChildrenInputTransparent()
	{
		SetupBuilder();

		var first = new Label { Text = "one" };
		var host = CreateHost();
		host.Add(first);

		await RunAttachedAsync(host, new TouchBehavior { IsEnabled = false }, async _ =>
		{
			await SettleAsync();

			Assert.False(first.InputTransparent);
			Assert.False(IsNativeInputTransparent(first));

			var late = new Label { Text = "late" };
			host.Add(late);
			Assert.False(late.InputTransparent);
		});
	}

	[Fact]
	public async Task ChildrenOfAContentViewsLayoutBecomeInputTransparent()
	{
		SetupBuilder();

		var child = new Label { Text = "inside" };
		var content = new Grid { Children = { child } };
		var host = new ContentView { Content = content, WidthRequest = HostSize, HeightRequest = HostSize };

		await RunAttachedAsync(host, new TouchBehavior(), async _ =>
		{
			Assert.True(child.InputTransparent);
			Assert.False(content.InputTransparent);
			await WaitUntilAsync(() => IsNativeInputTransparent(child), "The platform view of the content's child still takes input.");
		});
	}

	[Fact]
	public async Task ContentViewWhoseContentIsNotALayoutKeepsItsContentInteractive()
	{
		SetupBuilder();

		var label = new Label { Text = "inside" };
		var host = new ContentView { Content = label, WidthRequest = HostSize, HeightRequest = HostSize };
		var command = new CountingCommand();
		var behavior = new TouchBehavior { Command = command };

		await RunAttachedAsync(host, behavior, async _ =>
		{
			Assert.Same(host, behavior.Element);

			SimulatePress(behavior);
			SimulateRelease(behavior);
			Assert.Equal(1, command.ExecuteCount);

			await SettleAsync();
			Assert.False(label.InputTransparent, "The content of the content view must keep taking input.");
			Assert.False(IsNativeInputTransparent(label), "The platform view of the content must keep taking input.");
		});

		var emptyBehavior = new TouchBehavior();
		var empty = new ContentView { WidthRequest = HostSize, HeightRequest = HostSize };

		await RunAttachedAsync(empty, emptyBehavior, _ => Assert.Same(empty, emptyBehavior.Element));
	}

	[Fact]
	public async Task ElementWithoutChildrenAttachesWithoutTouchingAnything()
	{
		SetupBuilder();

		var label = new Label { Text = "alone", WidthRequest = HostSize, HeightRequest = HostSize };
		var behavior = new TouchBehavior();

		await RunAttachedAsync(label, behavior, _ =>
		{
			Assert.Same(label, behavior.Element);
			Assert.False(label.InputTransparent);
		});
	}

	[Fact]
	public async Task RemovedBehaviorGivesTheChildrenTheirInputBack()
	{
		SetupBuilder();

		var first = new Label { Text = "one" };
		var host = CreateHost();
		host.Add(first);
		var behavior = new TouchBehavior();

		await RunAttachedAsync(host, behavior, async _ =>
		{
			Assert.True(first.InputTransparent);

			host.Behaviors.Remove(behavior);

			Assert.False(first.InputTransparent, "A removed behavior must give the children their input back.");
			await WaitUntilAsync(() => !IsNativeInputTransparent(first), "The platform view of the child still ignores input.");

			var late = new Label { Text = "late" };
			host.Add(late);
			Assert.False(late.InputTransparent, "A removed behavior must not touch children that are added later.");
		});
	}

	[Fact]
	public async Task RemovedBehaviorKeepsAChildThatWasInputTransparentBefore()
	{
		SetupBuilder();

		var interactive = new Label { Text = "interactive" };
		var transparent = new Label { Text = "transparent", InputTransparent = true };
		var host = CreateHost();
		host.Add(interactive);
		host.Add(transparent);
		var behavior = new TouchBehavior();

		await RunAttachedAsync(host, behavior, async _ =>
		{
			Assert.True(interactive.InputTransparent);
			Assert.True(transparent.InputTransparent);

			host.Behaviors.Remove(behavior);

			Assert.False(interactive.InputTransparent, "A removed behavior must give the child its input back.");
			Assert.True(transparent.InputTransparent, "A child that was input transparent before the behavior must stay input transparent.");
			await WaitUntilAsync(() => !IsNativeInputTransparent(interactive) && IsNativeInputTransparent(transparent), "The platform views of the children do not follow their InputTransparent.");
		});
	}

	[Fact]
	public async Task ChildRemovedWhileAttachedGetsItsOwnInputTransparentBack()
	{
		SetupBuilder();

		var interactive = new Label { Text = "interactive" };
		var transparent = new Label { Text = "transparent", InputTransparent = true };
		var host = CreateHost();
		host.Add(interactive);
		host.Add(transparent);
		var behavior = new TouchBehavior();

		await RunAttachedAsync(host, behavior, _ =>
		{
			Assert.True(interactive.InputTransparent);
			Assert.True(transparent.InputTransparent);

			host.Remove(interactive);
			host.Remove(transparent);

			Assert.False(interactive.InputTransparent, "A child removed from the layout must take input again.");
			Assert.True(transparent.InputTransparent, "A child that was input transparent before the behavior must stay input transparent.");

			interactive.InputTransparent = true;
			host.Behaviors.Remove(behavior);
			Assert.True(interactive.InputTransparent, "The detach must not touch a child that was removed from the layout.");
		});
	}

	[Fact]
	public async Task TurningTheOptionOffGivesTheExistingChildrenTheirInputBack()
	{
		SetupBuilder();

		var first = new Label { Text = "one" };
		var host = CreateHost();
		host.Add(first);
		var behavior = new TouchBehavior();

		await RunAttachedAsync(host, behavior, async _ =>
		{
			Assert.True(first.InputTransparent);

			behavior.ShouldMakeChildrenInputTransparent = false;

			Assert.False(first.InputTransparent, "The existing child must take input once the option is off.");
			await WaitUntilAsync(() => !IsNativeInputTransparent(first), "The platform view of the existing child still ignores input.");
		});
	}

	[Fact]
	public async Task DisablingTheBehaviorGivesTheExistingChildrenTheirInputBack()
	{
		SetupBuilder();

		var first = new Label { Text = "one" };
		var host = CreateHost();
		host.Add(first);
		var behavior = new TouchBehavior();

		await RunAttachedAsync(host, behavior, async _ =>
		{
			Assert.True(first.InputTransparent);

			behavior.IsEnabled = false;

			var late = new Label { Text = "late" };
			host.Add(late);
			Assert.False(late.InputTransparent);
			Assert.False(first.InputTransparent, "The existing child must take input once the behavior is disabled, like a child added now.");
			await WaitUntilAsync(() => !IsNativeInputTransparent(first), "The platform view of the existing child still ignores input.");
		});
	}

	[Fact]
	public async Task SwitchingTheOptionAndTheBehaviorBackOnMakesTheChildrenInputTransparentAgain()
	{
		SetupBuilder();

		var interactive = new Label { Text = "interactive" };
		var transparent = new Label { Text = "transparent", InputTransparent = true };
		var host = CreateHost();
		host.Add(interactive);
		host.Add(transparent);
		var behavior = new TouchBehavior();

		await RunAttachedAsync(host, behavior, async _ =>
		{
			behavior.ShouldMakeChildrenInputTransparent = false;
			Assert.False(interactive.InputTransparent);
			behavior.ShouldMakeChildrenInputTransparent = true;
			Assert.True(interactive.InputTransparent, "Turning the option back on must make the existing child input transparent again.");

			behavior.IsEnabled = false;
			Assert.False(interactive.InputTransparent);
			Assert.False(transparent.InputTransparent);
			behavior.IsEnabled = true;
			Assert.True(interactive.InputTransparent, "Enabling the behavior again must make the existing child input transparent again.");
			Assert.True(transparent.InputTransparent);
			await WaitUntilAsync(() => IsNativeInputTransparent(interactive), "The platform view of the child still takes input.");

			// Disabling the behavior made every child take input; the detach must still give each child its value from before the behavior.
			host.Behaviors.Remove(behavior);
			Assert.False(interactive.InputTransparent, "The detach must give the child the value it had before the behavior.");
			Assert.True(transparent.InputTransparent, "The detach must give the child the value it had before the behavior.");
		});
	}

	// The app may block input to the layout while the behavior is attached, e.g. with the layout's InputTransparent and the behavior's IsEnabled
	// bound to the same busy flag.
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task ChangingIsEnabledKeepsTheInputTransparencyTheAppGaveTheLayout(bool layoutChangesFirst)
	{
		SetupBuilder();

		var child = new Label { Text = "child" };
		var host = CreateHost();
		host.Add(child);
		var behavior = new TouchBehavior();

		await RunAttachedAsync(host, behavior, async _ =>
		{
			Assert.False(host.InputTransparent, "Precondition: the attach makes the layout take input.");

			if (layoutChangesFirst)
			{
				host.InputTransparent = true;
				behavior.IsEnabled = false;
			}
			else
			{
				behavior.IsEnabled = false;
				host.InputTransparent = true;
			}

			Assert.True(host.InputTransparent, "Disabling the behavior must leave the layout input transparent, as the app set it.");
			Assert.True(child.InputTransparent, "The child of an input transparent layout must not take input.");
			await WaitUntilAsync(() => IsNativeInputTransparent(child), "The platform view of the child takes input.");

			behavior.IsEnabled = true;
			Assert.True(host.InputTransparent, "Enabling the behavior again must leave the layout input transparent, as the app set it.");
		});
	}

	[Fact]
	public async Task RemovedBehaviorDoesNotGiveTheViewsTheInputTransparencyOfAParent()
	{
		SetupBuilder();

		var child = new Label { Text = "child" };
		var host = CreateHost();
		host.Add(child);
		// An input transparent parent passes that on to its children (CascadeInputTransparent), so the layout and its children report
		// InputTransparent = true although that is not their own value.
		var root = new Grid { InputTransparent = true, Children = { host } };
		var behavior = new TouchBehavior();

		await RunLoadedAsync(root, host, async _ =>
		{
			Assert.True(host.InputTransparent && child.InputTransparent, "Precondition: the parent passes its input transparency on.");

			host.Behaviors.Add(behavior);
			await WaitUntilAsync(() => behavior.Element is not null, "The behavior did not attach.");
			root.InputTransparent = false;
			Assert.False(host.InputTransparent, "The layout takes input while the behavior is attached.");
			Assert.True(child.InputTransparent, "The child is input transparent while the behavior is attached.");

			host.Behaviors.Remove(behavior);

			Assert.False(host.InputTransparent, "The layout was not input transparent itself: it must take input once the behavior is removed.");
			Assert.False(child.InputTransparent, "The child was not input transparent itself: it must take input once the behavior is removed.");
			await WaitUntilAsync(() => !IsNativeInputTransparent(child), "The platform view of the child still ignores input.");
		});
	}

	[Fact]
	public async Task TwoBehaviorsOnALayoutGiveTheViewsBackWhenTheLastOneIsRemoved()
	{
		SetupBuilder();

		var interactive = new Label { Text = "interactive" };
		var transparent = new Label { Text = "transparent", InputTransparent = true };
		var host = CreateHost();
		host.Add(interactive);
		host.Add(transparent);
		// Without the cascade, the children report their own InputTransparent values, not the layout's.
		host.CascadeInputTransparent = false;
		host.InputTransparent = true;
		var first = new TouchBehavior();
		var second = new TouchBehavior();

		await RunLoadedAsync(host, async _ =>
		{
			Assert.False(interactive.InputTransparent, "Precondition: the layout does not pass its input transparency on.");

			host.Behaviors.Add(first);
			host.Behaviors.Add(second);
			await WaitUntilAsync(() => first.Element is not null && second.Element is not null, "The behaviors did not attach.");
			Assert.False(host.InputTransparent);
			Assert.True(interactive.InputTransparent);

			host.Behaviors.Remove(first);

			Assert.False(host.InputTransparent, "The layout must keep taking input for the behavior that is still attached.");
			Assert.True(interactive.InputTransparent, "The child must stay input transparent for the behavior that is still attached.");

			host.Behaviors.Remove(second);

			Assert.True(host.InputTransparent, "The layout must be input transparent again, as it was before the behaviors.");
			Assert.False(interactive.InputTransparent, "The child must take input again, as it did before the behaviors.");
			Assert.True(transparent.InputTransparent, "The child that was input transparent before the behaviors must stay so.");
			await WaitUntilAsync(() => !IsNativeInputTransparent(interactive), "The platform view of the child still ignores input.");
		});
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task RemovedBehaviorLetsGoOfTheLayoutItHandledAfterTheContentWasReplaced(bool newContentIsALayout)
	{
		SetupBuilder();

		var oldChild = new Label { Text = "old" };
		var oldLayout = new Grid { Children = { oldChild } };
		var host = new ContentView { Content = oldLayout, WidthRequest = HostSize, HeightRequest = HostSize };
		var behavior = new TouchBehavior();

		await RunAttachedAsync(host, behavior, _ =>
		{
			Assert.True(oldChild.InputTransparent, "Precondition: the child of the content's layout is input transparent.");

			host.Content = newContentIsALayout ? new Grid { Children = { new Label { Text = "new" } } } : new Label { Text = "new" };
			host.Behaviors.Remove(behavior);

			Assert.False(oldChild.InputTransparent, "The child of the layout the behavior handled must take input again.");

			var late = new Label { Text = "late" };
			oldLayout.Add(late);
			Assert.False(late.InputTransparent, "A removed behavior must not touch a child added later to the layout it handled.");
		});
	}

	sealed class ChildRaisingGrid : Grid
	{
		public void RaiseChildAdded(Element child) => OnChildAdded(child);
	}
}