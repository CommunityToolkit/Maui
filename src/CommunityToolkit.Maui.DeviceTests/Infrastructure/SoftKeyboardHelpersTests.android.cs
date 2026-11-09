namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.DeviceTestsInfrastructure)]
[Collection(TestCollections.DisableParallelization)]
public class SoftKeyboardHelpersTests : CommunityToolkitHandlerTestBase
{
	// Android 9 shows the keyboard for an Entry that left the window before Android handled its focus, and a request through the window cannot
	// hide it, so the next test failed with "A soft keyboard was showing when the visible frame without keyboard was recorded."
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

			// The focus runs now, and what it posts waits behind this continuation, which ends the test and removes the view.
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