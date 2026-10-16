using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.DeviceTests;

// Android only: GestureManager.HandleCollectionViewSelection and its search for the CollectionView are compiled for Android only.
public partial class GestureManagerCollectionViewTests
{
	// Catches: an unsupported selection mode being swallowed instead of reported (the NotSupportedException default arm). A mode outside
	// the enum can only reach the method directly; a touch of the platform view goes through the same HandleTouch.
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

	// The search for a CollectionView walks up the element's parents and must stop at the first one that is not a VisualElement: here the
	// Window above the page, which ends the search without a CollectionView, and the tap completes as usual. Hosted headless under a Window
	// that has no handler, the label's behavior never received the native touches (Android API 36), so the tree is not hosted: the behavior
	// gets its element directly and the touch comes through the internal handlers that the platform listener calls.
	// Catches: the search failing on an ancestor that is not a VisualElement (its "Parent as VisualElement" step), or selecting anyway.
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
}