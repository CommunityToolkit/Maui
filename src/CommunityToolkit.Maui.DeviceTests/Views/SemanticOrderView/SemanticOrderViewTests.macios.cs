using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using Foundation;
using UIKit;
using PlatformContentView = Microsoft.Maui.Platform.ContentView;

namespace CommunityToolkit.Maui.DeviceTests;

// iOS and Mac Catalyst: the views of ViewOrder are the first accessibilityElements of the native container, in order, and the rest of its
// content follows them (the container builds the list when UIKit asks for it). UIKit's default for a container is a nil
// accessibilityElements (the container then exposes its subviews); a non-nil array is the complete list of its accessibility elements, so
// the content that is not in the order must be listed too, and a non-nil EMPTY array declares that the container has no accessible
// children at all. UIKit looks into a listed view that is not an accessibility element itself and exposes what it holds: such a view must
// not hold a view of the order, which would be exposed a second time (see Exposes).
public partial class SemanticOrderViewTests
{
	[Fact]
	public async Task NativeViewIsAnAccessibilityContainerThatListsTheViewsInOrder()
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out var buttons);
		view.ViewOrder = [buttons[1], buttons[2]];

		var handler = await CreateHandlerAsync<SemanticOrderViewHandler>(view);

		await InvokeOnMainThreadAsync(() =>
		{
			var platformView = Assert.IsType<MauiSemanticOrderView>(handler.PlatformView);
			Assert.IsType<IUIAccessibilityContainer>(platformView, exactMatch: false);
			var elements = ElementsOf(handler);

			// The views of the order, then the button that is not in it: the view that holds its platform view in the layout, which UIKit
			// looks into (MAUI puts the UIButton of a Button in a WrapperView).
			Assert.Equal(3, elements.Length);
			Assert.IsType<UIView>(elements[0], exactMatch: false);
			Assert.Equal(PlatformViewOf(buttons[1]).Handle, elements[0].Handle);
			Assert.Equal(PlatformViewOf(buttons[2]).Handle, elements[1].Handle);
			Assert.True(Exposes(elements[2], PlatformViewOf(buttons[0])), $"The last element does not expose {NameOf(buttons[0])}: {DescribeNativeOrder(handler, buttons)}.");
		});
	}

	// The default (empty) ViewOrder means "use the default order" (the ViewOrder documentation), which for UIKit is a nil
	// accessibilityElements. An empty array instead hides every child of the container from VoiceOver.
	[Fact]
	public async Task EmptyViewOrderLeavesTheDefaultAccessibilityElements()
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out var buttons);
		Assert.Empty(view.ViewOrder);

		var handler = await CreateHandlerAsync<SemanticOrderViewHandler>(view);

		await InvokeOnMainThreadAsync(() =>
		{
			var array = AccessibilityElementsArray(handler);
			var isDefault = array is null;
			Assert.True(isDefault, $"accessibilityElements is an array of {array?.Count} elements instead of nil (UIKit's default): {DescribeNativeOrder(handler, buttons)}.");
		});
	}

	[Fact]
	public async Task ClearingTheViewOrderRemovesTheViewsFromTheNativeAccessibilityElements()
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out var buttons);
		view.ViewOrder = [buttons[0], buttons[1]];
		var handler = await CreateHandlerAsync<SemanticOrderViewHandler>(view);
		await InvokeOnMainThreadAsync(() => AssertPlatformOrder(handler, buttons[0], buttons[1]));

		await InvokeOnMainThreadAsync(() => view.ViewOrder = []);

		// No view of the previous order is listed any more, and the container falls back to UIKit's default (nil), as with the default
		// empty ViewOrder (EmptyViewOrderLeavesTheDefaultAccessibilityElements): a non-nil empty array would hide every child.
		await InvokeOnMainThreadAsync(() =>
		{
			AssertPlatformOrder(handler);
			AssertNoOrder(handler, buttons);
			var array = AccessibilityElementsArray(handler);
			var isDefault = array is null;
			Assert.True(isDefault, $"accessibilityElements is an array of {array?.Count} elements instead of nil (UIKit's default) after the ViewOrder was cleared: {DescribeNativeOrder(handler, buttons)}.");
		});
	}

	// The container answers UIKit's accessibilityElements request with the platform views of ViewOrder at that moment: a child entry that
	// gets its platform view is listed at once, before any layout pass and without ViewOrder being set again (MAUI does not necessarily lay
	// the container out again when a view is added below it), and laying the container out does not change the order.
	[Fact]
	public async Task AChildEntryIsListedAsSoonAsItHasItsPlatformView()
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out var buttons);
		var layout = (VerticalStackLayout)view.Content!;
		var later = new Button { Text = "Added later" };
		view.ViewOrder = [buttons[1], later, buttons[0]];
		var handler = await CreateHandlerAsync<SemanticOrderViewHandler>(view);

		await InvokeOnMainThreadAsync(() =>
		{
			var platformView = Assert.IsType<MauiSemanticOrderView>(handler.PlatformView);
			AssertPlatformOrder(handler, buttons[1], buttons[0]);

			// Adding the view to the layout creates its platform view; the container is not laid out in between.
			layout.Add(later);
			Assert.NotNull(later.Handler?.PlatformView);

			AssertPlatformOrder(handler, buttons[1], later, buttons[0]);

			platformView.SetNeedsLayout();
			platformView.LayoutIfNeeded();

			AssertPlatformOrder(handler, buttons[1], later, buttons[0]);
		});
	}

	// The layout of the Sample's SemanticOrderView page: a heading that is not in ViewOrder, in the same layout as the views of the order.
	// It must stay reachable: it is listed after the views of the order, which VoiceOver visits first.
	[Fact]
	public async Task ViewsThatAreNotInTheViewOrderAreListedAfterTheViewsOfTheOrder()
	{
		SetupBuilder();
		var heading = new Label { Text = "Heading" };
		var second = new Label { Text = "Second" };
		var third = new Button { Text = "Third" };
		var fourth = new Label { Text = "Fourth" };
		var fifth = new Button { Text = "Fifth" };
		var first = new Button { Text = "First" };
		var view = new SemanticOrderView
		{
			Content = new Border { Content = new VerticalStackLayout { heading, second, third, fourth, fifth, first } },
			ViewOrder = [first, second, third, fourth, fifth],
		};

		var handler = await CreateHandlerAsync<SemanticOrderViewHandler>(view);

		await InvokeOnMainThreadAsync(() =>
		{
			IView[] views = [first, second, third, fourth, fifth, heading];
			var elements = ElementsOf(handler);

			AssertPlatformOrder(handler, first, second, third, fourth, fifth);
			var headingIsLast = elements.Length is 6 && Exposes(elements[5], PlatformViewOf(heading));
			Assert.True(headingIsLast, $"Expected accessibilityElements = [{string.Join(", ", views.Select(NameOf))}], actual {DescribeNativeOrder(handler, views)}.");
		});
	}

	// Each visible view of the container is exposed once: the views of the order as themselves, first, and the rest of the content after
	// them, in the order of the view hierarchy. A view that holds views of the order is not listed (UIKit would look into it and expose them
	// a second time): what else it holds is listed instead. A view that holds none of them is listed as it is (UIKit exposes what it holds).
	// Hidden views, and views whose accessibility elements are hidden, are not exposed, as without an order.
	[Fact]
	public async Task EveryVisibleViewOfTheContainerIsExposedOnce()
	{
		SetupBuilder();
		var before = new Button { Text = "Before" };
		var ordered1 = new Button { Text = "Ordered 1" };
		var besideOrdered1 = new Label { Text = "Beside ordered 1" };
		var grouped1 = new Label { Text = "Grouped 1" };
		var grouped2 = new Label { Text = "Grouped 2" };
		var hidden = new Label { Text = "Hidden", IsVisible = false };
		var excluded = new Label { Text = "Excluded" };
		AutomationProperties.SetExcludedWithChildren(excluded, true);
		var ordered2 = new Button { Text = "Ordered 2" };
		var view = new SemanticOrderView
		{
			Content = new VerticalStackLayout
			{
				before,
				new Border { Content = new VerticalStackLayout { ordered1, besideOrdered1 } },
				new VerticalStackLayout { grouped1, grouped2 },
				hidden,
				excluded,
				ordered2,
			},
			ViewOrder = [ordered2, ordered1],
		};

		var handler = await CreateHandlerAsync<SemanticOrderViewHandler>(view);

		await InvokeOnMainThreadAsync(() =>
		{
			Assert.True(((UIView)PlatformViewOf(hidden)).Hidden, "The platform view of the invisible label is not hidden.");
			Assert.True(((UIView)PlatformViewOf(excluded)).AccessibilityElementsHidden, "AutomationProperties.ExcludedWithChildren did not hide the accessibility elements of the label.");
			IView[] views = [ordered2, ordered1, before, besideOrdered1, grouped1, grouped2, hidden, excluded];
			var elements = ElementsOf(handler);

			AssertPlatformOrder(handler, ordered2, ordered1);
			foreach (var visible in new IView[] { ordered2, ordered1, before, besideOrdered1, grouped1, grouped2 })
			{
				Assert.True(TimesExposed(elements, visible) is 1, $"{NameOf(visible)} is exposed {TimesExposed(elements, visible)} times: {DescribeNativeOrder(handler, views)}.");
			}

			foreach (var notExposed in new IView[] { hidden, excluded })
			{
				Assert.True(TimesExposed(elements, notExposed) is 0, $"{NameOf(notExposed)} is exposed {TimesExposed(elements, notExposed)} times: {DescribeNativeOrder(handler, views)}.");
			}

			// After the views of the order, in the order of the view hierarchy (the two grouped labels are in one listed layout).
			var positions = new IView[] { before, besideOrdered1, grouped1, grouped2 }.Select(visible => IndexOfExposure(elements, visible)).ToArray();
			var inOrder = positions[0] > 1 && positions[1] > positions[0] && positions[2] > positions[1] && positions[3] >= positions[2];
			Assert.True(inOrder, $"The rest of the content is not listed after the views of the order, in the order of the view hierarchy (positions {string.Join(", ", positions)}): {DescribeNativeOrder(handler, views)}.");
		});
	}

	// A view that holds a view of the order and is an accessibility element itself (here a layout with a semantic description) is listed as
	// it is: UIKit does not look into an accessibility element, so the view of the order is not exposed a second time, and the description
	// stays the element that VoiceOver reads for what the layout holds.
	[Fact]
	public async Task AnAccessibilityElementThatHoldsAViewOfTheOrderIsListedAsItIs()
	{
		SetupBuilder();
		var ordered = new Button { Text = "Ordered" };
		var besideOrdered = new Label { Text = "Beside ordered" };
		var described = new VerticalStackLayout { ordered, besideOrdered };
		SemanticProperties.SetDescription(described, "Described layout");
		var view = new SemanticOrderView
		{
			Content = new VerticalStackLayout { described },
			ViewOrder = [ordered],
		};

		var handler = await CreateHandlerAsync<SemanticOrderViewHandler>(view);

		await InvokeOnMainThreadAsync(() =>
		{
			Assert.True(((UIView)PlatformViewOf(described)).IsAccessibilityElement, "The layout with a semantic description is not an accessibility element.");
			IView[] expected = [ordered, described];

			AssertPlatformOrder(handler, ordered);
			var listed = ElementsOf(handler).Select(element => element.Handle).SequenceEqual(expected.Select(expectedView => PlatformViewOf(expectedView).Handle));
			Assert.True(listed, $"Expected accessibilityElements = [{string.Join(", ", expected.Select(NameOf))}], actual {DescribeNativeOrder(handler, [ordered, besideOrdered, described])}.");
		});
	}

	static NSObject PlatformViewOf(IView view) =>
		view.Handler?.PlatformView as NSObject ?? throw new InvalidOperationException($"{NameOf(view)} has no native view.");

	// The native accessibilityElements of the container: null when it is nil (UIKit's default).
	static NSArray? AccessibilityElementsArray(SemanticOrderViewHandler handler) =>
		(handler.PlatformView as IUIAccessibilityContainer)?.GetAccessibilityElements() as NSArray;

	static NSObject[] ElementsOf(SemanticOrderViewHandler handler) =>
		AccessibilityElementsArray(handler) is NSArray array ? NSArray.FromArray<NSObject>(array) ?? [] : [];

	// A listed element exposes a view when it is the view, or when UIKit looks into it (it is a view that is not an accessibility element)
	// and it holds the view.
	static bool Exposes(NSObject element, NSObject view) =>
		element.Handle == view.Handle
		|| (element is UIView { IsAccessibilityElement: false } container && view is UIView platformView && platformView.IsDescendantOfView(container));

	static int TimesExposed(NSObject[] elements, IView view)
	{
		var platformView = PlatformViewOf(view);
		return elements.Count(element => Exposes(element, platformView));
	}

	static int IndexOfExposure(NSObject[] elements, IView view)
	{
		var platformView = PlatformViewOf(view);
		return Array.FindIndex(elements, element => Exposes(element, platformView));
	}

	// The views of the order are the first accessibilityElements, in order, and no element after them exposes one of them again. With
	// nothing to order, accessibilityElements is nil (UIKit's default).
	static bool IsInNativeOrder(SemanticOrderViewHandler handler, params IView[] order)
	{
		if (order.Length is 0)
		{
			return AccessibilityElementsArray(handler) is null;
		}

		if (!order.All(view => view.Handler?.PlatformView is NSObject))
		{
			return false;
		}

		var orderedViews = order.Select(PlatformViewOf).ToArray();
		var elements = ElementsOf(handler);

		return elements.Take(orderedViews.Length).Select(element => element.Handle).SequenceEqual(orderedViews.Select(orderedView => orderedView.Handle))
			&& !elements.Skip(orderedViews.Length).Any(element => orderedViews.Any(orderedView => Exposes(element, orderedView)));
	}

	static string DescribeNativeOrder(SemanticOrderViewHandler handler, params IView[] views)
	{
		if (AccessibilityElementsArray(handler) is null)
		{
			return "accessibilityElements = nil";
		}

		// An element that is not one of the views is named after its type and the views that UIKit finds in it.
		var names = ElementsOf(handler).Select(element =>
		{
			if (views.FirstOrDefault(view => view.Handler?.PlatformView is NSObject platformView && platformView.Handle == element.Handle) is IView known)
			{
				return NameOf(known);
			}

			var exposed = views.Where(view => view.Handler?.PlatformView is NSObject platformView && Exposes(element, platformView)).Select(NameOf).ToList();
			return exposed.Count > 0 ? $"{element.GetType().Name} of {string.Join(" and ", exposed)}" : element.GetType().Name;
		});

		return $"accessibilityElements = [{string.Join(", ", names)}]";
	}

	// iOS builds the whole array on every request: the platform views of the order come first, in order, followed by the rest of the
	// container's content.
	static void AssertPlatformOrder(SemanticOrderViewHandler handler, params IView[] order)
	{
		var inOrder = IsInNativeOrder(handler, order);
		Assert.True(inOrder, $"Expected [{string.Join(", ", order.Select(NameOf))}] first, actual {DescribeNativeOrder(handler, order)}.");
	}

	static void AssertOrderLinks(SemanticOrderViewHandler handler, params IView[] order) => AssertPlatformOrder(handler, order);

	// A view without an order is not listed among the views of the order: it is listed after them, with the rest of the container's
	// content, or not at all.
	static void AssertNoOrder(SemanticOrderViewHandler handler, params IView[] views)
	{
		var elements = ElementsOf(handler).Select(element => element.Handle).ToList();
		var viewOrder = handler.VirtualView is ISemanticOrderView { ViewOrder: IEnumerable<IView> order } ? order : [];
		var orderedViews = viewOrder.Select(view => view.Handler?.PlatformView).OfType<NSObject>().Select(platformView => platformView.Handle).ToList();
		var endOfTheOrder = elements.FindLastIndex(orderedViews.Contains) + 1;

		foreach (var view in views)
		{
			var index = elements.IndexOf(PlatformViewOf(view).Handle);
			var ordered = index >= 0 && index < endOfTheOrder;
			Assert.False(ordered, $"{NameOf(view)} is listed among the views of the order: {DescribeNativeOrder(handler, views)}.");
		}
	}

	// Without a virtual view there is nothing to order: the container must keep the previous order or fall back to UIKit's default (nil),
	// never declare an empty list of accessibility elements.
	static void AssertPlatformOrderWithoutVirtualView(SemanticOrderViewHandler handler, params IView[] previousOrder)
	{
		var keptOrDefault = AccessibilityElementsArray(handler) is null || IsInNativeOrder(handler, previousOrder);
		Assert.True(keptOrDefault, $"Expected the previous order [{string.Join(", ", previousOrder.Select(NameOf))}] or nil, actual {DescribeNativeOrder(handler, previousOrder)}.");
	}

	sealed class PlainPlatformViewHandler : SemanticOrderViewHandler
	{
		protected override PlatformContentView CreatePlatformView() => new();
	}
}