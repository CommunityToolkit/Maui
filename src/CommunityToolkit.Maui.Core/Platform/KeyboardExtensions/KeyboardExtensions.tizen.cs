using Tizen.NUI.BaseComponents;

namespace CommunityToolkit.Maui.Core.Platform;

public static partial class KeyboardExtensions
{
	static bool HideKeyboard(this Tizen.NUI.BaseComponents.View view) => SetKeyInputFocus(view, false);

	static bool ShowKeyboard(this Tizen.NUI.BaseComponents.View view) => SetKeyInputFocus(view, true);

	static bool IsSoftKeyboardShowing(this Tizen.NUI.BaseComponents.View view)
	{
		return view.KeyInputFocus;
	}

	static bool SetKeyInputFocus(Tizen.NUI.BaseComponents.View view, bool isShow)
	{
		view.KeyInputFocus = isShow;

		return view.KeyInputFocus;
	}
}
