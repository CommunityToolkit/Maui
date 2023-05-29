using Tizen.NUI.BaseComponents;

namespace CommunityToolkit.Maui.Core.Platform;

public static partial class KeyboardExtensions
{
	static bool HideKeyboard(this View view) => SetKeyInputFocus(view, false);

	static bool ShowKeyboard(this View view) => SetKeyInputFocus(view, true);

	static bool IsSoftKeyboardShowing(this View view)
	{
		return view.KeyInputFocus;
	}

	static bool SetKeyInputFocus(View view, bool isShow)
	{
		view.KeyInputFocus = isShow;

		return view.KeyInputFocus;
	}
}
