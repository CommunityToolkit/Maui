#pragma warning disable CS0618 // ListView and ViewCell are obsolete, but Expander still supports them in a ListView
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.DeviceTests;

// Device tests of Expander. The platform work is in the header tap handler (Expander.shared.cs `ResizeExpanderInItemsView`):
// inside a ListView it makes the platform list recompute the row (iOS and Mac Catalyst reload the UITableView, Windows asks the
// Cell to update its size, Android needs nothing), inside a CollectionView on Windows it resizes the realized item containers
// (Expander.windows.cs). The shared tests run on every platform and read the native state: the platform visibility of the
// content and the height of the row the platform list gives the Expander. The platform files hold the accessors and the
// tests of the platform-only branches.
[Category(TestCategory.Expander)]
[Collection(TestCollections.DisableParallelization)]
public partial class ExpanderTests : CommunityToolkitHandlerTestBase
{
	const double headerHeight = 30;
	const double contentHeight = 120;

	const string listViewRowsDoNotResizeOnWindows = "MAUI's ListView on Windows ignores Cell.ForceUpdateSize (no ForceUpdateSizeRequested handler in its CellControl and ListViewRenderer, MAUI 10.0.90), so a row keeps the height it was first measured with. The toolkit's part, asking the tapped Expander's cell to update its size, is tested by HeaderTapInsideAListViewAsksTheCellOfTheTappedExpanderToUpdateItsSize.";

	[Fact]
	public async Task ContentIsHiddenOnThePlatformUntilTheHeaderIsTapped()
	{
		SetupBuilder();
		var header = new Label { Text = "Header" };
		var content = new Label { Text = "Content" };
		var expander = new Expander { Header = header, Content = content };

		await AttachAndRun(expander, async _ =>
		{
			await expander.OnLoadedAsync();

			Assert.False(expander.IsExpanded);
			Assert.True(IsPlatformViewVisible(header), "The header is always visible.");
			Assert.False(IsPlatformViewVisible(content), "The content of a collapsed expander is hidden on the platform.");

			expander.HandleHeaderTap(new TappedEventArgs(null));

			Assert.True(expander.IsExpanded);
			await AssertionExtensions.AssertEventually(() => IsPlatformViewVisible(content), message: "The content should become visible when the header is tapped.");
			Assert.True(IsPlatformViewVisible(header));

			expander.HandleHeaderTap(new TappedEventArgs(null));

			Assert.False(expander.IsExpanded);
			await AssertionExtensions.AssertEventually(() => !IsPlatformViewVisible(content), message: "The content should be hidden again when the header is tapped a second time.");
		});
	}

	[Fact]
	public async Task HeaderTapInsideAListViewGrowsAndShrinksTheRow()
	{
		Skip.If(OperatingSystem.IsWindows(), listViewRowsDoNotResizeOnWindows);
		SetupBuilder();
		var listView = CreateListView(itemCount: 3);

		await AttachAndRun(new ContentPage { Content = listView }, async _ =>
		{
			await AssertionExtensions.AssertEventually(() => GetRowHeight(listView, 0) > 0, timeout: 5000, message: "The ListView never realized its first row.");
			var collapsedHeight = GetRowHeight(listView, 0);
			var secondRowTop = GetRowTop(listView, 1);

			GetExpander(listView, 0).HandleHeaderTap(new TappedEventArgs(null));

			// The row grows by the height of the content, and the next row moves down with it.
			await AssertionExtensions.AssertEventually(() => GetRowHeight(listView, 0) >= collapsedHeight + contentHeight - 1, timeout: 5000,
				message: $"The row should have grown by {contentHeight} from {collapsedHeight} but is {GetRowHeight(listView, 0)}.");
			Assert.True(GetRowTop(listView, 1) >= secondRowTop + contentHeight - 1, "The second row should have moved down with the first row's growth.");

			GetExpander(listView, 0).HandleHeaderTap(new TappedEventArgs(null));

			await AssertionExtensions.AssertEventually(() => Math.Abs(GetRowHeight(listView, 0) - collapsedHeight) < 1, timeout: 5000,
				message: $"The row should be back at {collapsedHeight} but is {GetRowHeight(listView, 0)}.");
			Assert.True(Math.Abs(GetRowTop(listView, 1) - secondRowTop) < 1, "The second row should be back where it was.");
		});
	}

	[Fact]
	public async Task HeaderTapOnlyResizesTheRowOfTheTappedExpander()
	{
		Skip.If(OperatingSystem.IsWindows(), listViewRowsDoNotResizeOnWindows);
		SetupBuilder();
		var listView = CreateListView(itemCount: 3);

		await AttachAndRun(new ContentPage { Content = listView }, async _ =>
		{
			await AssertionExtensions.AssertEventually(() => GetRowHeight(listView, 2) > 0, timeout: 5000, message: "The ListView never realized its rows.");
			var firstRowHeight = GetRowHeight(listView, 0);
			var secondRowHeight = GetRowHeight(listView, 1);
			var thirdRowHeight = GetRowHeight(listView, 2);

			GetExpander(listView, 1).HandleHeaderTap(new TappedEventArgs(null));

			await AssertionExtensions.AssertEventually(() => GetRowHeight(listView, 1) >= secondRowHeight + contentHeight - 1, timeout: 5000, message: "The tapped row should have grown.");
			Assert.True(Math.Abs(GetRowHeight(listView, 0) - firstRowHeight) < 1, "The row above must not change.");
			Assert.True(Math.Abs(GetRowHeight(listView, 2) - thirdRowHeight) < 1, "The row below must not change.");
		});
	}

