using System;
using Android.Content;
using Android.Views;
using Android.Views.InputMethods;
using AndroidX.Core.View;
using Microsoft.Maui.Platform;
using AView = Android.Views.View;

namespace CommunityToolkit.Maui.Core.Platform;

public static partial class KeyboardExtensions
{
	static void HideKeyboard(this AView inputView)
	{
		var focusedView = inputView.Context?.GetActivity()?.Window?.CurrentFocus;
		AView tokenView = focusedView ?? inputView;

		using (var inputMethodManager = (InputMethodManager?)tokenView.Context?.GetSystemService(Context.InputMethodService))
		{
			var windowToken = tokenView.WindowToken;
			if (windowToken is not null && inputMethodManager is not null)
			{
				inputMethodManager.HideSoftInputFromWindow(windowToken, HideSoftInputFlags.None);
			}
		}
	}

	static void ShowKeyboard(this TextView inputView)
	{
		using (var inputMethodManager = (InputMethodManager?)inputView.Context?.GetSystemService(Context.InputMethodService))
		{
			// The zero value for the second parameter comes from 
			// https://developer.android.com/reference/android/view/inputmethod/InputMethodManager#showSoftInput(android.view.View,%20int)
			// Apparently there's no named value for zero in this case
			inputMethodManager?.ShowSoftInput(inputView, 0);
		}
	}

	static void ShowKeyboard(this AView view)
	{
		switch (view)
		{
			case TextView textView:
				textView.ShowKeyboard();
				break;
			case ViewGroup viewGroup:
				viewGroup.GetFirstChildOfType<TextView>()?.ShowKeyboard();
				break;
		}
	}

	static bool IsSoftKeyboardVisible(this AView view)
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