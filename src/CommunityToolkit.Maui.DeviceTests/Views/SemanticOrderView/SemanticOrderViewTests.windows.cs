using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace CommunityToolkit.Maui.DeviceTests;

// The container lists the views of the order that it holds first in its tab focus children (GetChildrenInTabFocusOrder). A view of the order that a
// control within it holds, or that is outside it, gets a tab index instead: the helpers accept each view in the form the container gives it.
public partial class SemanticOrderViewTests
{
	// WinUI's default TabIndex.
	const int defaultTabIndex = int.MaxValue;

	public enum TabOrderLayout
	{
		ButtonsInTheLayoutOfTheContainer,
		ButtonsInABorderInTheContainer,
		ButtonsInAScrollViewInTheContainer,
	}

	public enum TabOrderHost
	{
		AStackLayout,
		AScrollView,
	}

	// WinUI compares a tab index with every Tab stop of the nearest control that holds the view. The container is a panel, so tab indexes on the
	// views that it holds put them before the Tab stops above the container too: the Sample's Button above it was reached last.
	[Theory]
	[InlineData(TabOrderLayout.ButtonsInTheLayoutOfTheContainer, TabOrderHost.AStackLayout)]
	[InlineData(TabOrderLayout.ButtonsInTheLayoutOfTheContainer, TabOrderHost.AScrollView)]
	[InlineData(TabOrderLayout.ButtonsInABorderInTheContainer, TabOrderHost.AStackLayout)]
	[InlineData(TabOrderLayout.ButtonsInABorderInTheContainer, TabOrderHost.AScrollView)]
	[InlineData(TabOrderLayout.ButtonsInAScrollViewInTheContainer, TabOrderHost.AStackLayout)]
	[InlineData(TabOrderLayout.ButtonsInAScrollViewInTheContainer, TabOrderHost.AScrollView)]
	public async Task TabReachesTheViewsOfTheOrderAfterTheViewAboveTheContainerAndBeforeTheViewBelowIt(TabOrderLayout layout, TabOrderHost host)
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(layout, out var buttons);
		view.ViewOrder = [buttons[2], buttons[0]];
		var above = new Button { Text = "Above" };
		var below = new Button { Text = "Below" };
		var page = new VerticalStackLayout { above, view, below };
		View root = host is TabOrderHost.AScrollView ? new ScrollView { Content = page } : page;
		IView[] namedViews = [above, below, .. buttons];
		string[] tabOrder = [];
		string[] shiftTabOrder = [];

		await AttachAndRun(root, async _ =>
		{
			await view.OnLoadedAsync();
			tabOrder = TabStopsFrom(above, FocusNavigationDirection.Next, 4, namedViews);
			shiftTabOrder = TabStopsFrom(below, FocusNavigationDirection.Previous, 4, namedViews);
		});

