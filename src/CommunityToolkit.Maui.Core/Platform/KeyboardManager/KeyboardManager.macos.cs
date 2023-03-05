using System;
using UIKit;

namespace CommunityToolkit.Maui.Core.Platform;

[UnsupportedOSPlatform("MacOS")]
public static partial class KeyboardManager
{
	static void HideKeyboard(this UIView inputView)
	{
		throw new NotSupportedException("MacOS does not currently support opening the SoftKeyboard");
	}

	static void ShowKeyboard(this UIView inputView)
	{
		throw new NotSupportedException("MacOS does not currently support opening the SoftKeyboard");
	}

	static bool IsSoftKeyboardVisible(this UIView inputView)
	{
		throw new NotSupportedException("MacOS does not currently support opening the SoftKeyboard");
	}
}