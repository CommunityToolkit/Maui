using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using Windows.UI.ViewManagement;

namespace CommunityToolkit.Maui.Core.Platform;

public static partial class KeyboardManager
{
	static InputPane? InputPane
	{
		get
		{
			var handleId = Process.GetCurrentProcess().MainWindowHandle;
			if (handleId == IntPtr.Zero)
			{
				return null;
			}

			return InputPaneInterop.GetForWindow(handleId);
		}
	}

	static void HideKeyboard(this FrameworkElement _)
	{
		if (InputPane is not InputPane inputPane)
		{
			return;
		}

		inputPane.TryHide();
	}

	static void ShowKeyboard(this FrameworkElement _)
	{
		if (InputPane is not InputPane inputPane)
		{
			return;
		}

		inputPane.TryShow();
	}

	static bool IsSoftKeyboardVisible(this FrameworkElement _)
	{
		if (InputPane is not InputPane inputPane)
		{
			return false;
		}

		return inputPane.Visible;
	}
}