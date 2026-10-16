#pragma warning disable CS0618 // ListView and ViewCell are obsolete, but Expander still supports them in a ListView
using System.Reflection;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Handlers;
using WFrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using WGrid = Microsoft.UI.Xaml.Controls.Grid;
using WGridView = Microsoft.UI.Xaml.Controls.GridView;
using WGridViewItem = Microsoft.UI.Xaml.Controls.GridViewItem;
using WListView = Microsoft.UI.Xaml.Controls.ListView;
using WListViewItem = Microsoft.UI.Xaml.Controls.ListViewItem;
using WPoint = Windows.Foundation.Point;
using WVisibility = Microsoft.UI.Xaml.Visibility;

namespace CommunityToolkit.Maui.DeviceTests;

// Windows: inside a ListView the header tap asks the Cell to update its size, inside a CollectionView it sets the Height of the
// realized item containers (`#if WINDOWS` in Expander.ResizeExpanderInItemsView and Expander.windows.cs). The containers' Height
// property is what the toolkit writes, so it is read back from the native ListViewItem / GridViewItem; positions come from a
// TappedEventArgs whose GetPosition is overridden (MAUI only fills it from a real pointer tap).
public partial class ExpanderTests
{
	const double collectionViewWidth = 400;
	const double collectionViewHeight = 400;

	static readonly TimeSpan windowsLayoutTimeout = TimeSpan.FromSeconds(5);

	// ---- accessors for the shared tests

	static bool IsPlatformViewVisible(IView view) => ((WFrameworkElement)view.Handler!.PlatformView!).Visibility == WVisibility.Visible;

	// The row's size in device-independent units (the unit MAUI uses). 0 while the row is not realized.
	static double GetRowHeight(ListView listView, int row) => GetListViewItem(listView, row)?.ActualHeight ?? 0;

	static double GetRowTop(ListView listView, int row) =>
		GetListViewItem(listView, row) is WListViewItem container ? GetTop(container, GetNativeListView(listView)) : 0;

	static WListView GetNativeListView(ListView listView) => Assert.IsType<WListView>(listView.Handler!.PlatformView, exactMatch: false);

	static WListViewItem? GetListViewItem(ListView listView, int row) => GetNativeListView(listView).ContainerFromIndex(row) as WListViewItem;

	static double GetTop(WFrameworkElement element, WFrameworkElement relativeTo) =>
		element.TransformToVisual(relativeTo).TransformPoint(new WPoint(0, 0)).Y;

	// ---- ListView: Cell.ForceUpdateSize

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

	// ---- CollectionView with a vertical list layout (Expander.ForceUpdateCellSize, PlatformListView branch)

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

			// A TappedEventArgs created in code has no pointer position.
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

			// ContainerFromItem is null for the virtualized items: the loop skips them instead of failing.
			var exception = Record.Exception(() => expander.HandleHeaderTap(new PositionedTappedEventArgs(new Point(10, tapY))));

