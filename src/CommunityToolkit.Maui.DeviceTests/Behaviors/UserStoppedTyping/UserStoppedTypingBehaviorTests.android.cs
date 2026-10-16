using AView = Android.Views.View;

namespace CommunityToolkit.Maui.DeviceTests;

// Clearing the focus of an EditText leaves its soft keyboard open, so the behavior also hides the soft keyboard.
public partial class UserStoppedTypingBehaviorTests
{
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task SoftKeyboardIsHiddenWhenTheUserStopsTypingOnlyWhenShouldDismissKeyboardAutomatically(bool shouldDismissKeyboardAutomatically)
	{
		SetupBuilder();
		SkipWhenTheSoftKeyboardCannotShow();
		await EnsureSoftKeyboardHiddenAsync();
		var (entry, commandExecuted) = CreateEntryWithBehavior(shouldDismissKeyboardAutomatically);

		await AttachAndRun(entry, async handler =>
		{
			await entry.OnLoadedAsync();
			var editText = (AView)handler.PlatformView!;
			var frame = await ShowSoftKeyboardAsync(entry, editText);

			SetPlatformText(editText, "abc");

			Assert.Equal("abc", await commandExecuted.WaitAsync(TimeSpan.FromSeconds(5)));
			await AssertSoftKeyboardIsDismissedAsync(frame, shouldDismissKeyboardAutomatically);
		});
	}
}