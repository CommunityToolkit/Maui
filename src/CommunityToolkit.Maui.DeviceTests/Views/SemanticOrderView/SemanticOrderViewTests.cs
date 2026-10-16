using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.DeviceTests;

// SemanticOrderView (Android, iOS, Mac Catalyst, Windows): the handler creates the native MauiSemanticOrderView, and ViewOrder is
// applied to the native views: Android links the views with AccessibilityTraversalBefore/After (generating view ids), iOS and Mac
// Catalyst set the container's accessibilityElements, Windows sets the TabIndex of the views. The platform partials read that state
// (AssertPlatformOrder, AssertNoOrder, IsInNativeOrder, DescribeNativeOrder, ...).
//
// One contract for every platform: ViewOrder lists the views in the order assistive technologies visit them, and the views that are not
// in it stay accessible (iOS and Mac Catalyst list them after the views of the order: see the Apple partial). Entries that cannot be
// ordered natively (no platform view yet, or a platform view of another type) are left out and the other views keep their relative
// order; a child entry that gets its platform view later joins the order, wherever it is below the container (adding a view does not
// necessarily lay the container out again: see ViewOrderEntryThatGetsItsPlatformViewLaterJoinsTheNativeOrder); views dropped from
// ViewOrder lose their native order; with nothing to order the platform's default order is used (the ViewOrder documentation: "If not
// set, the default order is used").
[Category(TestCategory.SemanticOrderView)]
[Collection(TestCollections.DisableParallelization)]
public partial class SemanticOrderViewTests : CommunityToolkitHandlerTestBase
{
	public enum UnorderableEntry
	{
		WithoutAPlatformView,
		WithAPlatformViewOfAnotherType,
	}

	public enum EntryPosition
	{
		First,
		Middle,
		Last,
	}

	// What the layout that a child entry is added to is placed in, below the container.
	public enum ChildLayoutHost
	{
		TheContainer,
		AScrollView,
		AGridWithAFixedSize,
		ABorderWithAFixedSizeInAStack,
	}