			Assert.Null(exception);
			Assert.True(GetItemContainer(native, 0).Height >= headerHeight + contentHeight - 1);
		});
	}

	// ---- the container of the tapped item in a horizontal list, after a collapse and in a scrolled list
	//
	// The ListView branch of ForceUpdateCellSize used to compare the Y of the tap with offsets that it summed from the heights of the realized
	// item containers (moving the offset by the new height of a container it resized), which resized the wrong container in a horizontal list,
	// after a collapse whose tap lies below the new height of the container, and in a scrolled list. It now compares the tap with the bounds
	// of each container where the list shows it, like the GridView branch.

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

	// Items 0 and 1 are expanded, then the header of item 0 is tapped to collapse it. With ExpandDirection.Up the header is the bottom of
	// the container, so the tap lies below the collapsed height and also inside the range the loop computes for container 1 after the
	// resize; with Down that happens only for a tap on the bottom edge of the header (the range test includes both ends). The tap in the
	// middle of a Down header is the control row.
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

			// Expand both, each with a tap in the middle of its container, and let the list lay out the new heights.
			first.HandleHeaderTap(new PositionedTappedEventArgs(new Point(10, GetTop(GetItemContainer(native, 0), native) + GetItemContainer(native, 0).ActualHeight / 2)));
			await WaitForContainerHeightAsync(native, 0, headerHeight + contentHeight);
			second.HandleHeaderTap(new PositionedTappedEventArgs(new Point(10, GetTop(GetItemContainer(native, 1), native) + GetItemContainer(native, 1).ActualHeight / 2)));
			await WaitForContainerHeightAsync(native, 1, headerHeight + contentHeight);
			Assert.True(first.IsExpanded);
			Assert.True(second.IsExpanded);
			var heightsBefore = GetItemContainerHeights(native, 3);

			// The tap on the header of item 0, which collapses it.
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

	// ---- CollectionView with a grid layout (Expander.ForceUpdateCellSize, PlatformGridView branch, IterateItemsInRow)

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

			// Item 3 is the second item of the second row: the row is items 2 and 3.
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

			// The last row only holds item 2, while the span is 2: IterateItemsInRow stops at the end of the items.
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

	// ---- CollectionView whose platform view is no list (Expander.ForceUpdateCellSize, neither branch)

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

	// ---- helpers

	// A CollectionView handler whose platform view is a plain Grid, to reach the branches for a platform view that is neither a ListView nor a GridView.
	sealed class NonListCollectionViewHandler : ViewHandler<CollectionView, WGrid>
	{
		public NonListCollectionViewHandler() : base(ViewMapper)
		{
		}

		protected override WGrid CreatePlatformView() => new();
	}

	// A TappedEventArgs with a pointer position, which MAUI only provides for a real pointer tap.
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

	// The Expander the CollectionView realized for the item (the template also runs while MAUI measures, so not every Expander it
	// created is on screen): the realized one has the item as its binding context and a handler.
	static Expander GetExpanderOfItem(List<Expander> expanders, int index) =>
		expanders.Last(expander => expander.Handler is not null && Equals(expander.BindingContext, $"Item {index}"));

	static Task WaitForRealizedItemsAsync(Microsoft.UI.Xaml.Controls.ListViewBase native, int count) =>
		AssertionExtensions.AssertEventually(
			() => Enumerable.Range(0, count).All(index => native.ContainerFromIndex(index) is WFrameworkElement { ActualHeight: > 0 }),
			timeout: 5000,
			message: $"The CollectionView never realized its first {count} items.");

	static WListViewItem GetItemContainer(WListView native, int index) => Assert.IsType<WListViewItem>(native.ContainerFromIndex(index), exactMatch: false);

	// The Height property of the first item containers: what the Expander writes (NaN until it wrote one; compare with Equals).
	static double[] GetItemContainerHeights(WListView native, int count) => [.. Enumerable.Range(0, count).Select(index => GetItemContainer(native, index).Height)];

	static Task WaitForContainerHeightAsync(WListView native, int index, double atLeast) =>
		AssertionExtensions.AssertEventually(
			() => native.ContainerFromIndex(index) is WFrameworkElement container && container.ActualHeight >= atLeast - 1,
			timeout: 5000,
			message: $"The container of item {index} never grew to {atLeast}.");

	static WGridViewItem GetGridItemContainer(WGridView native, int index) => Assert.IsType<WGridViewItem>(native.ContainerFromIndex(index), exactMatch: false);

	// The center of the container, in the coordinates of the platform list: the coordinates the Expander compares the tap with.
	static Point CenterOf(WFrameworkElement container, WFrameworkElement list)
	{
		var topLeft = container.TransformToVisual(list).TransformPoint(new WPoint(0, 0));
		return new Point(topLeft.X + container.ActualWidth / 2, topLeft.Y + container.ActualHeight / 2);
	}
}