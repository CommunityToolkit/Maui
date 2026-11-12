using System.Diagnostics;
using Android.Content;
using Android.Content.Res;
using Android.OS;
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
	public static async Task<bool> HideAsync() => (await WaitUntilHiddenAsync(requestEvenWhenNothingShows: true)).IsHidden;

	public static async Task<bool> HideWhenShowingAsync() => (await WaitUntilHiddenAsync(requestEvenWhenNothingShows: false)).IsHidden;

	// The tests call this from their own thread and the description reads the window's views, so it is made on the UI thread, and only when
	// the keyboard did not hide.
	public static async Task EnsureHiddenAsync()
	{
		var (isHidden, wasAcknowledged) = await WaitUntilHiddenAsync(requestEvenWhenNothingShows: true);

		if (!isHidden)
		{
			var reason = wasAcknowledged ? string.Empty : ", and the input method did not acknowledge the hide";
			Assert.Fail($"The soft keyboard could not be hidden within {Timeout} ms{reason}: {await TestDispatcher.Current.DispatchAsync(Describe)}.");
		}
	}

	public static string Describe() => $"{VisibleFrame.Describe(DecorView)}, the window insets {(InsetsShowKeyboard(DecorView) ? "show" : "do not show")} the keyboard";

	// Before Android 11 the keyboard that an earlier request asked for can still be on its way up when the window looks hidden, and it only
	// shows once the input method processed that request. The input method acknowledges a hide after it processed everything before it, and
	// the window manager tells the window what that did shortly after, so quiet polls count from the first frame after the acknowledgement.
	static Task<(bool IsHidden, bool WasAcknowledged)> WaitUntilHiddenAsync(bool requestEvenWhenNothingShows) => TestDispatcher.Current.DispatchAsync(async () =>
	{
		var stopwatch = Stopwatch.StartNew();
		var quietPolls = 0;
		var nextHideRequestAt = TimeSpan.Zero;
		var acknowledgementRequestedAt = TimeSpan.Zero;
		var acknowledgement = Task.CompletedTask;

		if (requestEvenWhenNothingShows)
		{
			acknowledgement = RequestHide();
			nextHideRequestAt = TimeSpan.FromMilliseconds(hideRequestInterval);
		}

		var isHidden = await Wait(() =>
		{
			if (!acknowledgement.IsCompleted)
			{
				return false;
			}

			if (!IsShowing())
			{
				quietPolls++;
				return quietPolls >= quietPollsBeforeItIsHidden;
			}

			quietPolls = 0;
			if (stopwatch.Elapsed >= nextHideRequestAt)
			{
				acknowledgement = RequestHide();
				acknowledgementRequestedAt = stopwatch.Elapsed;
				nextHideRequestAt = stopwatch.Elapsed + TimeSpan.FromMilliseconds(hideRequestInterval);
			}

			return false;
		}, Timeout, pollInterval);

		// A request from the last polls can still be in flight when the keyboard simply stayed up, and that is not the input method's fault.
		return (isHidden, acknowledgement.IsCompleted || stopwatch.Elapsed - acknowledgementRequestedAt < TimeSpan.FromMilliseconds(hideRequestInterval));
	});

	// On Android 9 only the view that the input method manager serves can hide the keyboard, and that can be an Entry that left the window
	// before Android handled its focus (a test that ends right after Focus). A focused view that is in the window becomes the served view.
	static Task RequestHide()
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

			// From Android 11 the app's insets controller animates the keyboard after the input method answered, and on Android 16 the client answers
			// the receiver itself, so the acknowledgement is no fence there.
			if (!OperatingSystem.IsAndroidVersionAtLeast(30))
			{
				var acknowledged = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
				var wasRequested = inputMethodManager.HideSoftInputFromWindow(servingView.WindowToken, HideSoftInputFlags.None, new HideAcknowledgement(acknowledged));

				// Nothing was shown or on its way (or the window rejected the request), so the input method sends no acknowledgement.
				return wasRequested ? WaitForFrameAfterAsync(acknowledged.Task) : Task.CompletedTask;
			}

			inputMethodManager.HideSoftInputFromWindow(servingView.WindowToken, HideSoftInputFlags.None);
			return Task.CompletedTask;
		}
		finally
		{
			// Cleared first, so that removing the view does not hand the focus to the first focusable view of the window.
			servingView.ClearFocus();
			content.RemoveView(servingView);
		}
	}

	// The insets of the window are only updated by a frame, and the acknowledgement does not wait for one.
	static async Task WaitForFrameAfterAsync(Task acknowledgement)
	{
		await acknowledgement;

		var frameDrawn = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		DecorView.PostOnAnimation(new Java.Lang.Runnable(() => frameDrawn.TrySetResult()));
		await frameDrawn.Task;
	}

	sealed class HideAcknowledgement(TaskCompletionSource acknowledged) : ResultReceiver(new Handler(Looper.MainLooper ?? throw new InvalidOperationException("The main looper is not available.")))
	{
		protected override void OnReceiveResult(int resultCode, Bundle? resultData) => acknowledged.TrySetResult();
	}
}