using System;
using System.Diagnostics;
using System.Reflection.Metadata;

namespace CommunityToolkit.Maui.Core.Platform;

public static partial class KeyboardExtensions
{
	static void HideKeyboard(this object _)
	{
	}

	static void ShowKeyboard(this object _)
	{
	}

	static bool IsSoftKeyboardVisible(this object _)
	{
		return false;
	}
}