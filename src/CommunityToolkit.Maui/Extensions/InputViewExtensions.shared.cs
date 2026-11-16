#if ANDROID
using CommunityToolkit.Maui.Core.Platform;
#endif

namespace CommunityToolkit.Maui.Extensions;

static class InputViewExtensions
{
	public static void DismissKeyboard(this InputView inputView)
	{
		// The user can be typing in another view by now: UserStoppedTypingBehavior waits for its threshold first, and a binding can set the Text
		// of a view without the focus. Android hides the soft keyboard of a window, not of a view, so it would hide the keyboard of that other view.
		if (!inputView.IsFocused)
		{
			return;
		}

#if ANDROID
		// The soft keyboard stays open when the EditText of an Entry or an Editor loses the focus, so hide it first, while the view still has the focus.
		// HideKeyboardAsync completes synchronously on Android.
		_ = inputView.HideKeyboardAsync(CancellationToken.None);
#endif
		inputView.Unfocus();
	}
}