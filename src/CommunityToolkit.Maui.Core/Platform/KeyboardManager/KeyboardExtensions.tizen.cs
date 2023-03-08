using System;
using System.Diagnostics;
using System.Reflection.Metadata;

namespace CommunityToolkit.Maui.Core.Platform;

public static partial class KeyboardExtensions
{
	static bool HideKeyboard(this object _)
	{
		return false;
	}

	static bool ShowKeyboard(this object _)
	{
		return false;
	}

	static bool IsSoftKeyboardShowing(this object _)
	{
		return false;
	}
}