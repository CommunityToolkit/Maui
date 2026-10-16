using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// On Android a completed touch of a <see cref="TouchBehavior"/> in the item template of a <see cref="CollectionView"/> selects the item through
/// the toolkit (<c>GestureManager.HandleCollectionViewSelection</c>, compiled for Android only); on the other platforms the touch is the
/// platform's business, so the toolkit must leave the selection alone (on iOS and Mac Catalyst the collection view selects the item for a tap
/// that UIKit delivers, see the macios partial).
/// </summary>
[Category(TestCategory.TouchBehavior)]
[Collection(TestCollections.DisableParallelization)]
public partial class GestureManagerCollectionViewTests : TouchBehaviorTestBase
{
	sealed record Item(string Name);

	static readonly bool selectsThroughTheToolkit =
#if ANDROID
		true;
#else
		false;
#endif

	[Fact]
	public async Task TappedItemIsSelectedWhenSelectionIsSingle()
	{
		RequireNativePointerInput();
		SetupBuilder();

		var (collectionView, labels, items) = CreateCollectionView(SelectionMode.Single, nested: false);

		await RunLoadedAsync(collectionView, async _ =>
		{
			var first = await WaitForAttachedLabelAsync(labels, 0);
			var second = await WaitForAttachedLabelAsync(labels, 1);

			Tap(first);
			AssertSelection(collectionView, selectsThroughTheToolkit ? items[0] : null);

			Tap(second);
			AssertSelection(collectionView, selectsThroughTheToolkit ? items[1] : null);
		});
	}

	// The last step resets the selection the way an app does (SelectedItems = null): MAUI coerces null into a new, empty selection, so the
	// toolkit's "SelectedItems?.ToList() ?? []" fallback for a null list cannot be reached; the next tap starts a new selection.
	[Fact]
	public async Task TappedItemsToggleInTheSelectionWhenSelectionIsMultiple()
	{
		RequireNativePointerInput();
		SetupBuilder();

		var (collectionView, labels, items) = CreateCollectionView(SelectionMode.Multiple, nested: false);

		await RunLoadedAsync(collectionView, async _ =>
		{
			var first = await WaitForAttachedLabelAsync(labels, 0);
			var second = await WaitForAttachedLabelAsync(labels, 1);

			Tap(first);
			AssertSelectedItems(collectionView, selectsThroughTheToolkit ? [items[0]] : []);

			Tap(second);
			AssertSelectedItems(collectionView, selectsThroughTheToolkit ? [items[0], items[1]] : []);

			Tap(first);
			AssertSelectedItems(collectionView, selectsThroughTheToolkit ? [items[1]] : []);

			collectionView.SelectedItems = null!;
			Tap(second);
			AssertSelectedItems(collectionView, selectsThroughTheToolkit ? [items[1]] : []);
		});
	}

	[Fact]
	public async Task TappedItemIsNotSelectedWhenSelectionIsNone()
	{
		RequireNativePointerInput();
		SetupBuilder();

		var (collectionView, labels, _) = CreateCollectionView(SelectionMode.None, nested: false);

		await RunLoadedAsync(collectionView, async _ =>
		{
			var first = await WaitForAttachedLabelAsync(labels, 0);

			Tap(first);

			AssertSelection(collectionView, null);
			Assert.Empty(collectionView.SelectedItems ?? []);
		});
	}

	[Fact]
	public async Task ItemIsFoundWhenTheBehaviorIsNestedInsideTheTemplate()
	{
		RequireNativePointerInput();
		SetupBuilder();

		var (collectionView, labels, items) = CreateCollectionView(SelectionMode.Single, nested: true);

		await RunLoadedAsync(collectionView, async _ =>
		{
			var first = await WaitForAttachedLabelAsync(labels, 0);

			Tap(first);

			AssertSelection(collectionView, selectsThroughTheToolkit ? items[0] : null);
		});
	}

	(CollectionView CollectionView, List<Label> Labels, Item[] Items) CreateCollectionView(SelectionMode selectionMode, bool nested)
	{
		var items = new[] { new Item("a"), new Item("b"), new Item("c") };
		var labels = new List<Label>();
		var collectionView = new CollectionView
		{
			ItemsSource = items,
			SelectionMode = selectionMode,
			WidthRequest = 200,
			HeightRequest = 300,
			ItemTemplate = new DataTemplate(() =>
			{
				var label = new Label { WidthRequest = 100, HeightRequest = 40 };
				label.SetBinding(Label.TextProperty, new Binding(nameof(Item.Name)));
				label.Behaviors.Add(new TouchBehavior());
				labels.Add(label);

				return nested ? new Grid { Children = { label } } : label;
			})
		};

		return (collectionView, labels, items);
	}

	async Task<Label> WaitForAttachedLabelAsync(List<Label> labels, int index)
	{
		await WaitUntilAsync(
			() => labels.Count > index && labels[index].Handler is not null && labels[index].Behaviors.OfType<TouchBehavior>().Single().Element is not null,
			$"The item {index} was not realized with its touch behavior attached.", 5000);

		return labels[index];
	}

	void Tap(Label label)
	{
		var handler = (IPlatformViewHandler)label.Handler!;

		NativePress(handler, 10, 10);
		NativeRelease(handler, 10, 10);
	}

	static void AssertSelection(CollectionView collectionView, Item? expected) =>
		Assert.Equal(expected, collectionView.SelectedItem);

	static void AssertSelectedItems(CollectionView collectionView, object[] expected) =>
		Assert.Equal(expected, (collectionView.SelectedItems ?? []).Cast<object>().ToArray());
}