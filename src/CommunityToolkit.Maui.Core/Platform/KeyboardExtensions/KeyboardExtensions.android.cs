using Android.Content;
using Android.Views;
using Android.Views.InputMethods;
using AndroidX.Core.View;
using Microsoft.Maui.Platform;
using AView = Android.Views.View;

namespace CommunityToolkit.Maui.Core.Platform;

public static partial class KeyboardExtensions
{
	static bool HideKeyboard(this AView inputView)
	{
		var focusedView = inputView.Context?.GetActivity()?.Window?.CurrentFocus;
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
		return inputMethodManager?.ShowSoftInput(inputView, 0) is true;
	}

	static bool ShowKeyboard(this AView view) => view switch
	{
		TextView textView => textView.ShowKeyboard(),
		// The text editor of the group, as MAUI finds the query editor of a SearchView: its first TextView is not necessarily editable
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