	[Fact]
	public async Task HandlerCreatesTheNativeSemanticOrderView()
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out _);

		var handler = await CreateHandlerAsync<SemanticOrderViewHandler>(view);

		Assert.Same(handler, view.Handler);
		Assert.Same(view, handler.VirtualView);
		var platformView = Assert.IsType<MauiSemanticOrderView>(handler.PlatformView);
		Assert.Same(view, platformView.VirtualView);
	}

	[Fact]
	public async Task SemanticOrderViewResolvesItsToolkitHandler()
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out _);

		var handler = await CreateHandlerAsync(view);

		Assert.IsType<SemanticOrderViewHandler>(handler);
		Assert.IsType<MauiSemanticOrderView>(handler.PlatformView);
	}

	[Fact]
	public async Task ViewOrderIsAppliedToTheNativeViewsWhenTheHandlerIsCreated()
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out var buttons);
		view.ViewOrder = [buttons[2], buttons[0], buttons[1]];

		var handler = await CreateHandlerAsync<SemanticOrderViewHandler>(view);

		await InvokeOnMainThreadAsync(() => AssertPlatformOrder(handler, buttons[2], buttons[0], buttons[1]));
	}

	[Fact]
	public async Task ChangingViewOrderAppliesTheNewOrderToTheNativeViews()
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out var buttons);
		view.ViewOrder = [buttons[0], buttons[1]];
		var handler = await CreateHandlerAsync<SemanticOrderViewHandler>(view);
		await InvokeOnMainThreadAsync(() => AssertPlatformOrder(handler, buttons[0], buttons[1]));

		await InvokeOnMainThreadAsync(() => view.ViewOrder = [buttons[1], buttons[2], buttons[0]]);

		await InvokeOnMainThreadAsync(() => AssertOrderLinks(handler, buttons[1], buttons[2], buttons[0]));
	}

	// The native order must describe the current ViewOrder only: a view that was dropped from the order, and the ends of the new
	// order, must not keep links to the previous order (Android would otherwise be told that a view comes both before and after another).
	[Fact]
	public async Task ViewsDroppedFromTheViewOrderLoseTheirNativeOrder()
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out var buttons);
		view.ViewOrder = [buttons[0], buttons[1], buttons[2]];
		var handler = await CreateHandlerAsync<SemanticOrderViewHandler>(view);
		await InvokeOnMainThreadAsync(() => AssertPlatformOrder(handler, buttons[0], buttons[1], buttons[2]));

		await InvokeOnMainThreadAsync(() => view.ViewOrder = [buttons[2], buttons[0]]);

		await InvokeOnMainThreadAsync(() =>
		{
			AssertPlatformOrder(handler, buttons[2], buttons[0]);
			AssertNoOrder(handler, buttons[1]);
		});
	}

	[Fact]
	public async Task ViewOrderOfASingleViewIsApplied()
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out var buttons);
		view.ViewOrder = [buttons[1]];

		var handler = await CreateHandlerAsync<SemanticOrderViewHandler>(view);

		await InvokeOnMainThreadAsync(() => AssertPlatformOrder(handler, buttons[1]));
	}

	[Fact]
	public async Task EmptyViewOrderLeavesTheNativeViewsUnordered()
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out var buttons);
		Assert.Empty(view.ViewOrder);

		var handler = await CreateHandlerAsync<SemanticOrderViewHandler>(view);

		await InvokeOnMainThreadAsync(() =>
		{
			AssertPlatformOrder(handler);
			AssertNoOrder(handler, buttons);
		});
	}

	// Wherever the entry without a platform view is (on Android: the first or the second view of a pair), applying the order must not fail,
	// and once the entry has a platform view, setting the order again applies all of it. What the other views get in the meantime is asserted
	// by ViewOrderEntriesThatCannotBeOrderedAreLeftOutAndTheOtherViewsKeepTheirOrder.
	[Theory]
	[InlineData(EntryPosition.First)]
	[InlineData(EntryPosition.Middle)]
	[InlineData(EntryPosition.Last)]
	public async Task ViewOrderEntriesThatAreNotRealizedYetDoNotThrowAndAreAppliedWhenTheViewOrderIsSetAgain(EntryPosition position)
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out var buttons);
		var later = new Button { Text = "Realized later" };
		IView[] order = position switch
		{
			EntryPosition.First => [later, buttons[0], buttons[1]],
			EntryPosition.Middle => [buttons[0], later, buttons[1]],
			EntryPosition.Last => [buttons[0], buttons[1], later],
			_ => throw new ArgumentOutOfRangeException(nameof(position), position, null),
		};
		view.ViewOrder = order;

		// The entry has no handler yet: applying the order must not fail.
		var handler = await CreateHandlerAsync<SemanticOrderViewHandler>(view);
		Assert.Null(later.Handler);

		await InvokeOnMainThreadAsync(() =>
		{
			CreateHandler<ButtonHandler>(later);
			// Setting the same order again (a new list, so it is a property change) applies it with all views realized.
			view.ViewOrder = [.. order];

			AssertPlatformOrder(handler, order);
		});
	}

	// The same for an entry whose platform view is not a native view of this platform (on Android: the first or the second view of a pair
	// whose platform view is not an Android View): applying the order must not fail, and once the entry is replaced by a view that has a
	// native view, the whole order is applied. What the other views get while the entry is listed is asserted by
	// ViewOrderEntriesThatCannotBeOrderedAreLeftOutAndTheOtherViewsKeepTheirOrder.
	[Theory]
	[InlineData(EntryPosition.First)]
	[InlineData(EntryPosition.Middle)]
	[InlineData(EntryPosition.Last)]
	public async Task ViewOrderEntriesWithAPlatformViewOfAnotherTypeDoNotThrowAndTheOrderIsAppliedOnceTheyAreReplaced(EntryPosition position)
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out var buttons);
		var foreign = new ForeignView { Handler = new ObjectPlatformViewHandler(new object()) };
		view.ViewOrder = WithEntryAt(position, foreign, buttons[0], buttons[1]);

		// The entry's platform view is a plain object: applying the order must not fail.
		var handler = await CreateHandlerAsync<SemanticOrderViewHandler>(view);

		await InvokeOnMainThreadAsync(() =>
		{
			var replaced = WithEntryAt(position, buttons[2], buttons[0], buttons[1]);
			view.ViewOrder = replaced;

			AssertPlatformOrder(handler, replaced);
		});
	}

	// A child entry that is not part of the visual tree yet has no platform view when the order is applied. Once it gets one (it is added to
	// a layout below the container), it must take its place in the native order without ViewOrder being set again. Adding a view does not
	// necessarily lay the container out again: iOS and Mac Catalyst stop MAUI's layout invalidation at a view whose size cannot change (a
	// Grid or a Border with a WidthRequest and a HeightRequest), and WinUI lays out only the part of the tree whose size changed. Android
	// applies the order again when the container is laid out (OnLayout: requestLayout always reaches the container), iOS and Mac Catalyst
	// build it when UIKit asks for the container's accessibility elements, Windows applies it again after every layout pass.
	[Theory]
	[InlineData(ChildLayoutHost.TheContainer)]
	[InlineData(ChildLayoutHost.AScrollView)]
	[InlineData(ChildLayoutHost.AGridWithAFixedSize)]
	[InlineData(ChildLayoutHost.ABorderWithAFixedSizeInAStack)]
	public async Task ViewOrderEntryThatGetsItsPlatformViewLaterJoinsTheNativeOrder(ChildLayoutHost host)
	{
		SetupBuilder();
		var layout = new VerticalStackLayout();
		var buttons = new Button[2];
		for (var i = 0; i < buttons.Length; i++)
		{
			buttons[i] = new Button { Text = $"Button {i}" };
			layout.Add(buttons[i]);
		}

		var view = new SemanticOrderView
		{
			Content = host switch
			{
				ChildLayoutHost.TheContainer => layout,
				ChildLayoutHost.AScrollView => new ScrollView { Content = layout },
				ChildLayoutHost.AGridWithAFixedSize => new Grid { WidthRequest = 300, HeightRequest = 400, Children = { layout } },
				ChildLayoutHost.ABorderWithAFixedSizeInAStack => new VerticalStackLayout { new Label { Text = "Header" }, new Border { WidthRequest = 300, HeightRequest = 400, Content = layout } },
				_ => throw new ArgumentOutOfRangeException(nameof(host), host, null),
			},
		};
		var later = new Button { Text = "Added later" };
		view.ViewOrder = [buttons[0], later, buttons[1]];

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			var semanticOrderViewHandler = Assert.IsType<SemanticOrderViewHandler>(handler);
			Assert.Null(later.Handler);
			Assert.True(IsInNativeOrder(semanticOrderViewHandler, buttons[0], buttons[1]), $"The views that have a platform view are not in the native order: {DescribeNativeOrder(semanticOrderViewHandler, buttons[0], buttons[1])}.");

			// Adding the view creates its platform view.
			layout.Add(later);

			var joined = await AssertHelpers.Wait(
				() => later.Handler?.PlatformView is not null && IsInNativeOrder(semanticOrderViewHandler, buttons[0], later, buttons[1]),
				timeout: 5000);
			Assert.True(joined, $"The view that got its platform view later is not in the native order (it was added to a layout in {host}): {DescribeNativeOrder(semanticOrderViewHandler, buttons[0], later, buttons[1])}.");
		});
	}

	// ViewOrder is documented as "the collection of child views", but a view outside the container is ordered too when it has its platform
	// view as the order is applied (Android applies the order again only when the container is laid out, which adding a view elsewhere does
	// not necessarily cause, so an outside view that gets its platform view later is not guaranteed to join).
	[Fact]
	public async Task ViewOrderMayContainViewsThatAreNotChildrenOfTheSemanticOrderView()
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out var buttons);
		var outside = new Button { Text = "Outside" };
		view.ViewOrder = [outside, buttons[0]];

		var handler = await CreateHandlerAsync<SemanticOrderViewHandler>(view);
		await InvokeOnMainThreadAsync(() =>
		{
			CreateHandler<ButtonHandler>(outside);
			view.ViewOrder = [outside, buttons[0]];

			AssertPlatformOrder(handler, outside, buttons[0]);
		});
	}

	// An entry without a platform view (a view that is not part of the visual tree yet) and an entry whose platform view is not a native
	// view of this platform cannot be ordered natively, wherever they are in the list: they are left out and the other views keep their
	// relative order (every platform skips such an entry).
	[Theory]
	[InlineData(UnorderableEntry.WithoutAPlatformView, EntryPosition.First)]
	[InlineData(UnorderableEntry.WithoutAPlatformView, EntryPosition.Middle)]
	[InlineData(UnorderableEntry.WithoutAPlatformView, EntryPosition.Last)]
	[InlineData(UnorderableEntry.WithAPlatformViewOfAnotherType, EntryPosition.First)]
	[InlineData(UnorderableEntry.WithAPlatformViewOfAnotherType, EntryPosition.Middle)]
	[InlineData(UnorderableEntry.WithAPlatformViewOfAnotherType, EntryPosition.Last)]
	public async Task ViewOrderEntriesThatCannotBeOrderedAreLeftOutAndTheOtherViewsKeepTheirOrder(UnorderableEntry entryKind, EntryPosition position)
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out var buttons);
		IView entry = entryKind switch
		{
			UnorderableEntry.WithoutAPlatformView => new Button { Text = "Not realized" },
			// A view whose handler has a platform view of no native type (a real View would set up a gesture manager for a native view).
			UnorderableEntry.WithAPlatformViewOfAnotherType => new ForeignView { Handler = new ObjectPlatformViewHandler(new object()) },
			_ => throw new ArgumentOutOfRangeException(nameof(entryKind), entryKind, null),
		};
		view.ViewOrder = position switch
		{
			EntryPosition.First => [entry, buttons[0], buttons[1]],
			EntryPosition.Middle => [buttons[0], entry, buttons[1]],
			EntryPosition.Last => [buttons[0], buttons[1], entry],
			_ => throw new ArgumentOutOfRangeException(nameof(position), position, null),
		};

		var handler = await CreateHandlerAsync<SemanticOrderViewHandler>(view);

		await InvokeOnMainThreadAsync(() =>
		{
			// The entry still cannot be ordered natively.
			Assert.True(entryKind is UnorderableEntry.WithAPlatformViewOfAnotherType || entry.Handler is null, "The entry without a platform view got a handler.");

			Assert.True(
				IsInNativeOrder(handler, buttons[0], buttons[1]),
				$"The views around the entry that cannot be ordered are not in their relative order: {DescribeNativeOrder(handler, buttons)}.");
			AssertPlatformOrder(handler, buttons[0], buttons[1]);
			AssertNoOrder(handler, buttons[2]);
		});
	}

	// Without a virtual view there is nothing to order. Android and Windows keep the current native order, iOS and Mac Catalyst fall back
	// to UIKit's default (nil); whatever a platform does, it must not declare that the container has no accessible children (see the
	// Apple partial).
	[Fact]
	public async Task PlatformViewWithoutAVirtualViewIsHandledWhenUpdatingTheOrder()
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out var buttons);
		view.ViewOrder = [buttons[0], buttons[1]];
		var handler = await CreateHandlerAsync<SemanticOrderViewHandler>(view);

		await InvokeOnMainThreadAsync(() =>
		{
			var platformView = Assert.IsType<MauiSemanticOrderView>(handler.PlatformView);
			platformView.VirtualView = null;

			AssertPlatformOrderWithoutVirtualView(handler, buttons[0], buttons[1]);
		});
	}

	// The native view forgets the virtual view it was given: a later ViewOrder change of the old view still reaches the native view through
	// the handler's mapper, finds no virtual view, does not fail, and orders nothing (on every platform, whatever the native view keeps).
	[Fact]
	public async Task PlatformViewWithoutAVirtualViewIgnoresLaterViewOrderChanges()
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out var buttons);
		view.ViewOrder = [buttons[0], buttons[1]];
		var handler = await CreateHandlerAsync<SemanticOrderViewHandler>(view);

		await InvokeOnMainThreadAsync(() =>
		{
			var platformView = Assert.IsType<MauiSemanticOrderView>(handler.PlatformView);
			platformView.VirtualView = null;
			Assert.Null(platformView.VirtualView);
			Assert.Same(view, handler.VirtualView);

			view.ViewOrder = [buttons[1], buttons[2]];

			Assert.False(
				IsInNativeOrder(handler, buttons[1], buttons[2]),
				$"The order of a view that the native view no longer has was applied: {DescribeNativeOrder(handler, buttons)}.");
		});
	}

	[Fact]
	public async Task ReusingTheHandlerForAnotherViewAppliesTheViewOrderOfTheNewView()
	{
		SetupBuilder();
		var first = CreateSemanticOrderView(out var firstButtons);
		first.ViewOrder = [firstButtons[0], firstButtons[1]];
		var handler = await CreateHandlerAsync<SemanticOrderViewHandler>(first);
		var second = CreateSemanticOrderView(out var secondButtons);
		second.ViewOrder = [secondButtons[2], secondButtons[1], secondButtons[0]];

		await InvokeOnMainThreadAsync(() =>
		{
			handler.SetVirtualView(second);

			var platformView = Assert.IsType<MauiSemanticOrderView>(handler.PlatformView);
			Assert.Same(second, platformView.VirtualView);
			Assert.Same(second, handler.VirtualView);
			AssertPlatformOrder(handler, secondButtons[2], secondButtons[1], secondButtons[0]);
		});
	}

	[Fact]
	public async Task ViewOrderIsNotAppliedWhenThePlatformViewIsNotASemanticOrderView()
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out var buttons);
		view.ViewOrder = [buttons[1], buttons[0]];

		var handler = await CreateHandlerAsync<PlainPlatformViewHandler>(view);

		await InvokeOnMainThreadAsync(() =>
		{
			Assert.IsNotType<MauiSemanticOrderView>(handler.PlatformView);
			AssertNoOrder(handler, buttons);

			// Changing the order maps ViewOrder to a platform view that cannot apply it: nothing happens.
			view.ViewOrder = [buttons[0], buttons[1], buttons[2]];

			AssertNoOrder(handler, buttons);
		});
	}

	[Fact]
	public async Task HandlerWithoutMappersUsesTheDefaultPropertyMapper()
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out var buttons);
		var handler = new SemanticOrderViewHandler(null, null);

		await InvokeOnMainThreadAsync(() =>
		{
			handler.SetMauiContext(MauiContext);
			handler.SetVirtualView(view);
			AssertNoOrder(handler, buttons);

			// Setting the property reaches the handler's mapper: the default one maps ViewOrder to the native views.
			view.ViewOrder = [buttons[1], buttons[0], buttons[2]];

			AssertPlatformOrder(handler, buttons[1], buttons[0], buttons[2]);
		});
	}

	[Fact]
	public async Task HandlerWithoutMappersUsesTheDefaultCommandMapper()
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out _);
		var handler = new SemanticOrderViewHandler(null, null);
		var received = new List<object?>();
		var originalChain = SemanticOrderViewHandler.SemanticOrderViewCommandMapper.Chained;

		// The static command mapper has no command of its own: it is chained from ContentViewHandler's command mapper, so a handler that
		// uses it still gets MAUI's view commands (Focus, Unfocus, InvalidateMeasure, ...).
		Assert.Same(ContentViewHandler.CommandMapper, originalChain);
		Assert.NotNull(SemanticOrderViewHandler.SemanticOrderViewCommandMapper.GetCommand(nameof(IView.Focus)));
		Assert.NotNull(SemanticOrderViewHandler.SemanticOrderViewCommandMapper.GetCommand(nameof(IView.InvalidateMeasure)));

		try
		{
			// The handler uses the static command mapper itself, so a probe command added to it must reach the handler.
			SemanticOrderViewHandler.SemanticOrderViewCommandMapper.Chained = new CommandMapper<ISemanticOrderView, SemanticOrderViewHandler>(originalChain!)
			{
				["SemanticOrderViewTests.Probe"] = (_, _, args) => received.Add(args),
			};

			await InvokeOnMainThreadAsync(() =>
			{
				handler.SetMauiContext(MauiContext);
				handler.SetVirtualView(view);
				((IElementHandler)handler).Invoke("SemanticOrderViewTests.Probe", "payload");
			});

			Assert.Equal("payload", Assert.Single(received));
		}
		finally
		{
			SemanticOrderViewHandler.SemanticOrderViewCommandMapper.Chained = originalChain;
		}
	}

	[Fact]
	public async Task HandlerUsesThePropertyMapperPassedToTheConstructor()
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out var buttons);
		view.ViewOrder = [buttons[1], buttons[0]];
		var mappedViewOrderCount = 0;
		var propertyMapper = new PropertyMapper<ISemanticOrderView, SemanticOrderViewHandler>(SemanticOrderViewHandler.SemanticOrderViewMapper)
		{
			// Replaces the toolkit's ViewOrder mapping.
			[nameof(ISemanticOrderView.ViewOrder)] = (_, _) => mappedViewOrderCount++,
		};
		var handler = new SemanticOrderViewHandler(propertyMapper, null);

		await InvokeOnMainThreadAsync(() =>
		{
			handler.SetMauiContext(MauiContext);
			handler.SetVirtualView(view);
		});

		Assert.Equal(1, mappedViewOrderCount);

		await InvokeOnMainThreadAsync(() => view.ViewOrder = [buttons[0], buttons[1]]);

		Assert.Equal(2, mappedViewOrderCount);
	}

	[Fact]
	public async Task HandlerUsesTheCommandMapperPassedToTheConstructor()
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out _);
		var received = new List<object?>();
		var commandMapper = new CommandMapper<ISemanticOrderView, SemanticOrderViewHandler>(SemanticOrderViewHandler.SemanticOrderViewCommandMapper)
		{
			["SemanticOrderViewTests.Probe"] = (_, _, args) => received.Add(args),
		};
		var handler = new SemanticOrderViewHandler(null, commandMapper);

		await InvokeOnMainThreadAsync(() =>
		{
			handler.SetMauiContext(MauiContext);
			handler.SetVirtualView(view);
			((IElementHandler)handler).Invoke("SemanticOrderViewTests.Probe", "custom");
		});

		Assert.Equal("custom", Assert.Single(received));
	}

	[Fact]
	public async Task SetVirtualViewWithAViewThatIsNotASemanticOrderViewThrowsInvalidCastException()
	{
		SetupBuilder();
		var handler = new SemanticOrderViewHandler();
		handler.SetMauiContext(MauiContext);

		// A plain ContentView is a content view for the base handler, but the handler casts the virtual view to ISemanticOrderView
		// to hand it to its native view.
		await InvokeOnMainThreadAsync(() => Assert.Throws<InvalidCastException>(() => handler.SetVirtualView(new ContentView())));
	}

	[Fact]
	public async Task SemanticOrderViewDoesNotLeak()
	{
		SetupBuilder();
		WeakReference viewReference = null!, handlerReference = null!, platformViewReference = null!, childReference = null!, childPlatformViewReference = null!;

		await InvokeOnMainThreadAsync(() =>
		{
			var view = CreateSemanticOrderView(out var buttons);
			view.ViewOrder = [buttons[2], buttons[0], buttons[1]];
			var handler = CreateHandler<SemanticOrderViewHandler>(view);
			AssertPlatformOrder(handler, buttons[2], buttons[0], buttons[1]);

			viewReference = new WeakReference(view);
			handlerReference = new WeakReference(handler);
			platformViewReference = new WeakReference(handler.PlatformView);
			childReference = new WeakReference(buttons[0]);
			childPlatformViewReference = new WeakReference(buttons[0].Handler?.PlatformView);

			((IElementHandler)handler).DisconnectHandler();
			view.Handler = null;
		});

		await AssertionExtensions.WaitForGC(viewReference, handlerReference, platformViewReference, childReference, childPlatformViewReference);
	}

	// Shown in a window and removed again, the container does not stay alive: what it listens to while it is in the visual tree (Windows
	// applies the order again after every layout pass) does not keep it, its handler or the views of its order alive.
	[Fact]
	public async Task SemanticOrderViewThatWasShownInAWindowDoesNotLeak()
	{
		SetupBuilder();

		var references = await ShowInAWindowAndRemoveAsync();

		// On Android the views that were in the window are released only once the Java side was collected too, which takes the API 28
		// emulator longer than the 40 collections of AssertionExtensions.WaitForGC (they were all collected right after it gave up).
		await VisualBehaviorHelpers.WaitForGCAsync(references);
	}

	// Shows a container with an order in a window, removes it, waits until it was unloaded and disconnects its handler: returns weak
	// references to the container, its handler, its platform view, and a view of its order with its platform view.
	async Task<WeakReference[]> ShowInAWindowAndRemoveAsync()
	{
		var view = CreateSemanticOrderView(out var buttons);
		view.ViewOrder = [buttons[2], buttons[0], buttons[1]];
		WeakReference[] references = [];

		await AttachAndRun(view, async handler =>
		{
			await view.OnLoadedAsync();
			var semanticOrderViewHandler = Assert.IsType<SemanticOrderViewHandler>(handler);
			AssertPlatformOrder(semanticOrderViewHandler, buttons[2], buttons[0], buttons[1]);

			references = [new(view), new(handler), new(handler.PlatformView), new(buttons[0]), new(buttons[0].Handler?.PlatformView)];
		});

		await InvokeOnMainThreadAsync(async () =>
		{
			await view.OnUnloadedAsync();
			((IElementHandler?)view.Handler)?.DisconnectHandler();
			view.Handler = null;
		});

		return references;
	}

	sealed class ForeignView : VisualElement
	{
	}

	static SemanticOrderView CreateSemanticOrderView(out Button[] buttons)
	{
		var layout = new VerticalStackLayout();
		buttons = new Button[3];
		for (var i = 0; i < buttons.Length; i++)
		{
			buttons[i] = new Button { Text = $"Button {i}" };
			layout.Add(buttons[i]);
		}

		return new SemanticOrderView { Content = layout };
	}

	// The order of the two views with the entry first, between them, or last.
	static IView[] WithEntryAt(EntryPosition position, IView entry, IView first, IView second) => position switch
	{
		EntryPosition.First => [entry, first, second],
		EntryPosition.Middle => [first, entry, second],
		EntryPosition.Last => [first, second, entry],
		_ => throw new ArgumentOutOfRangeException(nameof(position), position, null),
	};

	// A readable name for failure messages.
	static string NameOf(IView view) => view switch
	{
		Button button => button.Text,
		Label label => label.Text,
		_ => view.GetType().Name,
	};
}