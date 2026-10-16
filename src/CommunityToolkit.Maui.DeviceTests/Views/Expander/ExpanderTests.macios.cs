#pragma warning disable CS0618 // ListView and ViewCell are obsolete, but Expander still supports them in a ListView
using CommunityToolkit.Maui.Views;
using Foundation;
using Microsoft.Maui.Handlers;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class ExpanderTests
{
	[Fact]
	public async Task HeaderTapInsideAListViewWhosePlatformViewIsNotATableViewTogglesAndDoesNotThrow()
	{
		SetupBuilder(builder => builder.ConfigureMauiHandlers(handlers => handlers.AddHandler<ListView, NonTableListViewHandler>()));
		var listView = CreateListView(itemCount: 1);
		var expander = GetExpander(listView, 0);

		var exception = await Record.ExceptionAsync(() => InvokeOnMainThreadAsync(() =>
		{
			var handler = CreateHandler<NonTableListViewHandler>(listView);
			Assert.IsNotType<UITableView>(handler.PlatformView);

			expander.HandleHeaderTap(new TappedEventArgs(null));
		}));

		Assert.Null(exception);
		Assert.True(expander.IsExpanded);
	}

	[Fact]
	public async Task HeaderTapReloadsTheTableViewOfEveryListViewTheExpanderIsIn()
	{
		SetupBuilder(builder => builder.ConfigureMauiHandlers(handlers => handlers.AddHandler<ListView, ReloadCountingListViewHandler>()));
		var outer = new ListView();
		var inner = new ListView();
		var expander = new Expander { Header = new Label { Text = "Header" }, Content = new Label { Text = "Content" } };

		inner.Parent = outer;
		expander.Parent = inner;

		await InvokeOnMainThreadAsync(() =>
		{
			var outerTable = CreateHandler<ReloadCountingListViewHandler>(outer).PlatformView;
			var innerTable = CreateHandler<ReloadCountingListViewHandler>(inner).PlatformView;

			expander.HandleHeaderTap(new TappedEventArgs(null));

			Assert.True(expander.IsExpanded);
			Assert.Equal(1, innerTable.ReloadCount);
			Assert.Equal(1, outerTable.ReloadCount);

			expander.HandleHeaderTap(new TappedEventArgs(null));

			Assert.False(expander.IsExpanded);
			Assert.Equal(2, innerTable.ReloadCount);
			Assert.Equal(2, outerTable.ReloadCount);
		});
	}

	[Fact]
	public async Task HeaderTapWithoutAHeaderDoesNotReloadTheTableViewOfTheListView()
	{
		SetupBuilder(builder => builder.ConfigureMauiHandlers(handlers => handlers.AddHandler<ListView, ReloadCountingListViewHandler>()));
		var listView = new ListView();
		var expander = new Expander { Content = new Label { Text = "Content" } };
		expander.Parent = listView;

		await InvokeOnMainThreadAsync(() =>
		{
			var table = CreateHandler<ReloadCountingListViewHandler>(listView).PlatformView;

			// Without a header, ResizeExpanderInItemsView returns before it walks the parents.
			expander.HandleHeaderTap(new TappedEventArgs(null));

			Assert.True(expander.IsExpanded);
			Assert.Equal(0, table.ReloadCount);
		});
	}

	static bool IsPlatformViewVisible(IView view) => !((UIView)view.Handler!.PlatformView!).Hidden;

	static double GetRowHeight(ListView listView, int row) => GetRowFrame(listView, row).Height;

	static double GetRowTop(ListView listView, int row) => GetRowFrame(listView, row).Y;

	static CoreGraphics.CGRect GetRowFrame(ListView listView, int row)
	{
		var table = Assert.IsType<UITableView>(listView.Handler!.PlatformView);
		table.LayoutIfNeeded();
		return table.RectForRowAtIndexPath(NSIndexPath.FromRowSection(row, 0));
	}

	// A plain UIView as platform view reaches the `as UITableView` null branch.
	sealed class NonTableListViewHandler : ViewHandler<ListView, UIView>
	{
		public NonTableListViewHandler() : base(ViewMapper)
		{
		}

		protected override UIView CreatePlatformView() => new();
	}

	sealed class ReloadCountingListViewHandler : ViewHandler<ListView, ReloadCountingTableView>
	{
		public ReloadCountingListViewHandler() : base(ViewMapper)
		{
		}

		protected override ReloadCountingTableView CreatePlatformView() => new();
	}

	public sealed class ReloadCountingTableView : UITableView
	{
		public int ReloadCount { get; private set; }

		public override void ReloadData()
		{
			ReloadCount++;
			base.ReloadData();
		}
	}
}