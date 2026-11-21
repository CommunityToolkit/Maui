namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.DeviceTestsInfrastructure)]
[Collection(TestCollections.DisableParallelization)]
public class KeyboardDismissalTestBaseTests : KeyboardDismissalTestBase
{
	// MAUI's Focus posts a keyboard show that Gboard can answer up to about a second later, during the next test.
	[Fact]
	public async Task FocusAsyncShowsNoSoftKeyboardThatCanComeUpAfterTheTest()
	{
		SetupBuilder();
		SkipWhenTheSoftKeyboardCannotShow();
		await EnsureSoftKeyboardHiddenAsync();
		var entry = new Entry { WidthRequest = 200 };

		await AttachAndRun(entry, async _ =>
		{
			await entry.OnLoadedAsync();
			await FocusAsync(entry);
			Assert.True(IsPlatformViewFocused(GetPlatformView(entry)));

			await InvokeOnMainThreadAsync(() => VisualBehaviorHelpers.AssertStaysFalseAsync(SoftKeyboardHelpers.IsShowing, "The soft keyboard came up after FocusAsync.", windowMilliseconds: 3000));
		});
	}
}
