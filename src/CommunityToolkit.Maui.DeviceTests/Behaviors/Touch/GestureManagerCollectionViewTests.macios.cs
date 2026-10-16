using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.DeviceTests;

// iOS and Mac Catalyst: the selection is the collection view's own, from a tap that UIKit delivers.
public partial class GestureManagerCollectionViewTests
{
	// A tap that UIKit delivers to an item whose template has a TouchBehavior selects the item, as a tap selects it without the behavior, and
	// completes the behavior's gesture: the behavior's recognizer never recognizes, so it never takes a touch away from the views.
	// Catches: the behavior's recognizer taking the tap for itself (recognizing it and cancelling the touches of the views, as a
	// UITapGestureRecognizer does): the collection view never selects the item.
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