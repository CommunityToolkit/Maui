using CommunityToolkit.Maui.Core.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;

namespace CommunityToolkit.Maui.DeviceTests;

// Windows: ViewOrder becomes the TabIndex (1, 2, 3, ...) of the native views, in order.
public partial class SemanticOrderViewTests
{
	// WinUI's default TabIndex.
	const int defaultTabIndex = int.MaxValue;

	[Fact]
	public async Task TabIndexOfTheNativeViewsFollowsTheViewOrderStartingAtOne()
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out var buttons);
		view.ViewOrder = [buttons[2], buttons[0], buttons[1]];

		var handler = await CreateHandlerAsync<SemanticOrderViewHandler>(view);

		await InvokeOnMainThreadAsync(() =>
		{
			Assert.Equal(1, PlatformViewOf(buttons[2]).TabIndex);
			Assert.Equal(2, PlatformViewOf(buttons[0]).TabIndex);
			Assert.Equal(3, PlatformViewOf(buttons[1]).TabIndex);
			AssertPlatformOrder(handler, buttons[2], buttons[0], buttons[1]);
		});
	}

	// A dropped view gets its default tab index back only while it still has the tab index that the order gave it: a tab index that the app
	// set since is kept.
	[Fact]
	public async Task DroppedViewKeepsATabIndexThatTheAppSetSince()
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out var buttons);
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

	static FrameworkElement PlatformViewOf(IView view) =>
		view.Handler?.PlatformView as FrameworkElement ?? throw new InvalidOperationException($"{NameOf(view)} has no native view.");

	// The views of the order have the tab indexes 1, 2, 3, ... in order.
	static bool IsInNativeOrder(SemanticOrderViewHandler handler, params IView[] order) =>
		order.Select((view, index) => view.Handler?.PlatformView is FrameworkElement platformView && platformView.TabIndex == index + 1).All(static inPlace => inPlace);

	static string DescribeNativeOrder(SemanticOrderViewHandler handler, params IView[] views) =>
		string.Join("; ", views.Select(view => view.Handler?.PlatformView is FrameworkElement platformView
			? $"{NameOf(view)}: TabIndex={platformView.TabIndex}"
			: $"{NameOf(view)}: no native view"));

	static void AssertPlatformOrder(SemanticOrderViewHandler handler, params IView[] order)
	{
		var inOrder = IsInNativeOrder(handler, order);
		Assert.True(inOrder, $"Expected the tab indexes 1..{order.Length} for [{string.Join(", ", order.Select(NameOf))}], actual {DescribeNativeOrder(handler, order)}.");
	}

	static void AssertOrderLinks(SemanticOrderViewHandler handler, params IView[] order) => AssertPlatformOrder(handler, order);

	static void AssertNoOrder(SemanticOrderViewHandler handler, params IView[] views)
	{
		foreach (var view in views)
		{
			var hasDefaultTabIndex = PlatformViewOf(view).TabIndex is defaultTabIndex;
			Assert.True(hasDefaultTabIndex, $"{NameOf(view)} still has a tab index of the order: {DescribeNativeOrder(handler, view)}.");
		}
	}

	// Without a virtual view the update does nothing: the indexes stay as they were.
	static void AssertPlatformOrderWithoutVirtualView(SemanticOrderViewHandler handler, params IView[] previousOrder) =>
		AssertPlatformOrder(handler, previousOrder);

	sealed class PlainPlatformViewHandler : SemanticOrderViewHandler
	{
		protected override ContentPanel CreatePlatformView() => new();
	}
}