using System.Runtime.Versioning;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using AndroidX.Core.View;
using Microsoft.Maui.Platform;
using AView = Android.Views.View;

namespace CommunityToolkit.Maui.Core.Platform;

public static partial class KeyboardExtensions
{
	const int reshowIntervalMilliseconds = 400;
	const int maximumReshowAttempts = 5;
	const long imeHideAnimationWindowMilliseconds = 2000;
	const long reshowDeadlineAfterHideMilliseconds = 4500;

	static int hideRequestCount;
	// Far enough in the past that UptimeMillis() minus it is always beyond the hide animation window, without overflowing like long.MinValue
	static long lastHideUptimeMilliseconds = -imeHideAnimationWindowMilliseconds - 1;

	static bool HideKeyboard(this AView inputView)
	{
		Interlocked.Increment(ref hideRequestCount);
		Interlocked.Exchange(ref lastHideUptimeMilliseconds, SystemClock.UptimeMillis());

		// A view with the focus can be in a dialog window of its own (a modal page or a Popup) while the activity's window keeps a focused view,
		// and the input method manager only hides the keyboard for the token of the window that the keyboard serves.
		var focusedView = inputView.HasFocus ? inputView : inputView.Context?.GetActivity()?.Window?.CurrentFocus;
		AView tokenView = focusedView ?? inputView;

		// Not disposed: GetSystemService returns the same .NET wrapper of the input method manager to every caller
		var inputMethodManager = (InputMethodManager?)tokenView.Context?.GetSystemService(Context.InputMethodService);
		var windowToken = tokenView.WindowToken;

		if (windowToken is not null && inputMethodManager is not null)
		{
			return inputMethodManager.HideSoftInputFromWindow(windowToken, HideSoftInputFlags.None);
		}

		return false;
	}

	static bool ShowKeyboard(this TextView inputView)
	{
		var hideRequestCountBeforeShow = Volatile.Read(ref hideRequestCount);
		// Not disposed: GetSystemService returns the same .NET wrapper of the input method manager to every caller
		var inputMethodManager = (InputMethodManager?)inputView.Context?.GetSystemService(Context.InputMethodService);

		// The zero value for the second parameter comes from 
		// https://developer.android.com/reference/android/view/inputmethod/InputMethodManager#showSoftInput(android.view.View,%20int)
		// Apparently there's no named value for zero in this case
		var shown = inputMethodManager?.ShowSoftInput(inputView, 0) is true;

		if (shown && OperatingSystem.IsAndroidVersionAtLeast(30) && !OperatingSystem.IsAndroidVersionAtLeast(33))
		{
			var lastHideUptime = Interlocked.Read(ref lastHideUptimeMilliseconds);
			if (SystemClock.UptimeMillis() - lastHideUptime <= imeHideAnimationWindowMilliseconds)
			{
				KeepTheKeyboardShownDespiteTheRunningImeHideAnimation(inputView, hideRequestCountBeforeShow, lastHideUptime + reshowDeadlineAfterHideMilliseconds);
			}
		}

		return shown;
	}

	// Android 11 and 12 hide the keyboard again when an IME hide animation still running at ShowSoftInput ends (fixed in Android 13),
	// so only a show right after a hide is guarded. Showing the IME insets cancels that animation unless its end is queued, so a later
	// ShowSoftInput follows if the keyboard is still hidden; the UI thread is needed and the caller must not wait.
	[SupportedOSPlatform("android30.0")]
	static void KeepTheKeyboardShownDespiteTheRunningImeHideAnimation(TextView inputView, int hideRequestCountAtShow, long deadlineUptimeMilliseconds)
	{
		if (Looper.MainLooper?.IsCurrentThread is true)
		{
			ShowImeInsetsThenReshowWhenHiddenAgain();
		}
		else
		{
			inputView.Post(ShowImeInsetsThenReshowWhenHiddenAgain);
		}

		void ShowImeInsetsThenReshowWhenHiddenAgain()
		{
			if (!CanStillShowTheKeyboard())
			{
				return;
			}

			inputView.WindowInsetsController?.Show(WindowInsets.Type.Ime());
			inputView.PostDelayed(() => ReshowWhenHiddenAgain(1, false), reshowIntervalMilliseconds);
		}

		// The keyboard of a slow input method can still be hidden by the animation after it showed, so it must show at two checks in a row.
		void ReshowWhenHiddenAgain(int attempt, bool wasShowingAtPreviousAttempt)
		{
			if (!CanStillShowTheKeyboard())
			{
				return;
			}

			var isShowing = inputView.IsSoftKeyboardShowing();
			if (isShowing && wasShowingAtPreviousAttempt)
			{
				return;
			}

			if (!isShowing)
			{
				var inputMethodManager = (InputMethodManager?)inputView.Context?.GetSystemService(Context.InputMethodService);
				inputMethodManager?.ShowSoftInput(inputView, 0);
			}

			if (attempt < maximumReshowAttempts)
			{
				inputView.PostDelayed(() => ReshowWhenHiddenAgain(attempt + 1, isShowing), reshowIntervalMilliseconds);
			}
		}

		// A hide requested after the show means that the caller hid the keyboard, not the animation; it can come before the posted first show runs.
		bool CanStillShowTheKeyboard() => Volatile.Read(ref hideRequestCount) == hideRequestCountAtShow
			&& inputView.Handle != IntPtr.Zero && inputView.IsAttachedToWindow && inputView.IsFocused && SystemClock.UptimeMillis() <= deadlineUptimeMilliseconds;
	}

	static bool ShowKeyboard(this AView view) => view switch
	{
		TextView textView => textView.ShowKeyboard(),
		// EditText rather than TextView, as MAUI finds the query editor of a SearchView: its first TextView is not necessarily editable
		// (a SearchView starts with its zero-size search badge)
		ViewGroup viewGroup => viewGroup.GetFirstChildOfType<EditText>()?.ShowKeyboard() is true,
		_ => false,
	};

	static bool IsSoftKeyboardShowing(this AView view)
	{
		var insets = ViewCompat.GetRootWindowInsets(view);
		if (insets is null)
		{
			return false;
		}

		var result = insets.IsVisible(WindowInsetsCompat.Type.Ime());
		return result;
	}
}