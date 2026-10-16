using AView = Android.Views.View;

namespace CommunityToolkit.Maui.DeviceTests;

// Android: clearing the focus of an EditText leaves its soft keyboard open, so the behavior also hides the soft keyboard.
public partial class MaxLengthReachedBehaviorTests
{
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task SoftKeyboardIsHiddenWhenMaxLengthIsReachedOnlyWhenShouldDismissKeyboardAutomatically(bool shouldDismissKeyboardAutomatically)
	{
		SetupBuilder();
		SkipWhenTheSoftKeyboardCannotShow();
		await EnsureSoftKeyboardHiddenAsync();
		var (entry, maxLengthReached) = CreateEntryWithBehavior(shouldDismissKeyboardAutomatically);

		await AttachAndRun(entry, async handler =>
		{
			await entry.OnLoadedAsync();
			var editText = (AView)handler.PlatformView!;
			var frame = await ShowSoftKeyboardAsync(entry, editText);

			SetPlatformText(editText, "abc");

			Assert.Equal("abc", await maxLengthReached.WaitAsync(TimeSpan.FromSeconds(2)));
			await AssertSoftKeyboardIsDismissedAsync(frame, shouldDismissKeyboardAutomatically);
		});
	}
}