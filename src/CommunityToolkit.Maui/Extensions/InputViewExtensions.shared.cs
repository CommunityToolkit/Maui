#if ANDROID
using CommunityToolkit.Maui.Core.Platform;
#endif

namespace CommunityToolkit.Maui.Extensions;

static class InputViewExtensions
{
	/// <summary>
	/// Removes the focus from the <see cref="InputView"/> and dismisses its soft keyboard.
	/// </summary>
	/// <remarks>
	/// On Android the soft keyboard stays open when an <c>EditText</c> (the platform view of an <see cref="Entry"/> or an <see cref="Editor"/>)
	/// loses the focus, so the keyboard is hidden first, while the view still has the focus.
	/// </remarks>
	public static void DismissKeyboard(this InputView inputView)
	{
#if ANDROID
		// HideKeyboardAsync completes synchronously on Android
		_ = inputView.HideKeyboardAsync(CancellationToken.None);
#endif
		inputView.Unfocus();
	}
}