using System;
using CommunityToolkit.Maui.Core.Extensions;
using Microsoft.Maui.Dispatching;

namespace CommunityToolkit.Maui.Core.Platform;

/// <summary>
/// 
/// </summary>
public static partial class KeyboardManager
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="targetView"></param>
	public static void HideKeyboard(this ITextInput targetView)
	{
		if (targetView is not IView view)
		{
			return;

		}

		if (!view.TryGetPlatformView(out var platformView))
		{
			return;
		}

		HideKeyboard(platformView);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="targetView"></param>
	public static void ShowKeyboard(this ITextInput targetView)
	{
		if (targetView is not IView view)
		{
			return;
		}

		if (!view.TryGetPlatformView(out var platformView))
		{
			return;
		}

		if (view.Handler is not IViewHandler handler)
		{
			return;
		}

		if (!view.IsFocused)
		{
			handler.Invoke(nameof(IView.Focus), new FocusRequest(false));
			handler.GetRequiredService<IDispatcher>()
				.Dispatch(() =>
				{
					platformView.ShowKeyboard();
				});
		}
		else
		{
			platformView.ShowKeyboard();
		}
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="targetView"></param>
	/// <returns></returns>
	public static bool IsSoftKeyboardVisible(this ITextInput targetView)
	{
		if (targetView is not IView view)
		{
			return false;
		}

		if (!view.TryGetPlatformView(out var platformView))
		{
			return false;
		}

		return platformView.IsSoftKeyboardVisible();
	}
}