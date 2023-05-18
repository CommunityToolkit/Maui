using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml;
using Windows.UI.ViewManagement;

namespace CommunityToolkit.Maui.Core.Platform;

public static partial class KeyboardExtensions
{
	static bool HideKeyboard(this FrameworkElement element)
	{
		if (TryGetInputPane(out var inputPane))
		{
			return inputPane.TryHide();
		}

		return false;
	}

	static bool ShowKeyboard(this FrameworkElement element)
	{
		if (TryGetInputPane(out var inputPane))
		{
			return inputPane.TryShow();
		}

		return false;
	}

	static bool IsSoftKeyboardShowing(this FrameworkElement element)
	{
		if (TryGetInputPane(out var inputPane))
		{
			return inputPane.Visible;
		}

		return false;
	}

	static bool TryGetInputPane([NotNullWhen(true)] out InputPane? inputPane)
	{
		var handleId = Process.GetCurrentProcess().MainWindowHandle;
		if (handleId == IntPtr.Zero)
		{
			inputPane = null;

			return false;
		}

		inputPane = InputPaneInterop.GetForWindow(handleId);
		return true;
	}
}