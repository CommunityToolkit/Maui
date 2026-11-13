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

	static int hideRequestCount;

	static bool HideKeyboard(this AView inputView)
	{
		Interlocked.Increment(ref hideRequestCount);

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
		// Not disposed: GetSystemService returns the same .NET wrapper of the input method manager to every caller
		var inputMethodManager = (InputMethodManager?)inputView.Context?.GetSystemService(Context.InputMethodService);

		// The zero value for the second parameter comes from 
		// https://developer.android.com/reference/android/view/inputmethod/InputMethodManager#showSoftInput(android.view.View,%20int)
		// Apparently there's no named value for zero in this case
		var shown = inputMethodManager?.ShowSoftInput(inputView, 0) is true;

		if (shown && OperatingSystem.IsAndroidVersionAtLeast(30) && !OperatingSystem.IsAndroidVersionAtLeast(33))
		{
			KeepTheKeyboardShownDespiteTheRunningImeHideAnimation(inputView);
		}

		return shown;
	}

	// Android 11 and 12 hide the keyboard again when an IME hide animation that was still running at ShowSoftInput ends (fixed in Android 13).
	// Showing the IME insets cancels that animation, unless its end is already queued, so a second ShowSoftInput follows when the keyboard is still hidden.
	// The insets controller needs the UI thread, and the caller must not wait for it.
	[SupportedOSPlatform("android30.0")]
	static void KeepTheKeyboardShownDespiteTheRunningImeHideAnimation(TextView inputView)
	{
		var hideRequestCountAtShow = Volatile.Read(ref hideRequestCount);

		if (Looper.MainLooper?.IsCurrentThread is true)
		{
			ShowImeInsetsThenReshowWhenHiddenAgain(inputView);
		}
		else
		{
			inputView.Post(() => ShowImeInsetsThenReshowWhenHiddenAgain(inputView));
		}

		void ShowImeInsetsThenReshowWhenHiddenAgain(TextView inputView)
		{
			if (!CanStillShowTheKeyboard(inputView))
			{
				return;
			}

			inputView.WindowInsetsController?.Show(WindowInsets.Type.Ime());
			inputView.PostDelayed(() => ReshowWhenHiddenAgain(inputView, 1, false), reshowIntervalMilliseconds);
		}

		// The keyboard of a slow input method can still be hidden by the animation after it showed, so it must show at two checks in a row.
		// A hide requested after the show means that the caller hid the keyboard, not the animation.
		void ReshowWhenHiddenAgain(TextView inputView, int attempt, bool wasShowingAtPreviousAttempt)
		{
			if (Volatile.Read(ref hideRequestCount) != hideRequestCountAtShow || !CanStillShowTheKeyboard(inputView))
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
				inputView.PostDelayed(() => ReshowWhenHiddenAgain(inputView, attempt + 1, isShowing), reshowIntervalMilliseconds);
			}
		}

		static bool CanStillShowTheKeyboard(TextView inputView) => inputView.Handle != IntPtr.Zero && inputView.IsFocused;
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