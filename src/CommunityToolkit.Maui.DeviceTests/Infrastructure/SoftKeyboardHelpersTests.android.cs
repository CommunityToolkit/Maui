namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.DeviceTestsInfrastructure)]
[Collection(TestCollections.DisableParallelization)]
public class SoftKeyboardHelpersTests : CommunityToolkitHandlerTestBase
{
	// The CI failure behind SoftKeyboardHelpers: the test ends before Android handled the focus of its Entry, so the keyboard is requested for a
	// view that already left the window, and the keyboard of that view cannot be hidden through the window.
	[Fact]
	public async Task HideAsyncHidesTheKeyboardOfAViewThatLeftTheWindowBeforeAndroidHandledItsFocus()
	{
		SetupBuilder();
		SoftKeyboardHelpers.SkipWhenTheSoftKeyboardCannotShow();
		await SoftKeyboardHelpers.EnsureHiddenAsync();
		var entry = new Entry { WidthRequest = 200 };

		await AttachAndRun(entry, async _ =>
		{
			await entry.OnLoadedAsync();
			entry.Focus();

			// The focus runs now, and what it posts waits behind this continuation, which ends the test and removes the view
			await Task.Yield();
		});

		await SoftKeyboardHelpers.EnsureHiddenAsync();

		await InvokeOnMainThreadAsync(() => VisualBehaviorHelpers.AssertStaysFalseAsync(SoftKeyboardHelpers.IsShowing, "The soft keyboard came up after it was hidden.", windowMilliseconds: 2000));
	}

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