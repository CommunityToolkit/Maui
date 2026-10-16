using System.Diagnostics;
using Android.Content;
using Android.Content.Res;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidX.Core.View;
using CommunityToolkit.Maui.DeviceTests.Runners;
using Microsoft.Maui.Dispatching;
using static CommunityToolkit.Maui.DeviceTests.AssertHelpers;
using AActivity = Android.App.Activity;
using AView = Android.Views.View;

namespace CommunityToolkit.Maui.DeviceTests;

// At the app root because the tests of Behaviors and of Extensions/KeyboardExtensions share them.
static class SoftKeyboardHelpers
{
	public const int Timeout = 5000;

	const int pollInterval = 50;
	const int quietPollsBeforeItIsHidden = 2;
	const int hideRequestInterval = 500;

	public static AActivity RunnerActivity => MauiProgramDefaults.DefaultContext as AActivity
		?? throw new InvalidOperationException("The runner's activity is not available.");

	static AView DecorView => RunnerActivity.Window?.DecorView ?? throw new InvalidOperationException("The runner's window is not available.");

	// Android never shows the soft keyboard while the device reports a hardware keyboard (an AVD with hw.keyboard=yes), unless the user turned
	// "show_ime_with_hard_keyboard" on. The pooled emulators have no hardware keyboard.
	public static void SkipWhenTheSoftKeyboardCannotShow()
	{
		var configuration = RunnerActivity.Resources?.Configuration ?? throw new InvalidOperationException("The runner's configuration is not available.");
		var hasHardwareKeyboard = configuration.Keyboard != KeyboardType.Nokeys && configuration.HardKeyboardHidden != HardKeyboardHidden.Yes;
		var showsImeWithHardwareKeyboard = Android.Provider.Settings.Secure.GetInt(RunnerActivity.ContentResolver, "show_ime_with_hard_keyboard", 0) == 1;

		Skip.If(hasHardwareKeyboard && !showsImeWithHardwareKeyboard, "This Android device reports a hardware keyboard (AVD hw.keyboard=yes), so Android does not show the soft keyboard.");
	}

	// The window insets (what the toolkit reads) and the visible display frame (what the tests measure) are updated at different moments,
	// so the keyboard only counts as hidden once both agree.
	public static bool IsShowing() => InsetsShowKeyboard(DecorView) || VisibleFrame.KeyboardCoversTheBottomOf(DecorView);

	// The same AndroidX call as the toolkit: used to prepare and clean up, never as the oracle of a toolkit result.
	public static bool InsetsShowKeyboard(AView view) =>
		ViewCompat.GetRootWindowInsets(view)?.IsVisible(WindowInsetsCompat.Type.Ime()) is true;

	// Also asks when nothing shows yet: a keyboard that was asked for can still be on its way up, and only a request to hide cancels it.
	public static Task<bool> HideAsync() => WaitUntilHiddenAsync(requestEvenWhenNothingShows: true);

	public static Task<bool> HideWhenShowingAsync() => WaitUntilHiddenAsync(requestEvenWhenNothingShows: false);

	public static async Task EnsureHiddenAsync() =>
		Assert.True(await HideAsync(), $"The soft keyboard could not be hidden within {Timeout} ms: {Describe()}.");

	public static string Describe() => $"{VisibleFrame.Describe(DecorView)}, the window insets {(InsetsShowKeyboard(DecorView) ? "show" : "do not show")} the keyboard";

	static Task<bool> WaitUntilHiddenAsync(bool requestEvenWhenNothingShows) => TestDispatcher.Current.DispatchAsync(() =>
	{
		var stopwatch = Stopwatch.StartNew();
		var quietPolls = 0;
		var nextHideRequestAt = TimeSpan.Zero;

		if (requestEvenWhenNothingShows)
		{
			RequestHide();
			nextHideRequestAt = TimeSpan.FromMilliseconds(hideRequestInterval);
		}

		return Wait(() =>
		{
			if (!IsShowing())
			{
				quietPolls++;
				return quietPolls >= quietPollsBeforeItIsHidden;
			}

			quietPolls = 0;
			if (stopwatch.Elapsed >= nextHideRequestAt)
			{
				RequestHide();
				nextHideRequestAt = stopwatch.Elapsed + TimeSpan.FromMilliseconds(hideRequestInterval);
			}

			return false;
		}, Timeout, pollInterval);
	});

	// A keyboard can only be hidden through the view that the input method manager serves, and on Android 9 that can be a view that already left
	// the window: MAUI posts the focus of an Entry and its request to show the keyboard, and Android posts its check of the focus behind them,
	// so a test that ends right after Focus removes the view before they run. Asking through the window then hides nothing. A view that is in
	// the window and has the focus becomes the served view, so it can hide the keyboard.
	static void RequestHide()
	{
		var activity = RunnerActivity;
		var content = activity.FindViewById<FrameLayout>(Android.Resource.Id.Content) ?? throw new InvalidOperationException("The activity has no content view.");
		var inputMethodManager = activity.GetSystemService(Context.InputMethodService) as InputMethodManager ?? throw new InvalidOperationException("The activity has no input method manager.");
		using var servingView = new AView(activity)
		{
			Focusable = true,
			FocusableInTouchMode = true,
		};

		content.AddView(servingView, new FrameLayout.LayoutParams(1, 1));
		try
		{
			servingView.RequestFocus();
			inputMethodManager.HideSoftInputFromWindow(servingView.WindowToken, HideSoftInputFlags.None);
		}
		finally
		{
			// Cleared first, so that removing the view does not hand the focus to the first focusable view of the window
			servingView.ClearFocus();
			content.RemoveView(servingView);
		}
	}
}