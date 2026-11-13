using CommunityToolkit.Maui.Behaviors;
using AEditText = Android.Widget.EditText;
using PlatformView = Android.Views.View;

namespace CommunityToolkit.Maui.DeviceTests;

// The binding has no getter for the flag that EditText.SetSelectAllOnFocus sets, so the tests focus the EditText and read the selection
// that Android makes.
public partial class SelectAllTextBehaviorTests
{
	static bool CanAssertPlatformViewIsCollected => false;

	static (int Start, int Length) GetNativeSelection(PlatformView platformView)
	{
		var editText = (AEditText)platformView;

		return (editText.SelectionStart, editText.SelectionEnd - editText.SelectionStart);
	}

	static void CollapseNativeSelection(PlatformView platformView) => ((AEditText)platformView).SetSelection(0);

	[Fact]
	public async Task BehaviorIsNotAttachedToAPlatformViewThatIsNotAnEditText()
	{
		SetupBuilder();

		// A SearchBar is an InputView, but its platform view is a SearchView, not an EditText: MAUI never attaches the behavior.
		var behavior = new CountingSelectAllTextBehavior();
		var searchBar = new SearchBar { Text = text, WidthRequest = 200 };
		searchBar.Behaviors.Add(behavior);

		await AttachAndRun(searchBar, async handler =>
		{
			await searchBar.OnLoadedAsync();

			Assert.IsNotType<AEditText>(handler.PlatformView);
			Assert.Equal(0, behavior.AttachedCount);
			Assert.Equal(0, behavior.DetachedCount);
		});

		await searchBar.OnUnloadedAsync();

		Assert.Equal(0, behavior.AttachedCount);
		Assert.Equal(0, behavior.DetachedCount);
	}

	// Focusing an input asks for its soft keyboard, which neither clearing the focus nor ending the test reliably takes back.
	protected override async ValueTask DisposeAsyncCore()
	{
		try
		{
			await base.DisposeAsyncCore();
		}
		finally
		{
			_ = await SoftKeyboardHelpers.HideAsync();
		}
	}
}