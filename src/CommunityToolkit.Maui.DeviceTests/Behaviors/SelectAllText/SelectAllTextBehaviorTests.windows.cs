using CommunityToolkit.Maui.Behaviors;
using Microsoft.UI.Xaml.Controls;
using PlatformView = Microsoft.UI.Xaml.FrameworkElement;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class SelectAllTextBehaviorTests
{
	// Not verified on a device: the WinUI element is only reachable through COM wrappers, so the toolkit-owned managed objects are what the leak tests require.
	static bool CanAssertPlatformViewIsCollected => false;

	static (int Start, int Length) GetNativeSelection(PlatformView platformView)
	{
		var textBox = (TextBox)platformView;

		return (textBox.SelectionStart, textBox.SelectionLength);
	}

	static void CollapseNativeSelection(PlatformView platformView) => ((TextBox)platformView).Select(0, 0);

	[Fact]
	public async Task BehaviorIsNotAttachedToAPlatformViewThatIsNotATextBox()
	{
		SetupBuilder();

		// A SearchBar is an InputView, but its platform view is an AutoSuggestBox, not a TextBox: MAUI never attaches the behavior.
		var behavior = new CountingSelectAllTextBehavior();
		var searchBar = new SearchBar { Text = text, WidthRequest = 200 };
		searchBar.Behaviors.Add(behavior);

		await AttachAndRun(searchBar, async handler =>
		{
			await searchBar.OnLoadedAsync();

			Assert.IsNotType<TextBox>(handler.PlatformView);
			Assert.Equal(0, behavior.AttachedCount);
			Assert.Equal(0, behavior.DetachedCount);
		});

		await searchBar.OnUnloadedAsync();

		Assert.Equal(0, behavior.AttachedCount);
		Assert.Equal(0, behavior.DetachedCount);
	}

	[Fact]
	public async Task MissingPlatformViewThrowsInvalidOperationExceptionWhenTheBehaviorAttachesOrDetaches()
	{
		SetupBuilder();

		var probe = new SelectAllTextBehaviorProbe();
		var entry = new Entry { Text = text };

		await InvokeOnMainThreadAsync(() =>
		{
			var attachException = Assert.Throws<InvalidOperationException>(() => probe.AttachTo(entry, null!));
			var detachException = Assert.Throws<InvalidOperationException>(() => probe.DetachFrom(entry, null!));

			Assert.Equal("The Platform View can't be null.", attachException.Message);
			Assert.Equal("The Platform View can't be null.", detachException.Message);
		});
	}
}