using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Core.Views;
using Microsoft.Maui.Handlers;
using AView = Android.Views.View;
using PlatformContentViewGroup = Microsoft.Maui.Platform.ContentViewGroup;

namespace CommunityToolkit.Maui.DeviceTests;

// Android: ViewOrder links the native views with AccessibilityTraversalAfter/Before (API 22+), generating a view id for every
// view of the order that has none. OnLayout applies the order again.
public partial class SemanticOrderViewTests
{
	[Fact]
	public async Task UpdateViewOrderKeepsPositiveViewIdsAndReplacesTheOthers()
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out var buttons);
		view.ViewOrder = [buttons[0], buttons[1], buttons[2]];
		var handler = await CreateHandlerAsync<SemanticOrderViewHandler>(view);

		await InvokeOnMainThreadAsync(() =>
		{
			var container = Assert.IsType<MauiSemanticOrderView>(handler.PlatformView);
			var first = PlatformViewOf(buttons[0]);
			var second = PlatformViewOf(buttons[1]);
			var third = PlatformViewOf(buttons[2]);

			// As the second view of a pair: a positive id is kept, 0 and -1 are not valid view ids and are replaced.
			first.Id = 4242;
			second.Id = 0;
			third.Id = AView.NoId;
			container.UpdateViewOrder();

			Assert.Equal(4242, first.Id);
			Assert.True(second.Id > 0, $"id of the second view: {second.Id}");
			Assert.True(third.Id > 0, $"id of the third view: {third.Id}");
			AssertPlatformOrder(handler, buttons[0], buttons[1], buttons[2]);

			// As the first view of a pair (it is the only view that is never the second one of a pair).
			var secondId = second.Id;
			first.Id = 0;
			container.UpdateViewOrder();

			Assert.True(first.Id > 0, $"id of the first view: {first.Id}");
			Assert.NotEqual(4242, first.Id);
			Assert.Equal(secondId, second.Id);
			AssertPlatformOrder(handler, buttons[0], buttons[1], buttons[2]);

			first.Id = AView.NoId;
			container.UpdateViewOrder();

			Assert.True(first.Id > 0, $"id of the first view: {first.Id}");
			AssertPlatformOrder(handler, buttons[0], buttons[1], buttons[2]);
		});
	}

	[Fact]
	public async Task UpdateViewOrderKeepsTheViewIdsItGeneratedBefore()
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out var buttons);
		view.ViewOrder = [buttons[0], buttons[1], buttons[2]];
		var handler = await CreateHandlerAsync<SemanticOrderViewHandler>(view);

		await InvokeOnMainThreadAsync(() =>
		{
			var ids = buttons.Select(button => PlatformViewOf(button).Id).ToArray();
			Assert.All(ids, id => Assert.True(id > 0));

			Assert.IsType<MauiSemanticOrderView>(handler.PlatformView).UpdateViewOrder();

			Assert.Equal(ids, buttons.Select(button => PlatformViewOf(button).Id));
		});
	}

	[Fact]
	public async Task ViewOrderOfASingleViewGeneratesNoViewIdAndNoLinks()
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out var buttons);
		view.ViewOrder = [buttons[1]];

		await CreateHandlerAsync<SemanticOrderViewHandler>(view);

		await InvokeOnMainThreadAsync(() =>
		{
			Assert.Equal(AView.NoId, PlatformViewOf(buttons[1]).Id);
			Assert.Equal(AView.NoId, PlatformViewOf(buttons[1]).AccessibilityTraversalAfter);
			Assert.Equal(AView.NoId, PlatformViewOf(buttons[1]).AccessibilityTraversalBefore);
		});
	}

	[Fact]
	public async Task EmptyViewOrderGeneratesNoViewIds()
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out var buttons);

		await CreateHandlerAsync<SemanticOrderViewHandler>(view);

		await InvokeOnMainThreadAsync(() => Assert.All(buttons, button => Assert.Equal(AView.NoId, PlatformViewOf(button).Id)));
	}

	[Fact]
	public async Task LayoutReappliesTheViewOrder()
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out var buttons);
		view.ViewOrder = [buttons[1], buttons[0]];
		var handler = await CreateHandlerAsync<SemanticOrderViewHandler>(view);

		await InvokeOnMainThreadAsync(() =>
		{
			foreach (var button in buttons)
			{
				PlatformViewOf(button).AccessibilityTraversalAfter = AView.NoId;
				PlatformViewOf(button).AccessibilityTraversalBefore = AView.NoId;
			}

			AssertNoOrder(handler, buttons);

			// A layout pass with a changed frame calls OnLayout.
			var container = Assert.IsType<MauiSemanticOrderView>(handler.PlatformView);
			container.Layout(container.Left, container.Top, container.Right + 1, container.Bottom + 1);

			AssertPlatformOrder(handler, buttons[1], buttons[0]);
		});
	}

	// The view removes only the links it made: links that the app set itself are kept when the order changes. Here the app makes the first
	// view of the order come after a view outside of the container, and a view that is then dropped from the order come before it.
	[Fact]
	public async Task ChangingViewOrderKeepsTheLinksThatTheAppSet()
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out var buttons);
		var outside = new Button { Text = "Outside" };
		view.ViewOrder = [buttons[0], buttons[1], buttons[2]];
		var handler = await CreateHandlerAsync<SemanticOrderViewHandler>(view);

		await InvokeOnMainThreadAsync(() =>
		{
			CreateHandler<ButtonHandler>(outside);
			var outsideView = PlatformViewOf(outside);
			outsideView.Id = AView.GenerateViewId();
			PlatformViewOf(buttons[0]).AccessibilityTraversalAfter = outsideView.Id;
			PlatformViewOf(buttons[1]).AccessibilityTraversalBefore = outsideView.Id;

			view.ViewOrder = [buttons[0], buttons[2]];

			AssertOrderLinks(handler, buttons[0], buttons[2]);
			Assert.Equal(outsideView.Id, PlatformViewOf(buttons[0]).AccessibilityTraversalAfter);
			Assert.Equal(outsideView.Id, PlatformViewOf(buttons[1]).AccessibilityTraversalBefore);
			// The link that the previous order made from the dropped view is removed.
			Assert.Equal(AView.NoId, PlatformViewOf(buttons[1]).AccessibilityTraversalAfter);
		});
	}

	// A view of the previous order whose Java peer was disposed meanwhile (its handler was disconnected and the view thrown away) has no links
	// left to remove: changing the order skips it instead of failing on it.
	// Catches: RemoveLinksOfThePreviousOrder reading the links of a disposed view (ObjectDisposedException).
	[Fact]
	public async Task ChangingViewOrderAfterAViewOfThePreviousOrderWasDisposedDoesNotThrow()
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out var buttons);
		view.ViewOrder = [buttons[0], buttons[1], buttons[2]];
		var handler = await CreateHandlerAsync<SemanticOrderViewHandler>(view);

		await InvokeOnMainThreadAsync(() =>
		{
			var disposed = PlatformViewOf(buttons[1]);
			disposed.Dispose();
			Assert.Equal(IntPtr.Zero, disposed.Handle);

			view.ViewOrder = [buttons[0], buttons[2]];

			AssertOrderLinks(handler, buttons[0], buttons[2]);
		});
	}

	[Fact]
	public async Task NativeViewIsCreatedWithTheAndroidContextOfTheMauiContext()
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out _);

		var handler = await CreateHandlerAsync<SemanticOrderViewHandler>(view);

		await InvokeOnMainThreadAsync(() =>
		{
			var platformView = Assert.IsType<MauiSemanticOrderView>(handler.PlatformView);
			Assert.NotNull(MauiContext.Context);
			Assert.Equal(MauiContext.Context.Handle, platformView.Context!.Handle);
		});
	}

	[Fact]
	public async Task HandlerThrowsWhenTheMauiContextHasNoAndroidContext()
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out _);
		var handler = new SemanticOrderViewHandler();
		handler.SetMauiContext(new MauiContextWithoutAndroidContext(MauiContext));

		var exception = await InvokeOnMainThreadAsync(() => Assert.Throws<InvalidOperationException>(() => handler.SetVirtualView(view)));

		Assert.Equal(noAndroidContextMessage, exception.Message);
	}

	[Fact]
	public async Task HandlerThrowsWhenItHasNoMauiContext()
	{
		SetupBuilder();
		var view = CreateSemanticOrderView(out _);
		var handler = new SemanticOrderViewHandler();

		var exception = await InvokeOnMainThreadAsync(() => Assert.Throws<InvalidOperationException>(() => handler.SetVirtualView(view)));

		// Whichever layer notices first (the toolkit's guard or MAUI's), the view is not created without a MauiContext.
		Assert.False(string.IsNullOrWhiteSpace(exception.Message));
	}

	[Fact]
	public void CreatePlatformViewThrowsWhenTheHandlerHasNoMauiContext()
	{
		var handler = new PlatformViewCreatingHandler();

		var exception = Assert.Throws<InvalidOperationException>(() => handler.CreatePlatformViewDirectly());

		Assert.Equal(noAndroidContextMessage, exception.Message);
	}

	[Fact]
	public async Task CreatePlatformViewThrowsWhenTheMauiContextHasNoAndroidContext()
	{
		SetupBuilder();
		var handler = new PlatformViewCreatingHandler();
		handler.SetMauiContext(new MauiContextWithoutAndroidContext(MauiContext));

		var exception = await InvokeOnMainThreadAsync(() => Assert.Throws<InvalidOperationException>(() => handler.CreatePlatformViewDirectly()));

		Assert.Equal(noAndroidContextMessage, exception.Message);
	}

	[Fact]
	public async Task CreatePlatformViewReturnsAMauiSemanticOrderViewOfTheAndroidContext()
	{
		SetupBuilder();
		var handler = new PlatformViewCreatingHandler();
		handler.SetMauiContext(MauiContext);

		var platformView = await InvokeOnMainThreadAsync(() => handler.CreatePlatformViewDirectly());

		Assert.IsType<MauiSemanticOrderView>(platformView);
		Assert.Equal(MauiContext.Context!.Handle, platformView.Context!.Handle);
	}

	[Fact]
	public async Task BelowAndroid22ViewIdsAreGeneratedButNothingIsLinked()
	{
		Skip.If(OperatingSystem.IsAndroidVersionAtLeast(22), "Needs an Android 21 device or emulator (the pooled ones are API 28 and 36): below API 22 the toolkit only traces that the order is not supported.");

		SetupBuilder();
		var view = CreateSemanticOrderView(out var buttons);
		view.ViewOrder = [buttons[0], buttons[1]];

		await CreateHandlerAsync<SemanticOrderViewHandler>(view);

		// AccessibilityTraversalAfter/Before do not exist below API 22, so only the generated ids can be read.
		await InvokeOnMainThreadAsync(() => Assert.All(buttons.Take(2), button => Assert.True(PlatformViewOf(button).Id > 0)));
	}

	static AView PlatformViewOf(IView view) =>
		view.Handler?.PlatformView as AView ?? throw new InvalidOperationException($"{NameOf(view)} has no native view.");

	static bool IsLinked(IView first, IView second) =>
		first.Handler?.PlatformView is AView firstView
		&& second.Handler?.PlatformView is AView secondView
		&& firstView.Id > 0
		&& secondView.Id > 0
		&& firstView.AccessibilityTraversalBefore == secondView.Id
		&& secondView.AccessibilityTraversalAfter == firstView.Id;

	// Every consecutive pair of the order is linked in both directions.
	static bool IsInNativeOrder(SemanticOrderViewHandler handler, params IView[] order)
	{
		for (var i = 1; i < order.Length; i++)
		{
			if (!IsLinked(order[i - 1], order[i]))
			{
				return false;
			}
		}

		return true;
	}

	static string DescribeNativeOrder(SemanticOrderViewHandler handler, params IView[] views) =>
		string.Join("; ", views.Select(view => view.Handler?.PlatformView is AView platformView
			? $"{NameOf(view)}: id={platformView.Id}, after={platformView.AccessibilityTraversalAfter}, before={platformView.AccessibilityTraversalBefore}"
			: $"{NameOf(view)}: no native view"));

	// Android keeps one link per direction on every view: the view that follows it, and the view that precedes it.
	static void AssertOrderLinks(SemanticOrderViewHandler handler, params IView[] order)
	{
		var views = order.Select(PlatformViewOf).ToArray();
		if (views.Length < 2)
		{
			return;
		}

		Assert.All(views, view => Assert.True(view.Id > 0, $"view id: {view.Id}"));
		Assert.Equal(views.Length, views.Select(view => view.Id).Distinct().Count());
		for (var i = 1; i < views.Length; i++)
		{
			var linked = IsLinked(order[i - 1], order[i]);
			Assert.True(linked, $"{NameOf(order[i - 1])} and {NameOf(order[i])} are not linked: {DescribeNativeOrder(handler, order)}");
		}
	}

	// The links of the order, and nothing else: the first view has no view before it and the last no view after it.
	static void AssertPlatformOrder(SemanticOrderViewHandler handler, params IView[] order)
	{
		AssertOrderLinks(handler, order);

		var views = order.Select(PlatformViewOf).ToArray();
		if (views.Length == 0)
		{
			return;
		}

		var firstHasNoViewBefore = views[0].AccessibilityTraversalAfter is AView.NoId;
		var lastHasNoViewAfter = views[^1].AccessibilityTraversalBefore is AView.NoId;
		Assert.True(firstHasNoViewBefore, $"The first view of the order still comes after another view: {DescribeNativeOrder(handler, order)}");
		Assert.True(lastHasNoViewAfter, $"The last view of the order still comes before another view: {DescribeNativeOrder(handler, order)}");
	}

	static void AssertNoOrder(SemanticOrderViewHandler handler, params IView[] views)
	{
		foreach (var view in views)
		{
			var platformView = PlatformViewOf(view);
			var unlinked = platformView.AccessibilityTraversalAfter is AView.NoId && platformView.AccessibilityTraversalBefore is AView.NoId;
			Assert.True(unlinked, $"{NameOf(view)} still has a native order link (expected {AView.NoId} in both directions): {DescribeNativeOrder(handler, view)}");
		}
	}

	// Without a virtual view the update returns: the links stay as they were.
	static void AssertPlatformOrderWithoutVirtualView(SemanticOrderViewHandler handler, params IView[] previousOrder) =>
		AssertPlatformOrder(handler, previousOrder);

	const string noAndroidContextMessage = "Android Context is null, please ensure your MauiApplication is initialized.";

	// Exposes the protected CreatePlatformView so that its guard is reached without going through MAUI's SetVirtualView first.
	sealed class PlatformViewCreatingHandler : SemanticOrderViewHandler
	{
		public PlatformContentViewGroup CreatePlatformViewDirectly() => CreatePlatformView();
	}

	sealed class PlainPlatformViewHandler : SemanticOrderViewHandler
	{
		protected override PlatformContentViewGroup CreatePlatformView() => new(MauiContext!.Context!);
	}

	sealed class MauiContextWithoutAndroidContext(IMauiContext inner) : IMauiContext
	{
		public IServiceProvider Services => inner.Services;

		public IMauiHandlersFactory Handlers => inner.Handlers;

		public Android.Content.Context? Context => null;
	}
}