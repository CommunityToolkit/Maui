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
/// Extension methods for interacting with a platforms Soft Keyboard Pane
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
	/// If a soft input device is currently showing, this will attempt to hide it.
	/// </summary>
	/// <param name="targetView"></param>
	/// <param name="token">Cancellation token</param>
	/// <returns>
	/// Returns <c>true</c> if the platform was able to hide the soft input device.</returns>
	public static Task<bool> HideKeyboardAsync(this ITextInput targetView, CancellationToken token)
	{
		if (!targetView.TryGetPlatformView(
			out var platformView,
			out _,
			out _))
		{
			return Task.FromResult(false).WaitAsync(token);
		}

		return Task.FromResult(HideKeyboard(platformView)).WaitAsync(token);
	}

	/// <summary>
	/// If a soft input device is currently hiding, this will attempt to show it.
	/// </summary>
	/// <param name="targetView"></param>
	/// <param name="token">Cancellation token</param>
	/// <returns>
	/// Returns <c>true</c> if the platform was able to show the soft input device.</returns>
	public static Task<bool> ShowKeyboardAsync(this ITextInput targetView, CancellationToken token)
	{
		if (!targetView.TryGetPlatformView(
			out var platformView,
			out var handler,
			out var view))
		{
			return Task.FromResult(false);
		}

		if (!view.IsFocused)
		{
			TaskCompletionSource<bool> result = new TaskCompletionSource<bool>();
			handler.Invoke(nameof(IView.Focus), new FocusRequest(false));
			handler.GetRequiredService<IDispatcher>()
				.Dispatch(() =>
				{
					result.TrySetResult(platformView.ShowKeyboard());
				});

			return result.Task.WaitAsync(token);
		}
		else
		{
			return Task.FromResult(platformView.ShowKeyboard()).WaitAsync(token);
		}
	}

	/// <summary>
	/// Checks to see if the platform is currently showing the soft input pane
	/// </summary>
	/// <param name="targetView"></param>
	/// <returns>
	/// Returns <c>true</c> if the soft input device is currently showing.</returns>
	public static bool IsSoftKeyboardShowing(this ITextInput targetView)
	{
		if (!targetView.TryGetPlatformView(
			out var platformView,
			out _,
			out _))
		{
			return false;
		}

		return platformView.IsSoftKeyboardShowing();
	}
}