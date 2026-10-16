using Android.Content;
using Android.Content.Res;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidX.Core.View;
using CommunityToolkit.Maui.Core.Platform;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;
using PlatformView = Android.Views.View;

namespace CommunityToolkit.Maui.DeviceTests;

// Clearing the focus of an EditText leaves its soft keyboard open, so the behaviors hide the keyboard themselves. The tests read whether
// the keyboard shows from the visible display frame; the AndroidX window insets (what the toolkit reads) only prepare and clean up.
public abstract partial class KeyboardDismissalTestBase
{
	const int softKeyboardTimeout = 5000;

	protected static void SetPlatformText(PlatformView platformView, string text) => ((EditText)platformView).Text = text;

	protected static bool IsPlatformViewFocused(PlatformView platformView) => platformView.IsFocused;

	// Android never shows the soft keyboard while the device reports a hardware keyboard (an AVD with hw.keyboard=yes), unless the user turned
	// "show_ime_with_hard_keyboard" on. The pooled emulators have no hardware keyboard.
	protected static void SkipWhenTheSoftKeyboardCannotShow()
	{
		var configuration = RunnerActivity.Resources!.Configuration!;
		var hasHardwareKeyboard = configuration.Keyboard != KeyboardType.Nokeys && configuration.HardKeyboardHidden != HardKeyboardHidden.Yes;
		var showsImeWithHardwareKeyboard = Android.Provider.Settings.Secure.GetInt(RunnerActivity.ContentResolver, "show_ime_with_hard_keyboard", 0) == 1;

		Skip.If(hasHardwareKeyboard && !showsImeWithHardwareKeyboard, "This Android device reports a hardware keyboard (AVD hw.keyboard=yes), so Android does not show the soft keyboard.");
	}

	protected static async Task<VisibleFrame> ShowSoftKeyboardAsync(InputView input, PlatformView platformView)
	{
		var frame = VisibleFrame.RecordWithoutKeyboard(platformView);

		Assert.True(await input.ShowKeyboardAsync(), "ShowKeyboardAsync returned false.");
		Assert.True(await Wait(() => frame.ShowsKeyboard, softKeyboardTimeout), $"The soft keyboard did not show: {frame}.");
		Assert.True(input.IsFocused, "The view does not have the focus while its soft keyboard shows.");

		return frame;
	}

	protected static async Task AssertSoftKeyboardIsDismissedAsync(VisibleFrame frame, bool isDismissed)
	{
		if (isDismissed)
		{
			Assert.True(await Wait(() => !frame.ShowsKeyboard, softKeyboardTimeout), $"The soft keyboard is still showing: {frame}.");
		}
		else
		{
			Assert.False(await Wait(() => !frame.ShowsKeyboard, 1000), $"The soft keyboard was hidden: {frame}.");
		}
	}

	protected override async ValueTask DisposeAsyncCore()
	{
		try
		{
			await base.DisposeAsyncCore();
		}
		finally
		{
			await EnsureSoftKeyboardHiddenAsync();
		}
	}

	protected static Task EnsureSoftKeyboardHiddenAsync() => InvokeOnMainThreadAsync(async () =>
	{
		var decorView = RunnerActivity.Window!.DecorView;
		decorView.FindFocus()?.ClearFocus();

		if (decorView.WindowToken is not null && RunnerActivity.GetSystemService(Context.InputMethodService) is InputMethodManager inputMethodManager)
		{
			inputMethodManager.HideSoftInputFromWindow(decorView.WindowToken, HideSoftInputFlags.None);
		}

		await Wait(() => ViewCompat.GetRootWindowInsets(decorView)?.IsVisible(WindowInsetsCompat.Type.Ime()) is not true, softKeyboardTimeout);
	});

	static Android.App.Activity RunnerActivity => MauiProgramDefaults.DefaultContext as Android.App.Activity
		?? throw new InvalidOperationException("The runner's activity is not available.");

	// The visible display frame of the window loses the height of the soft keyboard at its bottom, whatever the soft input mode (pan or
	// resize) is. 100 dp is more than any navigation bar and less than any soft keyboard.
	protected sealed class VisibleFrame
	{
		const int minimumKeyboardHeightInDp = 100;

		readonly PlatformView view;
		readonly int bottomWithoutKeyboard;
		readonly float minimumKeyboardHeight;

		VisibleFrame(PlatformView view)
		{
			Assert.True(view.IsAttachedToWindow, "The visible frame is read from a view that is in a window.");
			Assert.False(ViewCompat.GetRootWindowInsets(view)?.IsVisible(WindowInsetsCompat.Type.Ime()) is true, "A soft keyboard was showing when the visible frame without keyboard was recorded.");
			this.view = view;
			bottomWithoutKeyboard = Bottom(view);
			minimumKeyboardHeight = minimumKeyboardHeightInDp * view.Resources!.DisplayMetrics!.Density;
		}

		public bool ShowsKeyboard => bottomWithoutKeyboard - Bottom(view) > minimumKeyboardHeight;

		public static VisibleFrame RecordWithoutKeyboard(PlatformView view) => new(view);

		public override string ToString() =>
			$"visible frame bottom {Bottom(view)} px, {bottomWithoutKeyboard} px without keyboard, keyboard threshold {minimumKeyboardHeight} px";

		static int Bottom(PlatformView view)
		{
			using var frame = new Android.Graphics.Rect();
			view.GetWindowVisibleDisplayFrame(frame);
			return frame.Bottom;
		}
	}
}