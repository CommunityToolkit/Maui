using System.Diagnostics.CodeAnalysis;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Windows.UI.ViewManagement;

namespace CommunityToolkit.Maui.Core.Platform;

public static partial class KeyboardExtensions
{
	static bool HideKeyboard(this FrameworkElement element)
	{
		if (element.TryGetInputPane(out var inputPane))
		{
			return inputPane.TryHide();
		}

		return false;
	}

	static bool ShowKeyboard(this FrameworkElement element)
	{
		if (element.TryGetInputPane(out var inputPane))
		{
			return inputPane.TryShow();
		}

		return false;
	}

	static bool IsSoftKeyboardShowing(this FrameworkElement element)
	{
		if (element.TryGetInputPane(out var inputPane))
		{
			return inputPane.Visible;
		}

		return false;
	}

	static bool TryGetInputPane(this FrameworkElement element, [NotNullWhen(true)] out InputPane? inputPane)
	{
		var windowHandle = element.XamlRoot?.ContentIslandEnvironment is Microsoft.UI.Content.ContentIslandEnvironment contentIslandEnvironment
			? Win32Interop.GetWindowFromWindowId(contentIslandEnvironment.AppWindowId)
			: IntPtr.Zero;

		if (windowHandle == IntPtr.Zero)
		{
			inputPane = null;

			return false;
		}

		inputPane = InputPaneInterop.GetForWindow(windowHandle);
		return true;
	}
}