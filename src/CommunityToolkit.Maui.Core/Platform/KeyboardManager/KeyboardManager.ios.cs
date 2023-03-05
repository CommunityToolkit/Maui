using System;
using UIKit;

namespace CommunityToolkit.Maui.Core.Platform;

public static partial class KeyboardManager
{
	static void HideKeyboard(this UIView inputView) => inputView.ResignFirstResponder();

	static void ShowKeyboard(this UIView inputView) => inputView.BecomeFirstResponder();

	static bool IsSoftKeyboardVisible(this UIView inputView) => inputView.IsFirstResponder;
}