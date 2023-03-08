using System;
using UIKit;

namespace CommunityToolkit.Maui.Core.Platform;

[UnsupportedOSPlatform("MacOS")]
public static partial class KeyboardExtensions
{
	static bool HideKeyboard(this UIView inputView)
	{
		throw new NotSupportedException("MacOS does not currently support opening the SoftKeyboard");
	}

	static bool ShowKeyboard(this UIView inputView)
	{
		throw new NotSupportedException("MacOS does not currently support opening the SoftKeyboard");
	}

	static bool IsSoftKeyboardShowing(this UIView inputView)
	{
		throw new NotSupportedException("MacOS does not currently support opening the SoftKeyboard");
	}
}