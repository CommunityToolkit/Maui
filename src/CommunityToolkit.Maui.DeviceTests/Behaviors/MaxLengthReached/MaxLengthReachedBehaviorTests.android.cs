using Microsoft.Maui.Platform;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;
using AView = Android.Views.View;
using Dialog = Android.App.Dialog;

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

	// A modal page or a Popup is a dialog window of its own, and the page underneath keeps its focused Entry in the activity's window meanwhile.
	// Android hides the soft keyboard only when it is given the token of the window that the keyboard serves.
	[Fact]
	public async Task SoftKeyboardIsHiddenWhenMaxLengthIsReachedInADialogWhileThePageUnderneathHasAFocusedEntry()
	{
		SetupBuilder();
		SkipWhenTheSoftKeyboardCannotShow();
		await EnsureSoftKeyboardHiddenAsync();
		var pageEntry = new Entry { WidthRequest = 200 };
		var (entry, maxLengthReached) = CreateEntryWithBehavior(shouldDismissKeyboardAutomatically: true);

		await AttachAndRun(pageEntry, async _ =>
		{
			await pageEntry.OnLoadedAsync();
			var editText = entry.ToPlatform(MauiContext);

			// Like the dialog of a MAUI modal page (a full screen theme and the soft input mode of the activity's window), so that the soft keyboard
			// covers the bottom of the dialog's visible frame: the frame of a floating dialog does not change on Android 9.
			var dialog = new Dialog(SoftKeyboardHelpers.RunnerActivity, Android.Resource.Style.ThemeMaterialLightNoActionBar);
			dialog.SetContentView(editText);
			if (SoftKeyboardHelpers.RunnerActivity.Window?.Attributes is Android.Views.WindowManagerLayoutParams activityWindowAttributes)
			{
				dialog.Window?.SetSoftInputMode(activityWindowAttributes.SoftInputMode);
			}

			dialog.Show();
			try
			{
				await AssertEventually(() => editText.HasWindowFocus, timeout: 3000, message: "The dialog never got the window focus.");

				// Focused while the dialog has the window focus, so that Focus shows no soft keyboard for it: the tests hide a keyboard that shows
				// from a view of the activity's window, which would take the focus of that window from the page's Entry.
				await FocusAsync(pageEntry);
				var frame = await ShowSoftKeyboardAsync(entry, editText);
				Assert.True(pageEntry.IsFocused && IsPlatformViewFocused(GetPlatformView(pageEntry)), "The page's Entry lost the focus of the activity's window.");

				SetPlatformText(editText, "abc");

				Assert.Equal("abc", await maxLengthReached.WaitAsync(TimeSpan.FromSeconds(2)));
				await AssertSoftKeyboardIsDismissedAsync(frame, isDismissed: true);
			}
			finally
			{
				dialog.Dismiss();
				dialog.Dispose();
				entry.Handler?.DisconnectHandler();
			}
		});
	}
}