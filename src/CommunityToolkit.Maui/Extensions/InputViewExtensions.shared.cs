#if ANDROID
using CommunityToolkit.Maui.Core.Platform;
#endif

namespace CommunityToolkit.Maui.Extensions;

static class InputViewExtensions
{
	public static void DismissKeyboard(this InputView inputView)
	{
#if ANDROID
		// The soft keyboard stays open when the EditText of an Entry or an Editor loses the focus, so hide it first, while the view still has the focus.
		// HideKeyboardAsync completes synchronously on Android.
		_ = inputView.HideKeyboardAsync(CancellationToken.None);
#endif
		inputView.Unfocus();
	}
}