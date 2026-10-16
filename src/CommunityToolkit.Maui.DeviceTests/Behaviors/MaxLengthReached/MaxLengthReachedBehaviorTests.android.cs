using AView = Android.Views.View;

namespace CommunityToolkit.Maui.DeviceTests;

// Clearing the focus of an EditText leaves its soft keyboard open, so the behavior also hides the soft keyboard.
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

	// Android hides the soft keyboard of a window, not of a view, so a behavior that hid it for an Entry without the focus would hide the keyboard
	// of the Entry the user types in. A binding can set the Text of an Entry while the user types in another one.
	[Fact]
	public async Task SoftKeyboardOfAnotherInputStaysWhenTheTextOfAnInputWithoutTheFocusReachesMaxLength()
	{
		SetupBuilder();
		SkipWhenTheSoftKeyboardCannotShow();
		await EnsureSoftKeyboardHiddenAsync();
		var (entry, maxLengthReached) = CreateEntryWithBehavior(shouldDismissKeyboardAutomatically: true);
		var otherEntry = new Entry { WidthRequest = 200 };

		await AttachAndRun(new VerticalStackLayout { entry, otherEntry }, async _ =>
		{
			await entry.OnLoadedAsync();
			await otherEntry.OnLoadedAsync();
			var frame = await ShowSoftKeyboardAsync(otherEntry, GetPlatformView(otherEntry));

			entry.Text = "abc";

			Assert.Equal("abc", await maxLengthReached.WaitAsync(TimeSpan.FromSeconds(2)));
			Assert.True(otherEntry.IsFocused, "The other Entry lost the focus.");
			await AssertSoftKeyboardIsDismissedAsync(frame, isDismissed: false);
		});
	}
}