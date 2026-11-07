#pragma warning disable CS0618 // ListView and ViewCell are obsolete, but Expander still supports them in a ListView
using System.Reflection;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using LayoutPanel = Microsoft.Maui.Platform.LayoutPanel;
using WControl = Microsoft.UI.Xaml.Controls.Control;
using WFocusState = Microsoft.UI.Xaml.FocusState;
using WFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using WGrid = Microsoft.UI.Xaml.Controls.Grid;
using WGridView = Microsoft.UI.Xaml.Controls.GridView;
using WGridViewItem = Microsoft.UI.Xaml.Controls.GridViewItem;
using WListView = Microsoft.UI.Xaml.Controls.ListView;
using WListViewItem = Microsoft.UI.Xaml.Controls.ListViewItem;
using WPoint = Windows.Foundation.Point;
using WThickness = Microsoft.UI.Xaml.Thickness;
using WVisibility = Microsoft.UI.Xaml.Visibility;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class ExpanderTests
{
	const double collectionViewWidth = 400;
	const double collectionViewHeight = 400;

	static readonly TimeSpan windowsLayoutTimeout = TimeSpan.FromSeconds(5);

	static bool IsPlatformViewVisible(IView view) => ((WFrameworkElement)view.Handler!.PlatformView!).Visibility == WVisibility.Visible;

	static ExpanderLayoutPanel GetLayoutPanel(Expander expander) => Assert.IsType<ExpanderLayoutPanel>(((ContentView)expander).Content?.Handler?.PlatformView);

	static AutomationPeer GetAutomationPeer(Expander expander) =>
		FrameworkElementAutomationPeer.CreatePeerForElement(GetLayoutPanel(expander)) ?? throw new InvalidOperationException("The layout of the Expander has no automation peer.");

	static IExpandCollapseProvider GetExpandCollapseProvider(AutomationPeer peer) => Assert.IsType<IExpandCollapseProvider>(peer.GetPattern(PatternInterface.ExpandCollapse), exactMatch: false);

	static IInvokeProvider GetInvokeProvider(AutomationPeer peer) => Assert.IsType<IInvokeProvider>(peer.GetPattern(PatternInterface.Invoke), exactMatch: false);

	static double GetRowHeight(ListView listView, int row) => GetListViewItem(listView, row)?.ActualHeight ?? 0;

	static double GetRowTop(ListView listView, int row) =>
		GetListViewItem(listView, row) is WListViewItem container ? GetTop(container, GetNativeListView(listView)) : 0;

	static WListView GetNativeListView(ListView listView) => Assert.IsType<WListView>(listView.Handler!.PlatformView, exactMatch: false);

	static WListViewItem? GetListViewItem(ListView listView, int row) => GetNativeListView(listView).ContainerFromIndex(row) as WListViewItem;

	static double GetTop(WFrameworkElement element, WFrameworkElement relativeTo) =>
		element.TransformToVisual(relativeTo).TransformPoint(new WPoint(0, 0)).Y;

	[Fact]
	public async Task HeaderTapInsideAListViewAsksTheCellOfTheTappedExpanderToUpdateItsSize()
	{
		SetupBuilder();
		var listView = CreateListView(itemCount: 3);

		await AttachAndRun(new ContentPage { Content = listView }, async _ =>
		{
			await AssertionExtensions.AssertEventually(() => GetRowHeight(listView, 1) > 0, timeout: 5000, message: "The ListView never realized its rows.");
			var tappedExpander = GetExpander(listView, 1);
			var tappedCell = Assert.IsType<Cell>(tappedExpander.Parent, exactMatch: false);
			var otherCell = Assert.IsType<Cell>(GetExpander(listView, 0).Parent, exactMatch: false);
			var tappedCellRequests = new TaskCompletionSource();
			var otherCellRequests = 0;
			tappedCell.ForceUpdateSizeRequested += (_, _) => tappedCellRequests.TrySetResult();
			otherCell.ForceUpdateSizeRequested += (_, _) => otherCellRequests++;

			tappedExpander.HandleHeaderTap(new TappedEventArgs(null));

			// Cell.ForceUpdateSize raises the event after its own 16 ms delay.
			await tappedCellRequests.Task.WaitAsync(windowsLayoutTimeout);
			Assert.Equal(0, otherCellRequests);
		});
	}

	[Fact]
	public async Task HeaderTapInsideACollectionViewResizesOnlyTheItemContainerThatWasTapped()
	{
		SetupBuilder();
		var (collectionView, expanders) = CreateCollectionView(new LinearItemsLayout(ItemsLayoutOrientation.Vertical), itemCount: 4);

		await AttachAndRun(collectionView, async handler =>
		{
			var native = Assert.IsType<WListView>(handler.PlatformView, exactMatch: false);
			await WaitForRealizedItemsAsync(native, 4);
			var heightsBefore = Enumerable.Range(0, 4).Select(index => GetItemContainer(native, index).Height).ToArray();
			var tapY = GetItemContainer(native, 0).ActualHeight + GetItemContainer(native, 1).ActualHeight / 2;
			var expander = GetExpanderOfItem(expanders, 1);

			expander.HandleHeaderTap(new PositionedTappedEventArgs(new Point(10, tapY)));

			Assert.True(expander.IsExpanded);
			var expandedHeight = expander.Measure(double.PositiveInfinity, double.PositiveInfinity).Height;
			Assert.True(expandedHeight >= headerHeight + contentHeight - 1, $"The expanded Expander should measure at least header + content but measured {expandedHeight}.");
			Assert.Equal(expandedHeight, GetItemContainer(native, 1).Height, precision: 1);
			Assert.Equal(heightsBefore[0], GetItemContainer(native, 0).Height);
			Assert.Equal(heightsBefore[2], GetItemContainer(native, 2).Height);
			Assert.Equal(heightsBefore[3], GetItemContainer(native, 3).Height);
		});
	}

	[Fact]
	public async Task HeaderTapOnAnExpandedExpanderInACollectionViewShrinksItsContainerToTheHeader()
	{
		SetupBuilder();
		var (collectionView, expanders) = CreateCollectionView(new LinearItemsLayout(ItemsLayoutOrientation.Vertical), itemCount: 3);

		await AttachAndRun(collectionView, async handler =>
		{
			var native = Assert.IsType<WListView>(handler.PlatformView, exactMatch: false);
			await WaitForRealizedItemsAsync(native, 3);
			var expander = GetExpanderOfItem(expanders, 0);
			var tapY = GetItemContainer(native, 0).ActualHeight / 2;

			expander.HandleHeaderTap(new PositionedTappedEventArgs(new Point(10, tapY)));
			Assert.True(expander.IsExpanded);
			Assert.True(GetItemContainer(native, 0).Height >= headerHeight + contentHeight - 1);

			// The container is as tall as the expanded Expander now, so the same tap position still hits it.
			expander.HandleHeaderTap(new PositionedTappedEventArgs(new Point(10, tapY)));

			Assert.False(expander.IsExpanded);
			Assert.Equal(expander.Header.Measure(double.PositiveInfinity, double.PositiveInfinity).Height, GetItemContainer(native, 0).Height, precision: 1);
			Assert.Equal(headerHeight, GetItemContainer(native, 0).Height, precision: 1);
		});
	}

	[Theory]
	[InlineData(-1.0)]
	[InlineData(100000.0)]
	public async Task HeaderTapAwayFromEveryItemOfACollectionViewResizesNoContainer(double tapY)
	{
		SetupBuilder();
		var (collectionView, expanders) = CreateCollectionView(new LinearItemsLayout(ItemsLayoutOrientation.Vertical), itemCount: 3);

		await AttachAndRun(collectionView, async handler =>
		{
			var native = Assert.IsType<WListView>(handler.PlatformView, exactMatch: false);
			await WaitForRealizedItemsAsync(native, 3);
			var heightsBefore = Enumerable.Range(0, 3).Select(index => GetItemContainer(native, index).Height).ToArray();

			GetExpanderOfItem(expanders, 1).HandleHeaderTap(new PositionedTappedEventArgs(new Point(10, tapY)));

			Assert.Equal(heightsBefore, Enumerable.Range(0, 3).Select(index => GetItemContainer(native, index).Height));
		});
	}

	[Fact]
	public async Task HeaderTapWithoutAPositionInACollectionViewResizesNoContainer()
	{
		SetupBuilder();
		var (collectionView, expanders) = CreateCollectionView(new LinearItemsLayout(ItemsLayoutOrientation.Vertical), itemCount: 3);

		await AttachAndRun(collectionView, async handler =>
		{
			var native = Assert.IsType<WListView>(handler.PlatformView, exactMatch: false);
			await WaitForRealizedItemsAsync(native, 3);
			var heightsBefore = Enumerable.Range(0, 3).Select(index => GetItemContainer(native, index).Height).ToArray();
			var expander = GetExpanderOfItem(expanders, 1);

			var args = new TappedEventArgs(null);
			Assert.Null(args.GetPosition(collectionView));
			expander.HandleHeaderTap(args);

			Assert.True(expander.IsExpanded);
			Assert.Equal(heightsBefore, Enumerable.Range(0, 3).Select(index => GetItemContainer(native, index).Height));
		});
	}

	[Fact]
	public async Task HeaderTapInACollectionViewSkipsItemsThatAreNotRealized()
	{
		SetupBuilder();
		const int itemCount = 200;
		var (collectionView, expanders) = CreateCollectionView(new LinearItemsLayout(ItemsLayoutOrientation.Vertical), itemCount);

		await AttachAndRun(collectionView, async handler =>
		{
			var native = Assert.IsType<WListView>(handler.PlatformView, exactMatch: false);
			await WaitForRealizedItemsAsync(native, 3);
			Assert.Equal(itemCount, native.Items.Count);
			Assert.Null(native.ContainerFromIndex(itemCount - 1));
			var expander = GetExpanderOfItem(expanders, 0);
			var tapY = GetItemContainer(native, 0).ActualHeight / 2;

			var exception = Record.Exception(() => expander.HandleHeaderTap(new PositionedTappedEventArgs(new Point(10, tapY))));

			Assert.Null(exception);
			Assert.True(GetItemContainer(native, 0).Height >= headerHeight + contentHeight - 1);
		});
	}

	// The ListView branch of ForceUpdateCellSize used to compare the tap with offsets summed from the heights of the realized item containers
	// (a container it resized moved the offsets by its new height), which resized the wrong container in a horizontal list, after a collapse whose
	// tap lies below the new height of the container, and in a scrolled list. It now compares the tap with the bounds of each container where the
	// list shows it, like the GridView branch.

	[Fact]
	public async Task HeaderTapInAHorizontalCollectionViewResizesTheContainerOfTheTappedItem()
	{
		SetupBuilder();
		var (collectionView, expanders) = CreateCollectionView(new LinearItemsLayout(ItemsLayoutOrientation.Horizontal), itemCount: 4);

		await AttachAndRun(collectionView, async handler =>
		{
			var native = Assert.IsType<WListView>(handler.PlatformView, exactMatch: false);
			await WaitForRealizedItemsAsync(native, 3);
			var heightsBefore = GetItemContainerHeights(native, 3);
			var expander = GetExpanderOfItem(expanders, 2);

			expander.HandleHeaderTap(new PositionedTappedEventArgs(CenterOf(GetItemContainer(native, 2), native)));

			var heightsAfter = GetItemContainerHeights(native, 3);

			var expandedHeight = expander.Measure(double.PositiveInfinity, double.PositiveInfinity).Height;
			Assert.Equal(expandedHeight, heightsAfter[2], precision: 1);
			Assert.Equal(heightsBefore[0], heightsAfter[0]);
			Assert.Equal(heightsBefore[1], heightsAfter[1]);
		});
	}

	// With ExpandDirection.Up the header is the bottom of the container, so the tap lies below the collapsed height and also inside the range
	// that the summed offsets gave container 1 after the resize; with Down that happens only for a tap on the bottom edge of the header (that
	// range included both ends). The tap in the middle of a Down header is the control row.
	[Theory]
	[InlineData(ExpandDirection.Up, 0.5)]
	[InlineData(ExpandDirection.Down, 1.0)]
	[InlineData(ExpandDirection.Down, 0.5)]
	public async Task HeaderTapCollapsingAnExpanderInACollectionViewKeepsTheContainerOfTheNextItem(ExpandDirection direction, double tapInHeader)
	{
		SetupBuilder();
		var (collectionView, expanders) = CreateCollectionView(new LinearItemsLayout(ItemsLayoutOrientation.Vertical), itemCount: 3, direction);

		await AttachAndRun(collectionView, async handler =>
		{
			var native = Assert.IsType<WListView>(handler.PlatformView, exactMatch: false);
			await WaitForRealizedItemsAsync(native, 3);
			var first = GetExpanderOfItem(expanders, 0);
			var second = GetExpanderOfItem(expanders, 1);

			first.HandleHeaderTap(new PositionedTappedEventArgs(new Point(10, GetTop(GetItemContainer(native, 0), native) + GetItemContainer(native, 0).ActualHeight / 2)));
			await WaitForContainerHeightAsync(native, 0, headerHeight + contentHeight);
			second.HandleHeaderTap(new PositionedTappedEventArgs(new Point(10, GetTop(GetItemContainer(native, 1), native) + GetItemContainer(native, 1).ActualHeight / 2)));
			await WaitForContainerHeightAsync(native, 1, headerHeight + contentHeight);
			Assert.True(first.IsExpanded);
			Assert.True(second.IsExpanded);
			var heightsBefore = GetItemContainerHeights(native, 3);

			var container = GetItemContainer(native, 0);
			var headerTop = GetTop(container, native) + (direction == ExpandDirection.Up ? container.ActualHeight - headerHeight : 0);
			first.HandleHeaderTap(new PositionedTappedEventArgs(new Point(10, headerTop + tapInHeader * headerHeight)));

			Assert.False(first.IsExpanded);
			var heightsAfter = GetItemContainerHeights(native, 3);
			var collapsedHeight = first.Header.Measure(double.PositiveInfinity, double.PositiveInfinity).Height;

			Assert.Equal(collapsedHeight, heightsAfter[0], precision: 1);
			Assert.Equal(heightsBefore[1], heightsAfter[1]);
			Assert.Equal(heightsBefore[2], heightsAfter[2]);
		});
	}

	[Fact]
	public async Task HeaderTapInAScrolledCollectionViewResizesTheContainerOfTheTappedItem()
	{
		SetupBuilder();
		const int itemCount = 200;
		const int tappedItem = 50;
		var (collectionView, expanders) = CreateCollectionView(new LinearItemsLayout(ItemsLayoutOrientation.Vertical), itemCount);

		await AttachAndRun(collectionView, async handler =>
		{
			var native = Assert.IsType<WListView>(handler.PlatformView, exactMatch: false);
			await WaitForRealizedItemsAsync(native, 3);

			collectionView.ScrollTo(tappedItem, position: ScrollToPosition.Start, animate: false);
			await AssertionExtensions.AssertEventually(
				() => native.ContainerFromIndex(tappedItem) is WFrameworkElement { ActualHeight: > 0 } container && Math.Abs(GetTop(container, native)) < headerHeight,
				timeout: 5000,
				message: $"The CollectionView never scrolled item {tappedItem} to its top.");
			var realized = Enumerable.Range(0, itemCount).Where(index => native.ContainerFromIndex(index) is WListViewItem).ToArray();
			var heightsBefore = realized.ToDictionary(index => index, index => GetItemContainer(native, index).Height);
			var expander = GetExpanderOfItem(expanders, tappedItem);

			// The tap position is relative to the CollectionView, i.e. to what it shows: the header of the item at its top.
			expander.HandleHeaderTap(new PositionedTappedEventArgs(new Point(10, GetTop(GetItemContainer(native, tappedItem), native) + headerHeight / 2)));

			Assert.True(expander.IsExpanded);
			var changed = realized.Where(index => !GetItemContainer(native, index).Height.Equals(heightsBefore[index])).ToArray();

			var expandedHeight = expander.Measure(double.PositiveInfinity, double.PositiveInfinity).Height;
			Assert.Equal([tappedItem], changed);
			Assert.Equal(expandedHeight, GetItemContainer(native, tappedItem).Height, precision: 1);
		});
	}

	[Fact]
	public async Task HeaderTapInAGridCollectionViewResizesEveryContainerOfTheTappedRow()
	{
		SetupBuilder();
		var (collectionView, expanders) = CreateCollectionView(new GridItemsLayout(2, ItemsLayoutOrientation.Vertical), itemCount: 4);

		await AttachAndRun(collectionView, async handler =>
		{
			var native = Assert.IsType<WGridView>(handler.PlatformView, exactMatch: false);
			await WaitForRealizedItemsAsync(native, 4);
			var heightsBefore = Enumerable.Range(0, 4).Select(index => GetGridItemContainer(native, index).Height).ToArray();
			var expander = GetExpanderOfItem(expanders, 3);

			expander.HandleHeaderTap(new PositionedTappedEventArgs(CenterOf(GetGridItemContainer(native, 3), native)));

			var expandedHeight = expander.Measure(double.PositiveInfinity, double.PositiveInfinity).Height;
			Assert.True(expandedHeight >= headerHeight + contentHeight - 1);
			// IterateItemsInRow adds a random fraction below 1 to the height, so that the container always changes size.
			Assert.InRange(GetGridItemContainer(native, 2).Height, expandedHeight, expandedHeight + 1);
			Assert.InRange(GetGridItemContainer(native, 3).Height, expandedHeight, expandedHeight + 1);
			Assert.Equal(heightsBefore[0], GetGridItemContainer(native, 0).Height);
			Assert.Equal(heightsBefore[1], GetGridItemContainer(native, 1).Height);
		});
	}

	[Fact]
	public async Task HeaderTapInTheLastShortRowOfAGridCollectionViewResizesOnlyItsContainers()
	{
		SetupBuilder();
		var (collectionView, expanders) = CreateCollectionView(new GridItemsLayout(2, ItemsLayoutOrientation.Vertical), itemCount: 3);

		await AttachAndRun(collectionView, async handler =>
		{
			var native = Assert.IsType<WGridView>(handler.PlatformView, exactMatch: false);
			await WaitForRealizedItemsAsync(native, 3);
			var heightsBefore = Enumerable.Range(0, 3).Select(index => GetGridItemContainer(native, index).Height).ToArray();
			var expander = GetExpanderOfItem(expanders, 2);

			// The last row only holds item 2 while the span is 2, so IterateItemsInRow must stop at the end of the items.
			var exception = Record.Exception(() => expander.HandleHeaderTap(new PositionedTappedEventArgs(CenterOf(GetGridItemContainer(native, 2), native))));

			Assert.Null(exception);
			Assert.InRange(GetGridItemContainer(native, 2).Height, headerHeight + contentHeight - 1, double.MaxValue);
			Assert.Equal(heightsBefore[0], GetGridItemContainer(native, 0).Height);
			Assert.Equal(heightsBefore[1], GetGridItemContainer(native, 1).Height);
		});
	}

	[Fact]
	public async Task HeaderTapOutsideEveryItemOfAGridCollectionViewResizesNoContainer()
	{
		SetupBuilder();
		var (collectionView, expanders) = CreateCollectionView(new GridItemsLayout(2, ItemsLayoutOrientation.Vertical), itemCount: 4);

		await AttachAndRun(collectionView, async handler =>
		{
			var native = Assert.IsType<WGridView>(handler.PlatformView, exactMatch: false);
			await WaitForRealizedItemsAsync(native, 4);
			var heightsBefore = Enumerable.Range(0, 4).Select(index => GetGridItemContainer(native, index).Height).ToArray();

			GetExpanderOfItem(expanders, 0).HandleHeaderTap(new PositionedTappedEventArgs(new Point(-50, -50)));

			Assert.Equal(heightsBefore, Enumerable.Range(0, 4).Select(index => GetGridItemContainer(native, index).Height));
		});
	}

	[Fact]
	public async Task HeaderTapWithoutAPositionInAGridCollectionViewResizesNoContainer()
	{
		SetupBuilder();
		var (collectionView, expanders) = CreateCollectionView(new GridItemsLayout(2, ItemsLayoutOrientation.Vertical), itemCount: 4);

		await AttachAndRun(collectionView, async handler =>
		{
			var native = Assert.IsType<WGridView>(handler.PlatformView, exactMatch: false);
			await WaitForRealizedItemsAsync(native, 4);
			var heightsBefore = Enumerable.Range(0, 4).Select(index => GetGridItemContainer(native, index).Height).ToArray();

			GetExpanderOfItem(expanders, 1).HandleHeaderTap(new TappedEventArgs(null));

			Assert.Equal(heightsBefore, Enumerable.Range(0, 4).Select(index => GetGridItemContainer(native, index).Height));
		});
	}

	[Fact]
	public async Task HeaderTapInAGridViewWhoseItemsLayoutWasReplacedByAListLayoutResizesNoContainer()
	{
		SetupBuilder();
		var (collectionView, expanders) = CreateCollectionView(new GridItemsLayout(2, ItemsLayoutOrientation.Vertical), itemCount: 4);

		await AttachAndRun(collectionView, async handler =>
		{
			var native = Assert.IsType<WGridView>(handler.PlatformView, exactMatch: false);
			await WaitForRealizedItemsAsync(native, 4);

			// MAUI keeps the platform view it created, so the GridView stays while the virtual layout is not a grid anymore.
			collectionView.ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical);
			Assert.Same(native, handler.PlatformView);
			await WaitForRealizedItemsAsync(native, 4);
			var heightsBefore = Enumerable.Range(0, 4).Select(index => GetGridItemContainer(native, index).Height).ToArray();

			GetExpanderOfItem(expanders, 0).HandleHeaderTap(new PositionedTappedEventArgs(CenterOf(GetGridItemContainer(native, 0), native)));

			Assert.Equal(heightsBefore, Enumerable.Range(0, 4).Select(index => GetGridItemContainer(native, index).Height));
		});
	}

	[Fact]
	public async Task HeaderTapInAGridCollectionViewSkipsItemsThatAreNotRealized()
	{
		SetupBuilder();
		const int itemCount = 400;
		var (collectionView, expanders) = CreateCollectionView(new GridItemsLayout(2, ItemsLayoutOrientation.Vertical), itemCount);

		await AttachAndRun(collectionView, async handler =>
		{
			var native = Assert.IsType<WGridView>(handler.PlatformView, exactMatch: false);
			await WaitForRealizedItemsAsync(native, 4);
			Assert.Equal(itemCount, native.Items.Count);
			Assert.Null(native.ContainerFromIndex(itemCount - 1));
			var heightsBefore = Enumerable.Range(0, 4).Select(index => GetGridItemContainer(native, index).Height).ToArray();

			// The tap is below every item, so the loop does not stop at an item: it passes over the items that are not realized.
			var exception = Record.Exception(() => GetExpanderOfItem(expanders, 0).HandleHeaderTap(new PositionedTappedEventArgs(new Point(10, 100000))));

			Assert.Null(exception);
			Assert.Equal(heightsBefore, Enumerable.Range(0, 4).Select(index => GetGridItemContainer(native, index).Height));
		});
	}

	[Fact]
	public async Task IterateItemsInRowSkipsItemsWhoseContainersAreNotRealized()
	{
		SetupBuilder();
		var iterateItemsInRow = typeof(Expander).GetMethod("IterateItemsInRow", BindingFlags.NonPublic | BindingFlags.Static)
			?? throw new InvalidOperationException("Expander has no 'IterateItemsInRow' method: the Expander changed.");

		await InvokeOnMainThreadAsync(() =>
		{
			// An ItemsControl that is not in a visual tree never generates item containers.
			var itemsControl = new Microsoft.UI.Xaml.Controls.ItemsControl();
			for (var index = 0; index < 4; index++)
			{
				itemsControl.Items.Add($"Item {index}");
			}

			Assert.Null(itemsControl.ContainerFromIndex(1));

			// The row of item 1 holds the items 0 and 1, and neither has a container.
			var exception = Record.Exception(() => iterateItemsInRow.Invoke(null, [itemsControl, 1, 2, 50.0]));

			Assert.Null(exception);
		});
	}

	[Fact]
	public async Task HeaderTapInACollectionViewWithoutAHandlerTogglesAndDoesNotThrow()
	{
		SetupBuilder();
		var collectionView = new CollectionView();
		var expander = new Expander { Header = new Label { Text = "Header", HeightRequest = headerHeight }, Content = new BoxView { Color = Colors.Red, HeightRequest = contentHeight } };

		// The CollectionView is the parent of the Expander, as it is for the template root of an item, but it has no platform view.
		expander.Parent = collectionView;
		Assert.Null(collectionView.Handler);

		var exception = await Record.ExceptionAsync(() => InvokeOnMainThreadAsync(() => expander.HandleHeaderTap(new PositionedTappedEventArgs(new Point(10, 10)))));

		Assert.Null(exception);
		Assert.True(expander.IsExpanded);
	}

	[Fact]
	public async Task HeaderTapInACollectionViewWhosePlatformViewIsNeitherAListNorAGridTogglesAndDoesNotThrow()
	{
		SetupBuilder(builder => builder.ConfigureMauiHandlers(handlers => handlers.AddHandler<CollectionView, NonListCollectionViewHandler>()));
		var collectionView = new CollectionView();
		var expander = new Expander { Header = new Label { Text = "Header", HeightRequest = headerHeight }, Content = new BoxView { Color = Colors.Red, HeightRequest = contentHeight } };
		expander.Parent = collectionView;

		var exception = await Record.ExceptionAsync(() => InvokeOnMainThreadAsync(() =>
		{
			var handler = CreateHandler<NonListCollectionViewHandler>(collectionView);
			Assert.IsType<WGrid>(handler.PlatformView);

			expander.HandleHeaderTap(new PositionedTappedEventArgs(new Point(10, 10)));
		}));

		Assert.Null(exception);
		Assert.True(expander.IsExpanded);
	}

	// MAUI's LayoutHandler.PlatformViewFactory creates the platform view of every layout handler, so an app that sets it replaces the
	// ExpanderLayoutPanel of the Expander's layout too
	[Fact]
	public async Task AnExpanderWhoseLayoutGotItsPlatformViewFromTheLayoutFactoryOfTheAppTogglesAndDoesNotThrow()
	{
		SetupBuilder();
		var expander = CreateExpander();
		var platformViewsOfTheFactory = new List<LayoutPanel>();
		IViewHandler? layoutHandler = null;
		object? layoutPlatformView = null;

		var exception = await Record.ExceptionAsync(() => InvokeOnMainThreadAsync(() =>
		{
			var previousFactory = LayoutHandler.PlatformViewFactory;
			LayoutHandler.PlatformViewFactory = _ =>
			{
				var platformView = new LayoutPanel();
				platformViewsOfTheFactory.Add(platformView);
				return platformView;
			};
			try
			{
				var handler = CreateHandler<ContentViewHandler>(expander);
				layoutHandler = ((ContentView)expander).Content?.Handler;
				layoutPlatformView = layoutHandler?.PlatformView;

				expander.HandleHeaderTap(new TappedEventArgs(null));
				((IElementHandler)handler).DisconnectHandler();
			}
			finally
			{
				LayoutHandler.PlatformViewFactory = previousFactory;
			}
		}));

		Assert.Null(exception);

		// MAUI's own LayoutHandler takes the same factory and has no cast, so the test proves nothing unless the layout keeps the toolkit's handler
		Assert.IsType<ExpanderLayoutHandler>(layoutHandler);
		Assert.Contains(Assert.IsType<LayoutPanel>(layoutPlatformView), platformViewsOfTheFactory);
		Assert.True(expander.IsExpanded);
	}

	[Fact]
	public async Task UIAutomationExposesTheHeaderAsAKeyboardFocusableButtonNamedAfterTheHeader()
	{
		SetupBuilder();
		var expander = CreateExpander();

		await AttachAndRun(expander, async _ =>
		{
			await expander.OnLoadedAsync();
			var panel = GetLayoutPanel(expander);
			var peer = GetAutomationPeer(expander);

			Assert.True(panel.IsTabStop, "Tab does not stop at the header.");
			Assert.True(peer.IsKeyboardFocusable());
			Assert.True(peer.IsEnabled());
			Assert.True(peer.IsControlElement(), "The Expander is missing from the control view, which Narrator reads.");
			Assert.True(peer.IsContentElement());
			Assert.Equal(AutomationControlType.Button, peer.GetAutomationControlType());
			Assert.Equal(nameof(Expander), peer.GetClassName());
			Assert.Equal("Header", peer.GetName());
			Assert.Equal(ExpandCollapseState.Collapsed, GetExpandCollapseProvider(peer).ExpandCollapseState);
			GetInvokeProvider(peer);

			Assert.True(panel.Focus(WFocusState.Keyboard), "The header does not take the keyboard focus.");
			await AssertionExtensions.AssertEventually(peer.HasKeyboardFocus, message: "UI Automation does not report the keyboard focus on the header.");
		});
	}

	[Fact]
	public async Task UIAutomationExpandsCollapsesAndInvokesTheHeaderAndItsStateFollowsIsExpanded()
	{
		SetupBuilder();
		var expander = CreateExpander();
		List<bool> expandedChanges = [];
		List<object?> commandParameters = [];
		expander.ExpandedChanged += (_, e) => expandedChanges.Add(e.IsExpanded);
		expander.Command = new Command(parameter => commandParameters.Add(parameter));
		expander.CommandParameter = nameof(Expander.CommandParameter);

		await AttachAndRun(expander, async _ =>
		{
			await expander.OnLoadedAsync();
			var expandCollapse = GetExpandCollapseProvider(GetAutomationPeer(expander));
			var invoke = GetInvokeProvider(GetAutomationPeer(expander));

			expandCollapse.Expand();

			Assert.True(expander.IsExpanded);
			Assert.Equal(ExpandCollapseState.Expanded, expandCollapse.ExpandCollapseState);
			await AssertionExtensions.AssertEventually(() => IsPlatformViewVisible(expander.Content), message: "Expand did not show the content.");

			expandCollapse.Expand();
			Assert.True(expander.IsExpanded);

			expandCollapse.Collapse();
			Assert.False(expander.IsExpanded);
			Assert.Equal(ExpandCollapseState.Collapsed, expandCollapse.ExpandCollapseState);

			expandCollapse.Collapse();
			Assert.False(expander.IsExpanded);

			invoke.Invoke();
			Assert.True(expander.IsExpanded);

			invoke.Invoke();
			Assert.False(expander.IsExpanded);

			Assert.Equal<bool>([true, false, true, false], expandedChanges);
			Assert.Equal<object?>([.. Enumerable.Repeat(nameof(Expander.CommandParameter), 4)], commandParameters);

			expander.IsExpanded = true;
			Assert.Equal(ExpandCollapseState.Expanded, expandCollapse.ExpandCollapseState);

			expander.IsExpanded = false;
			Assert.Equal(ExpandCollapseState.Collapsed, expandCollapse.ExpandCollapseState);
		});
	}

	// WinUI cannot construct KeyRoutedEventArgs and the test app cannot inject keys (InputInjector needs the restricted inputInjectionBrokered
	// capability), so the test drives HandleKey, which the KeyDown and KeyUp handlers of the focused layout call.
	[Fact]
	public async Task EnterAndSpaceToggleTheHeaderLikeTheyClickAButton()
	{
		SetupBuilder();
		var expander = CreateExpander();

		await AttachAndRun(expander, async _ =>
		{
			await expander.OnLoadedAsync();
			var panel = GetLayoutPanel(expander);

			Assert.True(panel.HandleKey(VirtualKey.Enter, isKeyDown: true, isRepeat: false));
			Assert.True(panel.HandleKey(VirtualKey.Enter, isKeyDown: true, isRepeat: true));
			Assert.False(expander.IsExpanded);

			Assert.True(panel.HandleKey(VirtualKey.Enter, isKeyDown: false, isRepeat: false));
			Assert.True(expander.IsExpanded);

			Assert.True(panel.HandleKey(VirtualKey.Space, isKeyDown: true, isRepeat: false));
			Assert.True(expander.IsExpanded);

			Assert.True(panel.HandleKey(VirtualKey.Space, isKeyDown: false, isRepeat: false));
			Assert.False(expander.IsExpanded);

			Assert.False(panel.HandleKey(VirtualKey.Space, isKeyDown: false, isRepeat: false));
			Assert.False(panel.HandleKey(VirtualKey.A, isKeyDown: true, isRepeat: false));
			Assert.False(expander.IsExpanded);
		});
	}

	[Fact]
	public async Task ASpacePressIsDroppedWhenTheFocusLeavesTheHeader()
	{
		SetupBuilder();
		var previousControl = new Button { Text = "Previous" };
		var expander = CreateExpander();
		var layout = new VerticalStackLayout { previousControl, expander };

		await AttachAndRun(layout, async _ =>
		{
			await layout.OnLoadedAsync();
			var panel = GetLayoutPanel(expander);

			Assert.True(panel.Focus(WFocusState.Keyboard), "The header does not take the keyboard focus.");
			Assert.True(panel.HandleKey(VirtualKey.Space, isKeyDown: true, isRepeat: false));

			// WinUI raises LostFocus asynchronously, and the handler of the panel, subscribed in its constructor, runs before this one.
			var lostFocus = new TaskCompletionSource();
			panel.LostFocus += (_, _) => lostFocus.TrySetResult();
			Assert.True(Assert.IsType<WControl>(previousControl.Handler?.PlatformView, exactMatch: false).Focus(WFocusState.Keyboard), "The Button does not take the keyboard focus.");
			await lostFocus.Task.WaitAsync(windowsLayoutTimeout);

			Assert.True(panel.Focus(WFocusState.Keyboard), "The header does not take the keyboard focus again.");
			Assert.False(panel.HandleKey(VirtualKey.Space, isKeyDown: false, isRepeat: false));
			Assert.False(expander.IsExpanded);
		});
	}

	// Like a Button, only one press at a time toggles, and any other key pressed meanwhile cancels it.
	[Fact]
	public async Task AnotherKeyDropsAPressAndTwoPressKeysToggleOnce()
	{
		SetupBuilder();
		var expander = CreateExpander();

		await AttachAndRun(expander, async _ =>
		{
			await expander.OnLoadedAsync();
			var panel = GetLayoutPanel(expander);

			Assert.True(panel.HandleKey(VirtualKey.Space, isKeyDown: true, isRepeat: false));
			Assert.False(panel.HandleKey(VirtualKey.Escape, isKeyDown: true, isRepeat: false));
			Assert.False(panel.HandleKey(VirtualKey.Space, isKeyDown: false, isRepeat: false));
			Assert.False(expander.IsExpanded);

			Assert.True(panel.HandleKey(VirtualKey.Space, isKeyDown: true, isRepeat: false));
			Assert.True(panel.HandleKey(VirtualKey.Enter, isKeyDown: true, isRepeat: false));
			Assert.True(panel.HandleKey(VirtualKey.Space, isKeyDown: false, isRepeat: false));
			Assert.False(panel.HandleKey(VirtualKey.Enter, isKeyDown: false, isRepeat: false));
			Assert.True(expander.IsExpanded);

			// Unlike a Button, a key already held when the header got the focus (a repeat) does not press it.
			Assert.True(panel.HandleKey(VirtualKey.Space, isKeyDown: true, isRepeat: true));
			Assert.False(panel.HandleKey(VirtualKey.Space, isKeyDown: false, isRepeat: false));
			Assert.True(expander.IsExpanded);
		});
	}

	[Fact]
	public async Task TabMovesTheFocusFromThePreviousControlToTheHeader()
	{
		SetupBuilder();
		var previousControl = new Button { Text = "Previous" };
		var expander = CreateExpander();
		var layout = new VerticalStackLayout { previousControl, expander };

		await AttachAndRun(layout, async _ =>
		{
			await layout.OnLoadedAsync();
			var panel = GetLayoutPanel(expander);
			var xamlRoot = panel.XamlRoot ?? throw new InvalidOperationException("The Expander is not in a XAML tree.");

			Assert.True(Assert.IsType<WControl>(previousControl.Handler?.PlatformView, exactMatch: false).Focus(WFocusState.Keyboard), "The Button does not take the keyboard focus.");
			Assert.True(FocusManager.TryMoveFocus(FocusNavigationDirection.Next, new FindNextElementOptions { SearchRoot = xamlRoot.Content }), "Tab finds no element after the Button.");

			await AssertionExtensions.AssertEventually(() => ReferenceEquals(FocusManager.GetFocusedElement(xamlRoot), panel), message: "Tab does not move the focus from the Button to the header.");
		});
	}

	[Theory]
	[InlineData(ExpandDirection.Down)]
	[InlineData(ExpandDirection.Up)]
	public async Task TheFocusVisualFramesOnlyTheRowOfTheHeader(ExpandDirection direction)
	{
		SetupBuilder();
		var expander = CreateExpander(direction);

		await AttachAndRun(expander, async _ =>
		{
			await expander.OnLoadedAsync();
			var panel = GetLayoutPanel(expander);
			await AssertionExtensions.AssertEventually(() => IsAbout(panel.FocusVisualMargin, new WThickness(0)), message: $"The focus visual of the collapsed Expander has the margin {panel.FocusVisualMargin} instead of none.");

			expander.IsExpanded = true;

			var expectedMargin = direction is ExpandDirection.Down ? new WThickness(0, 0, 0, contentHeight) : new WThickness(0, contentHeight, 0, 0);
			await AssertionExtensions.AssertEventually(() => IsAbout(panel.FocusVisualMargin, expectedMargin), timeout: 5000, message: $"The focus visual of the expanded Expander does not frame only the header: its margin is {panel.FocusVisualMargin} instead of {expectedMargin}.");
		});

		static bool IsAbout(WThickness actual, WThickness expected) =>
			Math.Abs(actual.Left - expected.Left) < 1 && Math.Abs(actual.Top - expected.Top) < 1 && Math.Abs(actual.Right - expected.Right) < 1 && Math.Abs(actual.Bottom - expected.Bottom) < 1;
	}

	[Theory]
	[InlineData(nameof(Expander.IsEnabled))]
	[InlineData(nameof(Expander.Header) + nameof(VisualElement.IsEnabled))]
	[InlineData(nameof(Expander.Header) + nameof(VisualElement.IsVisible))]
	[InlineData(nameof(Expander.Header) + nameof(VisualElement.InputTransparent))]
	public async Task AHeaderThatATapCannotToggleIsNoTabStopAndUIAutomationCannotToggleIt(string blockingProperty)
	{
		SetupBuilder();
		var expander = CreateExpander();

		await AttachAndRun(expander, async _ =>
		{
			await expander.OnLoadedAsync();
			var panel = GetLayoutPanel(expander);
			var peer = GetAutomationPeer(expander);
			Assert.True(panel.IsTabStop);

			SetHeaderActivationBlocked(expander, blockingProperty, isBlocked: true);

			Assert.False(panel.IsTabStop, "Tab stops at a header that a tap cannot toggle.");
			Assert.False(peer.IsEnabled());
			Assert.Throws<ElementNotEnabledException>(() => GetInvokeProvider(peer).Invoke());
			Assert.Throws<ElementNotEnabledException>(() => GetExpandCollapseProvider(peer).Expand());
			panel.HandleKey(VirtualKey.Enter, isKeyDown: true, isRepeat: false);
			panel.HandleKey(VirtualKey.Enter, isKeyDown: false, isRepeat: false);
			Assert.False(expander.IsExpanded);

			SetHeaderActivationBlocked(expander, blockingProperty, isBlocked: false);

			Assert.True(panel.IsTabStop);
			Assert.True(peer.IsEnabled());
		});
	}

	[Fact]
	public async Task UIAutomationNamesTheExpanderAfterWhatALayoutHeaderHolds()
	{
		SetupBuilder();
		var expander = new Expander
		{
			Header = new HorizontalStackLayout
			{
				new BoxView { Color = Colors.Blue, WidthRequest = 20, HeightRequest = 20 },
				new Label { Text = "Brandon" },
				new Label { Text = "Minnick" }
			},
			Content = new Label { Text = "Content" }
		};

		await AttachAndRun(expander, async _ =>
		{
			await expander.OnLoadedAsync();

			Assert.Equal("Brandon, Minnick", GetAutomationPeer(expander).GetName());
		});
	}

	[Fact]
	public async Task UIAutomationNamesTheExpanderAfterTheSemanticDescriptionOfTheHeader()
	{
		SetupBuilder();
		var header = new Label { Text = "Header" };
		SemanticProperties.SetDescription(header, "Described header");
		var expander = new Expander { Header = header, Content = new Label { Text = "Content" } };

		await AttachAndRun(expander, async _ =>
		{
			await expander.OnLoadedAsync();

			Assert.Equal("Described header", GetAutomationPeer(expander).GetName());
		});
	}

	[Fact]
	public async Task UIAutomationReadsTheSemanticHintOfTheHeaderAsTheHelpTextOfTheExpander()
	{
		SetupBuilder();
		var header = new Label { Text = "Header" };
		SemanticProperties.SetHint(header, "Shows the content");
		var expander = new Expander { Header = header, Content = new Label { Text = "Content" } };

		await AttachAndRun(expander, async _ =>
		{
			await expander.OnLoadedAsync();

			Assert.Equal("Shows the content", GetAutomationPeer(expander).GetHelpText());
		});
	}

	[Fact]
	public async Task KeyboardAndUIAutomationActivationsResizeTheItemContainerOfTheirExpanderInACollectionView()
	{
		SetupBuilder();
		var (collectionView, expanders) = CreateCollectionView(new LinearItemsLayout(ItemsLayoutOrientation.Vertical), itemCount: 3);

		await AttachAndRun(collectionView, async handler =>
		{
			var native = Assert.IsType<WListView>(handler.PlatformView, exactMatch: false);
			await WaitForRealizedItemsAsync(native, 3);
			var heightsBefore = GetItemContainerHeights(native, 3);
			var expander = GetExpanderOfItem(expanders, 1);

			Assert.True(GetLayoutPanel(expander).HandleKey(VirtualKey.Enter, isKeyDown: true, isRepeat: false));
			Assert.True(GetLayoutPanel(expander).HandleKey(VirtualKey.Enter, isKeyDown: false, isRepeat: false));

			Assert.True(expander.IsExpanded);
			var expandedHeight = expander.Measure(double.PositiveInfinity, double.PositiveInfinity).Height;
			Assert.True(expandedHeight >= headerHeight + contentHeight - 1, $"The expanded Expander should measure at least header + content but measured {expandedHeight}.");
			Assert.Equal(expandedHeight, GetItemContainer(native, 1).Height, precision: 1);
			Assert.Equal(heightsBefore[0], GetItemContainer(native, 0).Height);
			Assert.Equal(heightsBefore[2], GetItemContainer(native, 2).Height);

			GetInvokeProvider(GetAutomationPeer(expander)).Invoke();

			Assert.False(expander.IsExpanded);
			Assert.Equal(headerHeight, GetItemContainer(native, 1).Height, precision: 1);
		});
	}

	[Fact]
	public async Task TheExpanderIsCollectedAfterUIAutomationToggledIt()
	{
		SetupBuilder();
		var expanderReference = new WeakReference(null);
		var panelReference = new WeakReference(null);

		await InvokeOnMainThreadAsync(() =>
		{
			var expander = CreateExpander();
			var handler = CreateHandler<ContentViewHandler>(expander);
			GetInvokeProvider(GetAutomationPeer(expander)).Invoke();
			Assert.True(expander.IsExpanded);

			expanderReference.Target = expander;
			panelReference.Target = GetLayoutPanel(expander);

			((IElementHandler)handler).DisconnectHandler();
			expander.Handler = null;
		});

		await AssertionExtensions.WaitForGC(expanderReference, panelReference);
	}

	// A plain Grid as platform view reaches the branches for a platform view that is neither a ListView nor a GridView.
	sealed class NonListCollectionViewHandler : ViewHandler<CollectionView, WGrid>
	{
		public NonListCollectionViewHandler() : base(ViewMapper)
		{
		}

		protected override WGrid CreatePlatformView() => new();
	}

	// MAUI only provides a pointer position for a real pointer tap.
	sealed class PositionedTappedEventArgs(Point position) : TappedEventArgs(null)
	{
		public override Point? GetPosition(Element? relativeTo) => position;
	}

	static (CollectionView CollectionView, List<Expander> Expanders) CreateCollectionView(IItemsLayout itemsLayout, int itemCount, ExpandDirection direction = ExpandDirection.Down)
	{
		var expanders = new List<Expander>();
		var collectionView = new CollectionView
		{
			WidthRequest = collectionViewWidth,
			HeightRequest = collectionViewHeight,
			ItemsLayout = itemsLayout,
			ItemsSource = Enumerable.Range(0, itemCount).Select(index => $"Item {index}").ToList(),
			ItemTemplate = new DataTemplate(() =>
			{
				var expander = new Expander
				{
					Direction = direction,
					Header = new Label { Text = "Header", HeightRequest = headerHeight },
					Content = new BoxView { Color = Colors.Red, HeightRequest = contentHeight }
				};
				expanders.Add(expander);
				return expander;
			})
		};

		return (collectionView, expanders);
	}

	// The template also runs while MAUI measures, so not every Expander it created is on screen: the realized one has the item as its
	// binding context and a handler.
	static Expander GetExpanderOfItem(List<Expander> expanders, int index) =>
		expanders.Last(expander => expander.Handler is not null && Equals(expander.BindingContext, $"Item {index}"));

	static Task WaitForRealizedItemsAsync(Microsoft.UI.Xaml.Controls.ListViewBase native, int count) =>
		AssertionExtensions.AssertEventually(
			() => Enumerable.Range(0, count).All(index => native.ContainerFromIndex(index) is WFrameworkElement { ActualHeight: > 0 }),
			timeout: 5000,
			message: $"The CollectionView never realized its first {count} items.");

	static WListViewItem GetItemContainer(WListView native, int index) => Assert.IsType<WListViewItem>(native.ContainerFromIndex(index), exactMatch: false);

	// Height is the property the Expander writes: it is NaN until it wrote one, so compare with Equals.
	static double[] GetItemContainerHeights(WListView native, int count) => [.. Enumerable.Range(0, count).Select(index => GetItemContainer(native, index).Height)];

	static Task WaitForContainerHeightAsync(WListView native, int index, double atLeast) =>
		AssertionExtensions.AssertEventually(
			() => native.ContainerFromIndex(index) is WFrameworkElement container && container.ActualHeight >= atLeast - 1,
			timeout: 5000,
			message: $"The container of item {index} never grew to {atLeast}.");

	static WGridViewItem GetGridItemContainer(WGridView native, int index) => Assert.IsType<WGridViewItem>(native.ContainerFromIndex(index), exactMatch: false);

	// The Expander compares the tap with coordinates in the platform list.
	static Point CenterOf(WFrameworkElement container, WFrameworkElement list)
	{
		var topLeft = container.TransformToVisual(list).TransformPoint(new WPoint(0, 0));
		return new Point(topLeft.X + container.ActualWidth / 2, topLeft.Y + container.ActualHeight / 2);
	}
}