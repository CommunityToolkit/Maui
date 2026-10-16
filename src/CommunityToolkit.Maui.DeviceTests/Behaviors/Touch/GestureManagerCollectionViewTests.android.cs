using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class GestureManagerCollectionViewTests
{
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
	// is not hosted: the behavior gets its element directly and the touch comes through the internal handlers that the platform listener calls.
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