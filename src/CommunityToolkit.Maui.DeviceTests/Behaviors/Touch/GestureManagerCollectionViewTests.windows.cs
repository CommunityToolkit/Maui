using ListViewBase = Microsoft.UI.Xaml.Controls.ListViewBase;

namespace CommunityToolkit.Maui.DeviceTests;

// Windows: the behavior's pointer handlers capture the pointer and handle the press and the release, so the ListViewItem never selects the
// item for a tap on the behavior's view. The toolkit selects it, and MAUI shows that selection in the platform list.
public partial class GestureManagerCollectionViewTests
{
	// A tap selects the item in the platform list too: what a person sees, and what UI Automation reports for the item.
	[Theory]
	[InlineData(SelectionMode.Single)]
	[InlineData(SelectionMode.Multiple)]
	public async Task TappedItemIsSelectedInThePlatformList(SelectionMode selectionMode)
	{
		SetupBuilder();

		var (collectionView, labels, _) = CreateCollectionView(selectionMode, nested: false);

		await RunLoadedAsync(collectionView, async _ =>
		{
			var first = await WaitForAttachedLabelAsync(labels, 0);
			var second = await WaitForAttachedLabelAsync(labels, 1);
			var listView = Assert.IsType<ListViewBase>(collectionView.Handler?.PlatformView, exactMatch: false);

			Tap(first);
			await WaitUntilAsync(() => GetSelectedIndexes(listView) is [0], "The tap did not select the first item in the platform list.");

			Tap(second);
			int[] expected = selectionMode is SelectionMode.Single ? [1] : [0, 1];
			await WaitUntilAsync(() => GetSelectedIndexes(listView).SequenceEqual(expected), $"After the second tap the platform list does not have exactly the items [{string.Join(", ", expected)}] selected.");

			if (selectionMode is SelectionMode.Multiple)
			{
				Tap(first);
				await WaitUntilAsync(() => GetSelectedIndexes(listView) is [1], "A tap on a selected item did not unselect it in the platform list.");
			}
		});

		static int[] GetSelectedIndexes(ListViewBase listView) => [.. listView.SelectedItems.Select(item => listView.Items.IndexOf(item)).Order()];
	}
}