		Assert.Equal(["Button 2", "Button 0", "Button 1", "Below"], tabOrder);
		Assert.Equal(["Button 1", "Button 0", "Button 2", "Above"], shiftTabOrder);
	}

	[Fact]
	public async Task TabIndexOfTheNativeViewsFollowsTheViewOrderStartingAtOne()
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(TabOrderLayout.ButtonsInAScrollViewInTheContainer, out var buttons);
		view.ViewOrder = [buttons[2], buttons[0], buttons[1]];

		var handler = await CreateHandlerAsync<SemanticOrderViewHandler>(view);

		await InvokeOnMainThreadAsync(() =>
		{
			Assert.Equal(1, PlatformViewOf(buttons[2]).TabIndex);
			Assert.Equal(2, PlatformViewOf(buttons[0]).TabIndex);
			Assert.Equal(3, PlatformViewOf(buttons[1]).TabIndex);
			Assert.Null(TabFocusChildrenOf(handler));
			AssertPlatformOrder(handler, buttons[2], buttons[0], buttons[1]);
		});
	}

	[Fact]
	public async Task DroppedViewKeepsATabIndexThatTheAppSetSince()
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(TabOrderLayout.ButtonsInAScrollViewInTheContainer, out var buttons);
		view.ViewOrder = [buttons[0], buttons[1], buttons[2]];
		var handler = await CreateHandlerAsync<SemanticOrderViewHandler>(view);

		await InvokeOnMainThreadAsync(() =>
		{
			PlatformViewOf(buttons[1]).TabIndex = 42;

			view.ViewOrder = [buttons[2], buttons[0]];

			AssertPlatformOrder(handler, buttons[2], buttons[0]);
			Assert.Equal(42, PlatformViewOf(buttons[1]).TabIndex);
		});
	}

	static SemanticOrderView CreateSemanticOrderView(TabOrderLayout layout, out Button[] buttons)
	{
		buttons = [new Button { Text = "Button 0" }, new Button { Text = "Button 1" }, new Button { Text = "Button 2" }];
		View content = layout switch
		{
			TabOrderLayout.ButtonsInTheLayoutOfTheContainer => new VerticalStackLayout { buttons[0], buttons[1], buttons[2] },
			TabOrderLayout.ButtonsInABorderInTheContainer => new VerticalStackLayout { buttons[0], new Border { Content = new VerticalStackLayout { buttons[1], buttons[2] } } },
			TabOrderLayout.ButtonsInAScrollViewInTheContainer => new ScrollView { Content = new VerticalStackLayout { buttons[0], buttons[1], buttons[2] } },
			_ => throw new ArgumentOutOfRangeException(nameof(layout), layout, null),
		};

		return new SemanticOrderView { Content = content };
	}

	// FindNextElement answers what Tab (Next) or Shift+Tab (Previous) moves the focus to, so no key press has to be simulated
	static string[] TabStopsFrom(IView start, FocusNavigationDirection direction, int count, IView[] namedViews)
	{
		var startView = PlatformViewOf(start);
		var focused = startView.Focus(FocusState.Programmatic);
		Assert.True(focused, $"{NameOf(start)} could not be focused.");

		var options = new FindNextElementOptions { SearchRoot = startView.XamlRoot.Content };
		var tabStops = new List<string>();
		for (var i = 0; i < count; i++)
		{
			var next = FocusManager.FindNextElement(direction, options);
			tabStops.Add(NameOfElement(next, namedViews));
			if (next is not UIElement element || !element.Focus(FocusState.Keyboard))
			{
				break;
			}
		}

		return [.. tabStops];
	}

	static string NameOfElement(DependencyObject? element, IView[] namedViews) =>
		namedViews.FirstOrDefault(candidate => Equals(candidate.Handler?.PlatformView, element)) is IView named
			? NameOf(named)
			: element?.GetType().Name ?? "null";

	static FrameworkElement PlatformViewOf(IView view) =>
		view.Handler?.PlatformView as FrameworkElement ?? throw new InvalidOperationException($"{NameOf(view)} has no native view.");

	static List<DependencyObject>? TabFocusChildrenOf(SemanticOrderViewHandler handler) =>
		(handler.PlatformView as MauiSemanticOrderView)?.GetTabFocusChildrenInViewOrder();

	static bool IsInNativeOrder(SemanticOrderViewHandler handler, params IView[] order)
	{
		if (order.Length is 0)
		{
			return TabFocusChildrenOf(handler) is null;
		}

		if (!order.All(static view => view.Handler?.PlatformView is FrameworkElement))
		{
			return false;
		}

		var platformViews = order.Select(PlatformViewOf).ToList();
		var tabFocusChildren = TabFocusChildrenOf(handler) ?? [];
		var listedViews = platformViews.Where(tabFocusChildren.Contains).ToList();
		var viewsWithATabIndex = platformViews.Except(listedViews).ToList();

		return tabFocusChildren.Take(listedViews.Count).SequenceEqual(listedViews)
			&& viewsWithATabIndex.Select(static (view, index) => view.TabIndex == index + 1).All(static inPlace => inPlace);
	}

	static string DescribeNativeOrder(SemanticOrderViewHandler handler, params IView[] views)
	{
		var tabFocusChildren = TabFocusChildrenOf(handler) is List<DependencyObject> children
			? $"[{string.Join(", ", children.Select(child => NameOfElement(child, views)))}]"
			: "WinUI's default";
		var tabIndexes = views.Select(view => view.Handler?.PlatformView is FrameworkElement platformView
			? $"{NameOf(view)}: TabIndex={platformView.TabIndex}"
			: $"{NameOf(view)}: no native view");

		return $"tab focus children {tabFocusChildren}; {string.Join("; ", tabIndexes)}";
	}

	static void AssertPlatformOrder(SemanticOrderViewHandler handler, params IView[] order)
	{
		var inOrder = IsInNativeOrder(handler, order);
		Assert.True(inOrder, $"Expected [{string.Join(", ", order.Select(NameOf))}] first in the tab focus children or with the tab indexes from 1, actual {DescribeNativeOrder(handler, order)}.");
	}

	static void AssertOrderLinks(SemanticOrderViewHandler handler, params IView[] order) => AssertPlatformOrder(handler, order);

	// A view without an order can still be listed, after the views of the order with the rest of the container's content: it only must not be
	// among them.
	static void AssertNoOrder(SemanticOrderViewHandler handler, params IView[] views)
	{
		var tabFocusChildren = TabFocusChildrenOf(handler) ?? [];
		var viewOrder = handler.VirtualView is ISemanticOrderView semanticOrderView ? semanticOrderView.ViewOrder : [];
		var platformViewsOfTheOrder = viewOrder.Select(static view => view.Handler?.PlatformView).OfType<DependencyObject>().ToList();
		var endOfTheOrder = tabFocusChildren.FindLastIndex(platformViewsOfTheOrder.Contains) + 1;

		foreach (var view in views)
		{
			var platformView = PlatformViewOf(view);
			var index = tabFocusChildren.IndexOf(platformView);
			var listedAmongTheOrder = index >= 0 && index < endOfTheOrder;
			Assert.False(listedAmongTheOrder, $"{NameOf(view)} is listed among the views of the order: {DescribeNativeOrder(handler, views)}.");

			var hasDefaultTabIndex = platformView.TabIndex is defaultTabIndex;
			Assert.True(hasDefaultTabIndex, $"{NameOf(view)} still has a tab index of the order: {DescribeNativeOrder(handler, views)}.");
		}
	}

	// The container builds its tab focus children from the virtual view whenever WinUI asks for them.
	static void AssertPlatformOrderWithoutVirtualView(SemanticOrderViewHandler handler, params IView[] previousOrder)
	{
		var isDefault = TabFocusChildrenOf(handler) is null;
		Assert.True(isDefault, $"Expected WinUI's default order without a virtual view, actual {DescribeNativeOrder(handler, previousOrder)}.");
	}

	sealed class PlainPlatformViewHandler : SemanticOrderViewHandler
	{
		protected override ContentPanel CreatePlatformView() => new();
	}
}