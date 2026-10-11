using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// On Android and Windows the behavior's view takes the touch, so the platform list never selects the item for it: a completed touch of a
/// <see cref="TouchBehavior"/> in the item template of a <see cref="CollectionView"/> selects the item through the toolkit
/// (<c>GestureManager.HandleCollectionViewSelection</c>, compiled for Android and Windows). On iOS and Mac Catalyst the touch is the
/// platform's business, so the toolkit must leave the selection alone (the collection view selects the item for a tap that UIKit delivers,
/// see the macios partial).
/// <para>
/// Windows takes no pointer input from a test (see <see cref="TouchBehaviorTestBase.NativePointerInputUnavailableReason"/>), so there a tap
/// is the sequence of internal calls that the behavior's pointer handlers make for a press and a release.
/// </para>
/// </summary>
[Category(TestCategory.TouchBehavior)]
[Collection(TestCollections.DisableParallelization)]
public partial class GestureManagerCollectionViewTests : TouchBehaviorTestBase
{
	sealed record Item(string Name);

	static readonly bool selectsThroughTheToolkit =
#if ANDROID || WINDOWS
		true;
#else
		false;
#endif

	[Fact]
	public async Task TappedItemIsSelectedWhenSelectionIsSingle()
	{
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
		SetupBuilder();

		var (collectionView, labels, items) = CreateCollectionView(SelectionMode.Single, nested: true);

		await RunLoadedAsync(collectionView, async _ =>
		{
			var first = await WaitForAttachedLabelAsync(labels, 0);

			Tap(first);

			AssertSelection(collectionView, selectsThroughTheToolkit ? items[0] : null);
		});
	}

#if ANDROID || WINDOWS
	// HandleTouch is called directly, as the platform's touch handler calls it, so that the NotSupportedException is thrown to the test.
	[Fact]
	public async Task UnsupportedSelectionModeIsReported()
	{
		SetupBuilder();

		var (collectionView, labels, _) = CreateCollectionView(SelectionMode.Single, nested: false);

		await RunLoadedAsync(collectionView, async _ =>
		{
			var label = await WaitForAttachedLabelAsync(labels, 0);
			var behavior = label.Behaviors.OfType<TouchBehavior>().Single();
			var rejected = Record.Exception(() => collectionView.SelectionMode = (SelectionMode)99);
			Skip.If(rejected is not null, $"MAUI already rejects a selection mode outside the enum ({rejected?.GetType().Name}), so the default arm of the toolkit's switch cannot be reached.");

			var completed = TouchStatus.Completed;
			Assert.Throws<NotSupportedException>(() => GestureManager.HandleTouch(in behavior, in completed));
		});
	}

	// Hosted headless under a Window that has no handler, the label's behavior never received the native touches (Android API 36), so the tree
	// is not hosted: the behavior gets its element directly and the touch comes through the internal handlers that the platform's touch
	// listener (Android) or pointer handlers (Windows) call.
	[Fact]
	public async Task SearchForTheCollectionViewEndsAtAnAncestorThatIsNotAVisualElement()
	{
		SetupBuilder();

		var command = new CountingCommand();
		var behavior = new DirectTouchBehavior { Command = command };
		var label = new Label { Text = "label" };
		var page = new ContentPage { Content = new Grid { Children = { label } } };
		var window = new Window(page);
		Assert.Same(window, page.Parent);

		await InvokeOnMainThreadAsync(() =>
		{
			behavior.SetViewOnly(label);

			SimulatePress(behavior);
			SimulateRelease(behavior);

			Assert.Equal(TouchStatus.Completed, behavior.CurrentTouchStatus);
			Assert.Equal(1, command.ExecuteCount);
		});
	}
#endif

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

	// Windows: what the behavior's pointer handlers do for a press and a release on the element (OnPointerPressed: interaction and touch
	// Started; OnPointerReleased: touch Completed, then interaction Completed), through the same internal calls.
	void Tap(Label label)
	{
#if WINDOWS
		var behavior = label.Behaviors.OfType<TouchBehavior>().Single();

		SimulatePress(behavior);
		SimulateRelease(behavior);
#else
		var handler = (IPlatformViewHandler)label.Handler!;

		NativePress(handler, 10, 10);
		NativeRelease(handler, 10, 10);
#endif
	}

	static void AssertSelection(CollectionView collectionView, Item? expected) =>
		Assert.Equal(expected, collectionView.SelectedItem);

	static void AssertSelectedItems(CollectionView collectionView, object[] expected) =>
		Assert.Equal(expected, (collectionView.SelectedItems ?? []).Cast<object>().ToArray());
}