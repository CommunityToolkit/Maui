using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Maui.Core.Extensions;
using Microsoft.Maui.Dispatching;

#if IOS || MACCATALYST
using PlatformView = UIKit.UIView;
#elif ANDROID
using PlatformView = Android.Views.View;
#elif WINDOWS
using PlatformView = Microsoft.UI.Xaml.FrameworkElement;
#elif TIZEN
using PlatformView = Tizen.NUI.BaseComponents.View;
#elif (NETSTANDARD || !PLATFORM) || (NET6_0_OR_GREATER && !IOS && !ANDROID && !TIZEN)
using PlatformView = System.Object;
using IPlatformViewHandler = Microsoft.Maui.IViewHandler;
#endif

namespace CommunityToolkit.Maui.Core.Platform;

/// <summary>
/// 
/// </summary>
public static partial class KeyboardExtensions
{
	static bool TryGetPlatformView(
		this ITextInput textInput,
		[NotNullWhen(true)] out PlatformView? platformView,
		[NotNullWhen(true)] out IPlatformViewHandler? handler,
		[NotNullWhen(true)] out IView? view)
	{
		if (textInput is not IView iView ||
			iView.Handler is not IPlatformViewHandler platformViewHandler)
		{
			platformView = null;
			handler = null;
			view = null;
			return false;
		}

		if (iView.Handler?.PlatformView is not PlatformView platform)
		{

			platformView = null;
			handler = null;
			view = null;
			return false;
		}

		handler = platformViewHandler;
		platformView = platform;
		view = iView;

		return true;
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="targetView"></param>
	public static void HideKeyboard(this ITextInput targetView)
	{
		if (!targetView.TryGetPlatformView(
			out var platformView,
			out _,
			out _))
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
		if (!targetView.TryGetPlatformView(
			out var platformView, 
			out var handler,
			out var view))
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
		if (!targetView.TryGetPlatformView(
			out var platformView,
			out _,
			out _))
		{
			return false;
		}

		return platformView.IsSoftKeyboardVisible();
	}
}