	[Fact]
	public async Task HeaderTapInsideAListViewWithoutAHandlerTogglesAndDoesNotThrow()
	{
		SetupBuilder();
		var listView = CreateListView(itemCount: 1);

		// The ListView has no handler, so there is no platform list to refresh. The cell is realized without one.
		var expander = GetExpander(listView, 0);
		Assert.Null(listView.Handler);
		Assert.Same(listView, expander.Parent!.Parent);

		var exception = await Record.ExceptionAsync(() => InvokeOnMainThreadAsync(() => expander.HandleHeaderTap(new TappedEventArgs(null))));

		Assert.Null(exception);
		Assert.True(expander.IsExpanded);
	}

	[Fact]
	public async Task HeaderTapOfAnExpanderThatIsAChildOfAListViewButNotOfACellTogglesAndDoesNotThrow()
	{
		SetupBuilder();
		var listView = new ListView();
		var expander = new Expander { Header = new Label { Text = "Header" }, Content = new Label { Text = "Content" } };

		// A ListView is the parent, but the Expander is not a Cell, so there is no row to update.
		expander.Parent = listView;

		var exception = await Record.ExceptionAsync(() => InvokeOnMainThreadAsync(() => expander.HandleHeaderTap(new TappedEventArgs(null))));

		Assert.Null(exception);
		Assert.True(expander.IsExpanded);
	}

	[Fact]
	public async Task HeaderTapOutsideAListViewTogglesAndDoesNotThrow()
	{
		SetupBuilder();
		var expander = new Expander { Header = new Label { Text = "Header" }, Content = new Label { Text = "Content" } };
		var layout = new VerticalStackLayout { Children = { new Label { Text = "Before" }, expander, new Label { Text = "After" } } };

		await AttachAndRun(layout, async _ =>
		{
			await layout.OnLoadedAsync();

			var exception = Record.Exception(() => expander.HandleHeaderTap(new TappedEventArgs(null)));

			Assert.Null(exception);
			Assert.True(expander.IsExpanded);
		});
	}

	[Fact]
	public async Task HeaderTapWithoutAHeaderTogglesAndDoesNotThrow()
	{
		SetupBuilder();
		var expander = new Expander { Content = new Label { Text = "Content" } };

		await AttachAndRun(expander, async _ =>
		{
			await expander.OnLoadedAsync();
			Assert.Null(expander.Header);

			// ResizeExpanderInItemsView returns before it walks the parents. On Windows it measures the Expander after a tap that expands
			// and the header after a tap that collapses, so only the second tap would reach the missing header.
			var exception = Record.Exception(() =>
			{
				expander.HandleHeaderTap(new TappedEventArgs(null));
				Assert.True(expander.IsExpanded);
				expander.HandleHeaderTap(new TappedEventArgs(null));
			});

			Assert.Null(exception);
			Assert.False(expander.IsExpanded);
		});
	}

	[Fact]
	public async Task ExpanderHandlerAndPlatformViewAreCollectedAfterTheHandlerIsDisconnected()
	{
		SetupBuilder();
		WeakReference expanderReference = null!, handlerReference = null!, platformViewReference = null!, headerReference = null!;

		await InvokeOnMainThreadAsync(() =>
		{
			var header = new Label { Text = "Header" };
			var expander = new Expander { Header = header, Content = new Label { Text = "Content" } };
			var handler = CreateHandler<ContentViewHandler>(expander);
			expander.HandleHeaderTap(new TappedEventArgs(null));
			expanderReference = new WeakReference(expander);
			headerReference = new WeakReference(header);
			handlerReference = new WeakReference(handler);
			platformViewReference = new WeakReference(handler.PlatformView);

			((IElementHandler)handler).DisconnectHandler();
			expander.Handler = null;
		});

		await AssertionExtensions.WaitForGC(expanderReference, headerReference, handlerReference, platformViewReference);
	}

	// The Expander the ListView realized for the row (the template can also run for row measurement, so not every Expander the
	// template created is on screen).
	static Expander GetExpander(ListView listView, int row) =>
		Assert.IsType<Expander>(Assert.IsType<ViewCell>(((ITemplatedItemsView<Cell>)listView).TemplatedItems[row]).View);

	static ListView CreateListView(int itemCount) => new()
	{
		HasUnevenRows = true,
		ItemsSource = Enumerable.Range(0, itemCount).Select(index => $"Item {index}").ToList(),
		ItemTemplate = new DataTemplate(() => new ViewCell
		{
			View = new Expander
			{
				Header = new Label { Text = "Header", HeightRequest = headerHeight },
				Content = new BoxView { Color = Colors.Red, HeightRequest = contentHeight }
			}
		})
	};
}