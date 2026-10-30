using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class GestureManagerCollectionViewTests
{
	// The behavior's recognizer never recognizes, so it never takes a touch away from the views: a recognizer that recognized the tap would
	// cancel the touches of the views, as a UITapGestureRecognizer does, and the collection view would never select the item.
	[Theory]
	[InlineData(SelectionMode.Single)]
	[InlineData(SelectionMode.Multiple)]
	public async Task TapThroughUIKitSelectsTheItemAndCompletesTheGesture(SelectionMode selectionMode)
	{
		SetupBuilder();

		var (collectionView, labels, items) = CreateCollectionView(selectionMode, nested: false);

		await RunLoadedAsync(collectionView, async _ =>
		{
			var first = await WaitForAttachedLabelAsync(labels, 0);
			var completed = new EventLog<TouchGestureCompletedEventArgs>();
			first.Behaviors.OfType<TouchBehavior>().Single().TouchGestureCompleted += completed.Handle;

			await TapThroughUIKitAsync(GetPlatformView((IPlatformViewHandler)first.Handler!));

			await WaitUntilAsync(() => IsOnlySelected(collectionView, items[0]), "The tap did not select the item.");
			await WaitUntilAsync(() => completed.Count > 0, "The tap did not complete the behavior's gesture.");
			await SettleAsync();

			Assert.Equal(1, completed.Count);
		});

		static bool IsOnlySelected(CollectionView collectionView, Item item) => collectionView.SelectionMode is SelectionMode.Single
			? Equals(collectionView.SelectedItem, item)
			: collectionView.SelectedItems is [var selected] && Equals(selected, item);
	}
}