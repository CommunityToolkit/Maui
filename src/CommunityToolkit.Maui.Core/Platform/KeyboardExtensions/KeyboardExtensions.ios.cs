namespace CommunityToolkit.Maui.Core.Platform;

public static partial class KeyboardExtensions
{
	static bool HideKeyboard(this UIView inputView) => inputView.ResignFirstResponder();

	static bool ShowKeyboard(this UIView inputView) => inputView.BecomeFirstResponder();

	static bool IsSoftKeyboardShowing(this UIView inputView) => inputView.IsFirstResponder;
}